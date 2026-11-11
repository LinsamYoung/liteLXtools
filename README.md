# LiteLX

面向当前 Lenovo 82TF 的 Windows 轻量工具，包含开机画面、右键菜单与风扇除尘。采用开机画面后台接口与 Windows 原生接口组合。

主窗口已改为WPF，使用圆角卡片、胶囊导航和矢量布局，并声明Per-Monitor V2 DPI。界面源码为 `src/LiteLX/ModernWindow.xaml` 与 `ModernWindow.cs`。右键页提供应用图标、系统本地化名称/产品名称、分组入口及开关，分组操作保留原注册位置，直接读取和修改注册表中的实际禁用状态。无法解析的扩展保留在全部注册项中。当前已验证本机缩放下的三页渲染，多显示器之间的实时缩放仍待现场检查。

## 当前可用程度

| 功能 | 当前状态 |
| --- | --- |
| 开机画面 | 已接入主窗口直接保存和恢复的后台执行器，支持直接写入、文件校验和状态回读。模拟测试通过，实机写入和重启显示尚未验收 |
| 原生右键菜单管理 | 编译与真实只读扫描通过，支持普通命令与传统COM handler。写入和Explorer效果尚待验证 |
| 风扇WMI FullSpeed | 本机管理员权限下5秒启停与独立停止回读通过。旧式EnergyDrv启停验证失败 |

调查、依赖与卸载后判断见 [分析报告](analysis/report.md)。风扇真实测试及日志缺口见 [测试记录](docs/fan-test-result.md)。当前卸载后完整验收仍待用户另行批准。

## 启动

双击 `build\LiteLX.exe`。终端使用 `build\LiteLX.Cli.exe`。

元数据、原始私有getter与写入分别有明确入口。风扇WMI和固件参数读取在本机需要管理员权限，可从管理员终端启动工具。GUI 启动时申请一次管理员权限，开机画面的后台执行器继承该权限。

开机画面页选择 BMP 后点击“保存”即可直接执行，操作说明常驻页面，结果回到主窗口显示。“恢复默认画面”使用同一后台执行流程。操作会直接修改 EFI 图片和固件设置，临时工作文件在操作结束时清理。当前已完成模拟验证，实机写入和重启显示仍待验收。

## 构建

在项目根目录运行下列命令。使用Windows已安装的.NET Framework编译器，无需.NET SDK和NuGet。

```powershell
.\build.ps1
.\build.ps1 -Test
```

目标为Windows x64、.NET Framework 4.x。GUI与CLI为x64，开机画面后台执行器为x86以匹配原DLL。源码注释使用中文。

构建产物为 `build\LiteLX.exe`、`build\LiteLX.Cli.exe`。开机画面后台执行器为 `modules\LenovoLogo\LogoOperationHost.exe`。移动程序时保留项目根目录下 `build` 与 `modules` 的相邻布局。

## 提取原厂模块

当前工作目录中已经完成提取。复现时使用下列脚本，原安装目录按只读源处理，目标已有不同文件时脚本会停止。

```powershell
.\tools\extract-logo.ps1
.\build.ps1
.\build\LiteLX.Cli.exe module list
```

开机画面模块包含 `AIToolAPI.dll`、本项目编译的 `LogoOperationHost.exe` 和 `manifest.json`。原生接口文件为260872字节，清单记录源路径和SHA-256。

## 常用CLI

```powershell
# Windows电量、驱动身份、设备映射和WMI类架构。
.\build\LiteLX.Cli.exe status

# 固件变量读取需要管理员权限。
.\build\LiteLX.Cli.exe logo status

# 离线BMP准备。宽高需核对目标固件，输出目录应为用户工作文件夹。
.\build\LiteLX.Cli.exe logo prepare 'C:\Images\logo.png' '.\prepared-logo' 2560 1600

# 只读列出右键菜单，可传过滤词。
.\build\LiteLX.Cli.exe menu list Lenovo

# 省略--apply时只显示计划。
.\build\LiteLX.Cli.exe menu disable '<扫描结果中的ID>' --apply
.\build\LiteLX.Cli.exe menu enable '<扫描结果中的ID>' --apply

# 风扇默认WMI FullSpeed。确认参数用于显式逻辑查询。
.\build\LiteLX.Cli.exe fan status --confirm-hardware-query
.\build\LiteLX.Cli.exe fan plan
.\build\LiteLX.Cli.exe fan run 5
```

