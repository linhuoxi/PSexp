# PSexp

用于 Photoshop 的 Windows 批量导出工具，支持将文档、画板和一级图层导出为 JPG / PNG。

使用 C#、.NET 10 和 Avalonia 构建，以 [MIT 许可证](LICENSE)开源。

## 功能

- 五种导出模式：快速导出、全部画板、选中画板、一级图层、切片。
- JPG / PNG 格式、JPG 质量、输出位置及命名设置。
- 显示导出任务状态、输出数量和错误信息。
- 浅色 / 深色主题、窗口置顶、贴边收纳和设置持久化。

“切片”是本项目沿用的模式名称：按顺序导出初始可见的一级图层，并非 Photoshop 原生切片区域。一级图层模式会导出符合条件的一级图层，包括初始隐藏的图层；背景和“排除”前缀图层不作为单独导出目标。

## 下载与运行

前往 [Releases](https://github.com/linhuoxi/PSexp/releases) 查看已发布版本和 Windows x64 附件；若没有附件，可按下方说明自行编译。

1. 使用 Windows x64，安装并启动 Photoshop，打开需要导出的文档。
2. 运行 PSexp，选择导出模式、格式及输出目录。
3. 执行导出并检查任务状态和生成的图片。

自包含发布版无需另外安装 .NET。导出依赖 Photoshop UXP 独立脚本支持（Photoshop 23.5+）以及本机可用的 `.psjs` 文件关联；文档探测使用 Windows COM。不同 Photoshop 版本的实际兼容性需要实机验证。

取消任务表示 PSexp 停止等待，Photoshop 可能仍在执行已触发的操作。再次导出前请确认 Photoshop 已结束上一任务。

## 从源码编译

需要 **.NET 10 SDK**。在克隆后的仓库根目录运行：

```powershell
.\编译.bat
```

脚本先运行 .NET 自测，再发布 Windows x64 自包含单文件程序；成功后将 `PSexp v3.0.exe` 放到 `编译.bat` 旁边，中间文件保存在系统临时目录。默认不混淆、不裁剪，保留单文件压缩；脚本不上传 GitHub。

生成的 EXE 不纳入 Git 版本管理。Node.js 仅用于额外的 UXP 脚本行为测试，运行软件不需要 Node.js。

## 项目结构

```text
PSdodo/                         应用源码、界面和内置 Photoshop 脚本
tests/PSdodo.Tests/             .NET 自测和 UXP 脚本行为测试
references/photoshop-scripts/   历史脚本参考，不直接编译进应用
docs/                          开发和界面规范
编译.bat                        公开的编译与自测入口
LICENSE                         MIT 许可证
```

GitHub 仓库根目录对应作者本地的 `PSexp/源码/`，克隆后无需再创建一层 `源码`。

## 开发与反馈

- [开发指南](docs/开发指南.md)：构建、测试、模块说明和维护注意事项。
- [UI 规范](docs/UI规范.md)：组件尺寸、状态和布局规则。
- [提交问题或建议](https://github.com/linhuoxi/PSexp/issues)：请附 Windows / Photoshop 版本、复现步骤和错误信息；日志或截图中的个人路径请先脱敏。

自动化测试不能代替 Photoshop 实机导出和窗口交互验收。
