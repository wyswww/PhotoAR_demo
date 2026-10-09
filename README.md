# PhotoAR：照片生成模型与 Android AR

这是独立的新工程，位置为本目录中的 `Unity`。使用本机 Unity 6000.3.19f1、AR Foundation 6.3.5 与 ARCore，后端复用已经部署的 TripoSR，未修改原 TripoSR 或已有 Unity 工程。

**当前状态：后端模拟、真实 GPU 推理、Unity 内分功能测试、完整 Play Mode 模拟联调与 Android APK 构建已通过。最新版 APK 已安装，当前 AAK-AN00 手机已完成真实照片生成、6464 顶点模型加载、显示和自转。Google ARCore 原生会话返回设备不兼容，真实 AR 尚未通过。用户已同意先验证 EasyAR 表面跟踪；接入需官方 SDK 与测试 License Key。具体限制见 `reports/CURRENT_PHONE_AR.md`。**

## 功能与模块

| 模块 | 实现 | 独立测试方式 |
|---|---|---|
| 照片输入 | Android 系统文件选择器；编辑器读取照片路径；后端处理 EXIF 方向 | 手机选图；HTTP 上传测试 |
| 3D 生成 | `backend/adapters.py` 的真实 TripoSR 适配器和模拟适配器 | `Test-Backend.ps1` 与 `Test-Backend.ps1 -Real` |
| 任务接口 | 上传、单线程 GPU 队列、状态查询、下载；失败与超时状态 | `backend/test_service.py`、`backend/test_http.py` |
| 模型解析 | OBJ 顶点色、三角化、负索引；TripoSR Z 向上转换为 Unity Y 向上 | 独立 C# 解析测试；Unity 的 Model loader 菜单 |
| 模型展示 | 最大尺寸归一为 0.3 米，底部中心作为模型原点 | App 内无需服务器的模型测试按钮 |
| 自转 | 模型子对象绕本地 Y 轴每秒 30 度；支持暂停 | Unity 的 Rotation and fixed anchor 菜单 |
| 平面选择 | 可追踪、未被合并的平面边界投影至相机画面；近裁剪与屏幕边界裁剪后比较面积 | 几何测试、模拟相机平面测试 |
| 中心与锚定 | 使用完整平面边界多边形的面积重心；由 ARAnchorManager 创建真实锚点 | 模拟场景；手机真实 AR 验证 |

“面积最大”指**画面中的可见投影面积**，不是平面在现实世界中的平方米数。平面重叠时分别计算投影，不做深度遮挡分割。中心取整个已检测平面边界的几何重心；部分平面出画时，该中心有可能在画外。当前支持水平与竖直平面。

模型载入后先预览。点击放置按钮时选择当下最大平面并锚定，随后不因新平面出现而自动跳动。要重新选择，先重置放置。锚点和旋转对象分开：锚点 → 自转对象 → 底部居中的网格。竖直平面上，自转轴沿平面法线。

## 本机运行

在本目录打开 PowerShell：

```powershell
# 不需要 GPU 的独立模拟自测（包括真实本机 HTTP 通信）
.\Test-Backend.ps1

# 使用本机 TripoSR 生成示例椅子模型
.\Test-Backend.ps1 -Real

# 启动真实后端，默认只监听本机
.\Start-Backend.ps1

# 启动模拟后端，独立检查 Unity 网络流程
.\Start-Backend.ps1 -Mock
```

后端地址为 `http://127.0.0.1:8090`。模拟与真实后端使用同一端口，分别启动，不能同时占用该端口。服务器在窗口中运行，结束时按 Ctrl+C。

本机配置：

```text
TripoSR：E:\PROJECT_my_python\TripoSR
Python：E:\conda-envs\triposr311\python.exe
Unity：E:\Unity\6000.3.19f1\Editor\Unity.exe
```

可通过 `TRIPOSR_REPO`、`TRIPOSR_PYTHON`、`PHOTOAR_PORT`、`PHOTOAR_DATA` 环境变量覆盖后端配置。当前网格分辨率 128；提高质量时可设置 `PHOTOAR_RESOLUTION=256`。推理单任务上限 600 秒，最多 4 个等待/执行中的任务，照片上限 12 MB、2400 万像素。缓存和生成输出写在服务数据目录。服务重启后，遗留的未完成任务查询会返回失败，需要重新上传。

## Unity 分功能模拟自测

1. 在 Unity Hub 中确保编辑器许可证有效；添加并打开本目录的 `Unity` 工程。
2. 执行 `PhotoAR > Prepare Android project`。这个菜单创建场景、配置 Android/ARCore、保留模型顶点色 Shader，并将输入处理设为 Both。
3. 执行 `PhotoAR > Tests` 中各项独立测试，或执行 `PhotoAR > Run independent self tests` 一次运行全部。
4. 点击 Play。编辑器使用两块模拟平面，无需手机相机；模拟锚点明确标记为模拟对象。
5. 首先用 `Test model loading + rotation` 检查模型展示和自转；暂停/恢复并检查锚点不转。
6. 用 `Test largest visible plane` 检查选取面积和中心；用放置/重置按钮验证放置流程。
7. 启动模拟后端，填写照片路径，选择照片再生成，检查上传、轮询、下载、加载流程。
8. 改为真实后端，使用 `E:\PROJECT_my_python\TripoSR\examples\chair.png`，检查真实模型显示、色彩、朝向、底部接触及旋转。

