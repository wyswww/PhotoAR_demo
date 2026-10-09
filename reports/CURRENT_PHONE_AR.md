# 当前手机 AR 接入状态

设备：AAK-AN00 / HNAAK，Android 16，荣耀 WIN RT。

## 已完成的手机验证

新版 PhotoAR 已安装成功。手机缓存的已选照片通过 USB 本地连接提交给真实 TripoSR 服务，完成生成、OBJ 下载、6464 顶点 Mesh 加载和屏幕显示。连续诊断中 Y 轴角度变化，确认自转。证据：phone-preview.json、phone-preview.png。生成质量仍取决于照片和 TripoSR 的重建能力。

## 原始 AR 要求尚未通过

Google ARCore 已安装，相机权限已授予，但原生 Session 构造抛出 UnavailableDeviceNotCompatibleException。没有相机帧、跟踪平面或真实锚点。模型预览不能视为真实 AR 验收。

## 候选替代方案

EasyAR Surface Tracking 使用相机与惯性传感器跟踪环境表面，无需使用 ARCore；需实机验证。本功能不提供平面检测或真实尺度，特征丢失可能漂移，因此不能直接满足自动选择最大平面中心的原始要求。EasyAR Motion Tracking 需要机型标定，其当前公开列表未找到 WIN / AAK；不能承诺本机支持。

用户已同意先验证表面跟踪，并提供官方 SDK 4003.1.1 与本地授权文件。Android 包名匹配检查已通过。授权配置保存在忽略版本控制的 Unity/Assets/PhotoAR/LocalSecrets/；不得提交到公共仓库。

独立场景为 Assets/Scenes/EasyARSurface.unity，入口 PhotoAR/EasyAR/Build independent surface test，产物 builds/PhotoAR-EasyAR.apk。该测试以官方 SurfaceTracking 示例的相机和会话配置为基础，关闭 XR 自动启动以避免 ARCore 抢占相机；自动加载已生成模型，复用 ObjLoader/Spin/GenerationClient。easyar-diagnostics.json 记录会话、帧数、目标找到/丢失次数、相机位置及模型状态，用于真实手机验收。它不记录图像或密钥。

新增提供者应实现 IPlacementProvider，将相机姿态与背景接入单独封装；GenerationClient、ObjLoader 与 Spin 继续复用。先单独验证相机、追踪稳定性和放置，再联调重建模型。AR Foundation 原生路径保留独立测试。

## 官方资料

- https://developer.honor.com/cn/docs/game_center/guides/jieruzhinan/jixing
- https://developers.google.com/ar/devices?hl=zh-CN
- https://www.easyar.com/doc/en/develop/surface-tracking/intro.html
- https://easyar.com/doc/en/develop/motion-tracking/devices-easyar.html
- https://www.easyar.com/doc/en/develop/unity/fundamentals/initialization.html
- https://www.easyar.com/view/download.html

内网穿透和公网部署均未开展。

## EasyAR 首轮实机结果

独立 APK 构建成功、签名验证通过，已更新安装并启动。SHA-256：6B14DC19713B33E853626EEEC8C4D040F54ACB6B6EFD798602965A4F020836FA。

本机原生相机和表面跟踪已启动：Session Running，首次诊断相机帧数 165，tracked=true，found=1，lost=0；6464 顶点模型曾报告可见，自转角度变化。截图确认真实相机背景。随后移动中诊断模型在画面外，相机位置数值显著变化；表面跟踪不具备真实尺度，不能仅凭 tracked=true 判定放置稳定。已请求用户重新对准桌面中心并隐藏面板，验证模型可见性与漂移。证据 phone-easyar.json / phone-easyar.png。

照片生成到 AR 的同次完整流程、长时间跟踪稳定性和原始最大平面锚定仍未验收。当前结果证明这台手机能够启动 EasyAR 相机和表面跟踪，不代表原始 AR Foundation 需求已完成。

## 自转修复与手机回归

用户反馈不旋转后，连续诊断发现模型 Y 角度固定为 166.6797 度，即使模型可见也不变化。原按钮为无状态提示的暂停/恢复切换；修复为独立 Start rotation / Stop rotation 按钮与 ON/OFF 状态，加载后显式开启，测试版速度为 60 度/秒。

更新 APK 构建、签名校验及手机安装成功。手机端通过同一控制方法完成开始、停止、恢复三阶段测试，全部为 True，最终保持开启；诊断显示 rotationRunning=true、modelActive=true、modelVisible=true、rotationSpeed=60，角度变化。证据 phone-rotation-test.txt、phone-easyar-rotation.json。

最新 EasyAR APK SHA-256：BA3C8C990F26C1D9A6BFB7179079B82D20E43A94693A57014E3EE5680E60EA96。
