# 中文界面、图标与历史模型库

已实现中文创作台、可收起的底部面板、跟踪与自转状态、独立开始/停止按钮、照片预览、生成按钮和历史模型库。界面为独立 PhotoARMobileUI，模型库通信为 ModelLibraryClient，生成与跟踪模块继续复用。

电脑服务增加 GET /models 与 GET /jobs/{id}/thumbnail；历史模型来自保存目录，仅列出成功且网格文件存在的记录。缩略图最长边240像素。七项服务测试通过，包含服务重启后的历史记录读取、缺失模型排除及缩略图验证。

第一轮 UI APK 已安装。手机端通过真实模型库行按钮触发选择，结果 Selection via UI button: True; SavedModels: 4; Rotation: True。已加载历史模型至 AR 目标，实际模型顶点数变化为5390。用户在此版本继续生成了新模型，服务器记录成功，可在列表刷新后查看。

手机截图发现标题字体行高与 SDK SessionDump 文本覆盖问题；最终修正版已解决（放宽文字垂直行高、关闭 SessionDump UI，错误保留日志），保留 EasyAR 个人授权水印。最终构建和签名验证通过，但两次安装返回 INSTALL_FAILED_ABORTED: User rejected permissions；等待手机安装确认后完成最终视觉验收。

最终 APK：builds/PhotoAR-EasyAR.apk。SHA-256：2C11EF92518C4F24D9D30482F6012C0D7509BAA4611F3843315BC1034872D4D3。

Android APK 包含默认、Legacy、Round 与 Adaptive 图标资源。图标、完整生成提示词及字体来源见 design/DESIGN.md 与 design/AppIcon.png。

真实最大平面中心锚定仍未实现；本轮工作为既有表面跟踪版本的 UI 与模型库完善。内网穿透仍未开展。