本工程已将 9 个所需 Unity 包放到项目的 `Packages` 内，不依赖旧工程的包缓存路径，也未下载或安装新包。

## Android 验证和打包

编辑器模拟通过后，执行 `PhotoAR > Build Android APK`。输出为 `builds/PhotoAR.apk`，目标 ARM64、IL2CPP、OpenGL ES 3，最低 Android 8.0；实际设备还需支持 ARCore。当前 APK 已生成并通过签名检查。

首轮真机联调建议使用 USB：启动真实后端，连接已启用 USB 调试并授权的手机，然后执行本机 Android SDK 的 `adb reverse tcp:8090 tcp:8090`。App 中仍填写 `http://127.0.0.1:8090`。这是 USB 端口映射，不需要公网或内网穿透。

手机打开 App 后授权相机，从系统文件选择器选择照片，生成模型，缓慢扫描平面，再点击放置。核对最大可见平面、几何中心、缩放、朝向、平面接触、自转暂停和重置。当前版本通过系统选图输入照片；需要新照片时可先使用手机相机拍照，再选择照片。

需要同一局域网联调时，可手动启动 `Start-Backend.ps1 -Lan` 并将 App 地址改为电脑局域网 IP。当前未开启该模式、未修改防火墙、未配置内网穿透。公网部署与认证应在各项本地/手机验收成功后另行处理。

## API

| 请求 | 行为 |
|---|---|
| `GET /health` | 检查服务、当前真实/模拟模式 |
| `POST /jobs` | multipart 字段 `photo`，成功返回 HTTP 202 和任务 ID |
| `GET /jobs/{id}` | 状态 `queued/running/succeeded/failed` |
| `GET /jobs/{id}/model` | 下载顶点色 OBJ；未成功时返回 HTTP 409 |
| `GET /models` | 列出电脑上保存的成功模型，按时间倒序，服务重启后仍可读取 |
| `GET /jobs/{id}/thumbnail` | 返回对应照片的最长边240像素 JPEG 缩略图 |

Unity 的 `GenerationClient` 负责接口通信，`ObjLoader` 负责模型解析/展示，`Spin` 负责旋转，`IPlacementProvider` 隔离模拟平面和 AR 平面，`ARPlacement` 负责真实锚点，`AppController` 只编排界面流程。后端的 `MeshGenerator` 协议隔离真实/模拟生成器，服务层不导入 GPU 模型代码。

## 当前手机的 EasyAR 独立测试

手机界面已改为中文创作台。点击“模型库”可查看已生成的模型与照片缩略图，点击“放置”加载并重新定位，无需重新生成；保持电脑服务和 USB 连接，列表可以刷新。开始/停止自转为独立按钮，底部面板可以收起。“独立测试”入口保留旋转、示例模型与连接检查。默认、圆形与自适应启动图标已配置为新的 PhotoAR 图标。界面层 PhotoARMobileUI 与模型库客户端 ModelLibraryClient 独立。

用户已同意先测试表面跟踪。SDK 已接入，Android 授权包名验证通过。使用菜单 `PhotoAR > EasyAR > Build independent surface test` 构建，或关闭本工程编辑器后执行 `scripts/Build-EasyAR.ps1 -LicenseFile <本地授权文本路径>`。输出 `builds/PhotoAR-EasyAR.apk`，与 PhotoAR 使用相同包名，安装会更新现有测试 App。原始 AR Foundation 场景通过原菜单独立构建。

表面跟踪不提供平面检测、最大平面选择或真实尺度；实机结果见 `reports/CURRENT_PHONE_AR.md`。授权资产位于 `Unity/Assets/PhotoAR/LocalSecrets/`，已加入忽略列表；分享源码时需排除此目录。该实验采用本地生成的模型作为测试夹具，若分享工程请自行替换个人模型。

## 结果

- `reports/TEST_REPORT.md`：已执行测试及尚未验证项目。
- `reports/chair.obj`：本机实际生成的椅子网格，可在 Blender 中查看。
- `reports/chair-preview.png`：实际网格的静态预览；不是 AR 场景截图。

实现参考：[TripoSR 官方源码](https://github.com/VAST-AI-Research/TripoSR/blob/main/run.py)、[Unity ARAnchorManager](https://docs.unity.cn/Packages/com.unity.xr.arfoundation%406.1/api/UnityEngine.XR.ARFoundation.ARAnchorManager.html)。实际 API 签名也对照了本机安装的 AR Foundation 6.3.5 源码。