风扇写入必须同时添加 `--execute --confirm-hardware`。这些命令会真实控制硬件，应在对应测试方案获得批准后运行。所有私有控制路径限定本次已分析的82TF和驱动哈希。

EnergyDrv旧式风扇路径可显式选择 `--energy`。与默认WMI的状态值和停止命令必须区分。首次真实测试使用的是当时默认的EnergyDrv版本，当前诊断命令应加该标志。

## 测试步骤

1. 执行 `build.ps1 -Test`。包含风扇与开机画面专用模拟测试，覆盖风扇取消、启停错误保留、开机画面保存和恢复默认、校验失败及回读失败后的关闭处理。
2. 执行 `status`，核对驱动身份和WMI架构。原厂程序、服务和驱动的配置在此步骤仅被读取。
3. 执行 `menu list`，核对菜单来源、位数、路径和警告。本机基线为4552项，机器软件变化后数量可能变化。
4. 用测试PNG执行 `logo prepare`，核对BMP分辨率与黑边，确认输出仅位于所选工作目录。
5. 打开GUI，检查三个页面及开机画面图片预览。实际固件保存、恢复和重启显示另行验收。
6. 实际菜单禁用/启用可先选一项用户明确指定的第三方命令，记录其ID，确认Explorer显示，再启用。`MenuSelfTest` 使用独立临时 HKCU 条目验证切换、不同来源和类型的禁用值、叠加隐藏状态，并检查操作前后备份文件列表一致。
7. 风扇WMI已有获批的5秒启停记录。新增硬件写入测试仍需制定含原始状态、回滚和回读的具体方案，批准后再执行。
8. 卸载后验证需用户另行批准。保留正常安装的联想ACPI/电源驱动包及提取的Logo模块，逐项重新验收。

## 文件组织

- `src/LiteLX` 为GUI、CLI、原生注册表与硬件适配器源码。
- `src/LogoOperationHost.cs` 为开机画面保存和恢复提供x86后台执行器。
- `modules/LenovoLogo` 为开机画面后台接口、执行器和校验清单。
- `analysis` 为分功能调查和实际运行证据。
- `docs` 为菜单细节与风扇测试方案、结果。
- `tools/HardwareSelfTest.cs` 为纯模拟回归测试。
- `tools/collect-readonly.ps1` 可复现文件、服务、驱动与WMI架构采集。
- `tools/pe_inspect.py` 为只读PE与机器码辅助分析，依赖本轮安装至 `tools/vendor` 的pefile与Capstone。工具运行本身仅依赖Windows与.NET Framework。

## 紧凑界面

主窗口默认640×520逻辑像素，最小600×480。开机画面页包含预览区、参数区和底部恢复/取消/确认操作。启动时显示上次读取的缓存图片，缓存缺失时提示选择BMP图片。选择BMP后立即替换预览，取消还原缓存预览。

点击保存后，后台执行器读取固件尺寸并等比例准备 BMP，本机为2560×1600。保存时停用自定义画面，直接替换图片及原厂校验信息，最后启用并回读。恢复默认画面会关闭自定义画面并清理自定义图片。写入阶段失败时尝试关闭自定义画面，结果回到主窗口显示。实机写入仍未进行新测试。

右键管理按名称、规范化命令与CLSID分组展示，保留每条原始注册位置。图标取自注册图标或应用资源，名称取自系统本地化资源、COM信息和文件产品说明。无法识别的扩展保留在全部注册项中，图标资源缺失时显示通用图标。开关直接读取和修改实际禁用状态，支持普通命令的禁用与隐藏值，以及当前用户和整机的扩展屏蔽项。切换时只回读相关注册位置和同一扩展的入口，原位更新开关，保留图标、分组与滚动位置。点击后立即播放160毫秒的滑动和底色过渡，失败时按实际回读状态回退。系统关闭界面动画时采用即时切换。

已验证三页渲染、图片替换预览、风扇模拟断言及64项开机画面模拟断言。Per-Monitor V2已启用，跨显示器实时缩放仍待现场验证。
