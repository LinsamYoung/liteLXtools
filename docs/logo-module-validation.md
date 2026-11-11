# 原厂开机画面子模块验证

用户授权提取可独立运行的原厂子模块，并另外批准打开和关闭Logo窗口的初始化验证。实际测试只观察窗口加载与关闭。

## 提取集合

`modules/LenovoLogo` 包含5个原厂DLL和1个外部图片，共1570639字节。逐文件SHA-256记录于manifest.json。自编x86宿主为VendorLogoHost.exe，源码位于src/VendorLogoHost.cs。

宿主直接创建LogoDiy.LogoDiyView。通用LenovoToolMonitoring宿主在启动时还会初始化BatteryPolicyManager和其他配置插件，当前提取采用单窗口入口。

## 运行观察

- 普通权限窗口可以打开，但固件参数读取不足时原窗口显示过负数高度，图片操作呈禁用状态。
- 管理员权限下窗口显示最大2560×1600、支持BMP，并加载当前开机Logo。
- 通过WPF渲染获得完整窗口截图 `analysis/vendor-logo-window-admin.png`。
- 窗口正常关闭，进程退出码0。
- 初始化前与管理员窗口关闭后的逻辑盘列表均为C、D、E。
- 原厂保存/恢复按钮尚未点击，固件写入与重启效果尚未验收。

Logo页面从EFI分区读取当前图片，初始化可能短时挂载并解除该分区。此次授权包含这一初始化行为。GUI中的原厂模块启动按钮默认请求UAC管理员权限，主工具其他功能沿用各自所需权限。

## 独立性边界

原窗口在自编宿主中成功加载，当前包的CLR引用与原生AIToolAPI依赖足以覆盖已测试初始化路径。原文件保持提取时SHA-256。

原厂模块的完整保存交互、所有格式分支及其他依赖尚未覆盖。电脑管家主程序和服务当时仍安装运行，卸载后的独立性需在用户批准卸载后继续验收。当前静态调用链已经定位到Windows固件变量和EFI文件接口，报告据此给出最小保留判断。

## 主窗口直接执行

2026-11-11 已将主界面的保存和恢复入口接入 `LogoOperationHost.exe`。这是独立的 x86 无界面执行器，继承主窗口的管理员权限，通过结果文件向主窗口返回状态。`VendorLogoHost.exe` 继续提供 CLI 原厂窗口诊断入口。

静态检查确认原厂 `LogoDiy.ViewModels.WndHelper` 使用 cdecl 声明以下接口，保存流程中的图片校验参数使用 ANSI 字符串，当前后台执行器传入临时 EFI 盘符下的 ASCII 图片路径。

| 接口 | 原始签名 |
| --- | --- |
| `GetLogoDIYInfo` | `int(ref byte enable, ref uint format, ref int height, ref int width)` |
| `SetLogoDIYInfo` | `int(byte enable)` |
| `SetLogDIYCheck` | `int(string image)` |

原生导出分别位于 `AIToolAPI.dll` 的 RVA `0x4870`、`0x48B0` 和 `0x4C30`。`SetLogDIYCheck` 会按校验数据版本选择不同实现，后台执行器复用该导出。原始 DLL 的 SHA-256 为 `dbfdfd8d28e2580d8defdeee710f9bf5a251c3d2ef5ddc323aeeeb5198ebb5fc`，执行器在调用前再次校验。

`LBLDESP` 为10字节，命名空间为 `{871455D0-5576-4FB8-9865-AF0824463B9E}`。`LBLDVC` 为40字节，使用独立命名空间 `{871455D1-5576-4FB8-9865-AF0824463C9F}`。后台执行器读取两个变量以核对长度和接口返回值，写入后回读参数。

保存和恢复默认直接修改图片和固件设置。参数、输入图片及结果使用 `%TEMP%\LiteLX\LogoOperations\<操作ID>` 下的临时工作文件传递，操作结束时清理。写入阶段失败时尝试关闭自定义画面，并将错误返回主窗口。后台执行器通过共享硬件互斥锁串行执行，主窗口在操作结束前保持忙碌状态。临时 EFI 盘符在退出前解除，清理失败会返回具体盘符和原因。

固件变量写入使用 Windows 的固件环境变量权限，EFI 临时挂载使用 `mountvol /s`，清理使用 `/d`。[Windows 固件写入文档](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setfirmwareenvironmentvariableexw)、[mountvol 文档](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/mountvol)

新增 `tools/LogoSelfTest.cs`，使用模拟固件和独立临时文件验证成功保存、恢复默认、部分完成失败、校验失败、回读不符和关闭失败，并断言操作过程未生成图片或固件备份。构建通过170项原有模拟断言及64项开机画面断言，另通过84项界面检查。已验证后台执行器的 x86 架构、无参数退出和错误结果返回。本轮验证覆盖模拟流程及静态接口，实机固件写入、UAC 后的完整运行和重启显示效果尚未验收。

## 页面提示与启动授权

开机画面页将保存、恢复默认和重启生效的说明常驻显示在按钮上方。保存和恢复默认按钮直接执行对应操作。

GUI 的 `gui.manifest` 声明 `requireAdministrator`，每次启动时由 Windows 请求授权。`LogoOperationHost.exe` 使用 `asInvoker`，主窗口已取得管理员令牌后，后台执行器继承该令牌。构建已核对两个可执行文件实际嵌入的清单。[Windows UAC 与父子进程令牌说明](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/user-account-control/how-it-works)

2026-11-11 对本机原厂组件的只读检查得到以下结果。

- `LenovoToolMonitoring.exe` 的实际清单为 `asInvoker`，SHA-256 为 `c80d8ecd7e020dc94bec68b1dc3698bf178ce0d98f1044f2d0bb9c33f809209d`。REA 资源检查记录为 `ev_aa3086dd1a2f9d50e1a93f2d1e9b00d04af2f5e4d5cfea04ab60d59d8529ecf6`。
- 原厂工具进程 PID 8628 的父进程为 PID 12148，即 `LenovoTray.exe`。通过 `GetTokenInformation(TokenElevation)` 实测两个进程均为 `Elevated`。
- `LenovoPcManagerService` 正在运行，其服务账户为 `LocalSystem`。

原厂模块启动时已经处于高权限上下文，这与其打开时无需再次出现 UAC 的现象一致。托盘进程最初取得高权限的完整启动链尚未追踪。
