# PhotoAR 测试记录

日期：2026-10-02。目标：Android / Unity 6000.3.19f1 / AR Foundation 6.3.5。

## 已执行且通过

| 检查 | 结果与依据 |
|---|---|
| 后端接口模拟 | 6 项：健康检查、无效照片、超限照片、未知任务、完整生成流程、模拟 GPU 失败及下载拒绝 |
| 本机真实 HTTP | 启动独立模拟服务器，通过 TCP 回环执行健康检查、multipart 上传、轮询和 OBJ 下载；完成后自动停止服务器 |
| 真实 GPU 生成接口 | 使用已安装 TripoSR、离线模型缓存和 chair.png，真实任务成功，随后通过下载接口获取 OBJ；详见 real-inference.json |
| 真实模型 | 10,252 个顶点、20,420 个三角形，约 1.01 MB，保留顶点颜色 |
| C# 代码编译 | 编辑器配置/测试与运行时代码编译成功；另以 Android 编译条件单独编译运行时代码成功，包括相机权限代码；详见 csharp-compilation.txt 和 android-csharp-compilation.txt |
| Android 选图桥接编译 | Gallery.java 对本机 Android API 35 和 Unity Java 类编译成功；Android Fragment API 有弃用提示，未进行手机运行测试 |
| 独立 C# 算法/解析 | 13 项：面积、重心、顺逆时针一致性、画面裁剪、画外剔除、空边界、退化边界拒绝、OBJ 拓扑/负索引、坐标转换、顶点色、非法索引拒绝、真实 TripoSR 网格解析；详见 geometry-tests.txt |

已修复首次真实推理依赖导入停顿：Numba 缓存移到项目服务数据目录，保留原 TripoSR 与原 Python 环境。

## 本次续测已通过

首次运行遇到许可证错误，用户打开并激活编辑器后已解决。此次在独立测试副本中运行，避免与用户已打开的工程冲突：

- Unity 内几何、原生 Mesh 创建、旋转及暂停、固定锚点与最大投影平面独立测试全部通过，见 `unity-selftest.txt`。
- Play Mode 完整流程通过：真实椅子 Mesh 加载、Shader 错误检查、按实际帧自转、模拟中心放置、锚点不随自转移动、暂停、UnityWebRequest 上传/查询/下载/加载，见 `runtime-smoke.txt`。这是无图形批处理测试，未进行屏幕视觉验收。
- Android ARM64 / IL2CPP / OpenGL ES 3 APK 构建成功，最新诊断版约 42 MB，签名验证通过，位置 `builds/PhotoAR.apk`。
- 已修复包自身 Tests 源码进入工程编译范围的问题，改用 Unity 忽略的 `Tests~` 目录保留。
- 已修复 ARCore + Vulkan 最低 Android 版本冲突，改用 OpenGL ES 3；当前不要求设备具备 Depth API。
- 首版 PhotoAR 已成功安装并启动于 USB 连接手机 AAK-AN00，Android 16。
- 官方 ARCore `1.56.262080393` 已成功安装。APK 来自 Google 的 `google-ar/arcore-android-sdk` 1.56.0 Release；下载 SHA-256 与 GitHub 发布摘要一致，APK 签名检查通过。

最新已安装 PhotoAR APK SHA-256：`B9DE42BBCAE91D77494C69A7499FCF3E843A454DC2469920666A5A63E8331A9F`。

## 待手机验收

- 手机最新诊断版 PhotoAR 已安装成功。原生照片选择已成功，缓存照片真实生成和下载加载已成功，模型有 6464 个顶点。
- 连续诊断确认模型可见且 Y 轴角度改变；手机截图确认模型显示，见 phone-preview.json、phone-preview.png。此项为 3D 预览，不是 AR。
- 原生 ARCore Session 返回 UnavailableDeviceNotCompatibleException；相机帧和平面数量为零，真实中心锚定未通过。
- EasyAR SDK 和授权已取得，Android 包名验证通过。独立 APK 构建、签名验证、安装成功。实机 Session Running，相机帧数增长，tracked=true，模型自转；真实相机截图已保存。模型曾可见，移动后在画面外，正在等待用户重新居中检查放置稳定性。详见 CURRENT_PHONE_AR.md。
- 自转修复版已安装。显式开始/停止按钮和旋转状态显示已实现，默认开启。手机同一控制方法的开始、停止、恢复测试全通过，模型可见且开启自转，每秒60度；证据 phone-rotation-test.txt、phone-easyar-rotation.json。

相应独立测试和构建入口已实现，见 README。最新诊断版会在 App 私有目录记录 `ar-diagnostics.json`，只含 AR 状态、相机权限、平面数量和模型加载状态，不记录照片，也不上传数据。ARCore 安装成功本身不能证明此设备支持真实 AR 跟踪。

Unity 窗口自动操作被自动审批拒绝，返回“Computer Use was not approved to use Unity”。因此以上自动验证使用独立工程副本与批处理完成。

## 未开展

内网穿透、公网发布、防火墙修改均未进行。待编辑器分功能测试、完整本机联调、手机真机验收成功后再推进。
