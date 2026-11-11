# 联想电脑管家四功能逆向与独立工具记录

调查日期为2026年11月10日。安装版本为5.1.210.10222，当前设备为 LENOVO 82TF。原安装目录为 `C:\Program Files (x86)\Lenovo\PCManager`。

## 当前交付状态

| 功能 | 机制与实现 | 当前验证程度 |
| --- | --- | --- |
| 开机画面 | 已提取原厂 LogoDiy 模块，自编 x86 WPF 宿主承载原窗口，GUI/CLI 可启动。另有 Windows 固件参数读取和离线 BMP 准备 | 原厂窗口在管理员权限下成功打开并关闭，显示2560×1600及BMP。保存/恢复写入尚未测试 |
| 右键菜单 | Windows 注册表原生枚举、禁用及基于本工具日志恢复 | 实机只读扫描4552项。真实菜单变更仍待测试。现代MSIX菜单适配未完成 |
| 风扇除尘 | 当前默认选 WMI FullSpeed。EnergyDrv 旧式后端保留为显式诊断选项 | 管理员WMI5秒启停与独立停止回读通过。EnergyDrv启停验证失败，详见风扇测试记录 |
| 电池三模式 | 自编 EnergyDrv 后端，原始协议复原，回读与部分失败恢复。另提取原厂综合设置页候选 | 电池纯模拟135项断言通过。真实查询raw=0x10820202，当前常规、快充支持。实际模式写入尚未测试，原厂候选启动尚未验证 |

无硬件测试合计170项断言通过。上述状态必须逐项解读，当前完整四功能的卸载后验收仍未完成。

## 证据范围

REA MCP 已实际使用，包含 PE 绑定、托管程序集元数据、CIL调用锚点、P/Invoke 边界和PE资源检查。原生提供程序返回 `provider_unavailable`，本机没有配置 Ghidra/IDA。原生机器码链由工作目录内的 pefile 2024.8.26 与 Capstone 5.0.9 只读解析补足。

REA记录位于 `analysis/evidence/rea-bundle.json`，补充的原生解析、CIM架构、文件清单和运行记录也在同目录。托管元数据与原生反汇编是发货制品证据，模拟设备测试验证控制逻辑。它们分别保留不同证据边界。

首次风扇测试按用户明确授权执行。随后三项WMI getter及原厂Logo窗口初始化也分别取得授权。固件保存/恢复、电池模式写入、卸载及驱动/服务停用均未列入本轮实际测试。

## 开机画面涉及的具体界面

截图入口为“开机画面定制”，配置 id 为 `lenovologdiy`。原入口通过 `LenovoToolMonitoring.exe 1041` 打开 `LogoDiy.dll`。

静态调用链如下。

```text
LogoDiyView / LogoDiyViewModel
  → GetLogoInfo / SaveLogoClick
  → AIToolAPI.dll.GetLogoDIYInfo / SetLogoDIYInfo / SetLogDIYCheck
  → Windows GetFirmwareEnvironmentVariableExW / SetFirmwareEnvironmentVariableExW
  → 固件变量与 EFI\Lenovo\Logo 图片
```

`SetImagePath` 和 `SaveLogoClick` 的 CIL 分别包含读取与写入 EFI 图片的实际调用。`ChangeEFIDisk` 使用 mountvol 挂载和解除系统分区。`SaveLogoClick` 构造文件名 `mylogo_{宽}x{高}`，调用文件复制及校验设置。加载圆圈选项使用 `bcdedit -set bootuxdisabled on/off`。

因此入口控制的是固件/EFI阶段的开机Logo，同时包含Windows启动加载圆圈选项。原厂管理员窗口已读出当前Logo，显示最大2560×1600、支持BMP。Windows文档确认固件变量接口所需权限为 SeSystemEnvironmentPrivilege，具体变量集合由固件决定。[Microsoft固件变量读取文档](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getfirmwareenvironmentvariableexw)

已复原变量 `LBLDESP` 使用命名空间 `{871455D0-5576-4FB8-9865-AF0824463B9E}`，结构10字节。偏移0为enable，偏移1为小端int32宽，偏移5为小端int32高，偏移9为格式掩码。`LBLDVC` 参与校验。自编工具仅实现该参数读取及离线图像准备，实际写入沿用已提取原厂UI，保存流程仍待单独验收。

核心证据包括 LogoDiy.dll SHA-256 `5f6efa18804da953408f98c6eda6058937a80537c17945e851434963eabc6e85`，REA成员记录 `ev_e2053eb3135c706b293eb734a414aff9008baabce1ad049bbe26a48633b2bc38`，P/Invoke记录 `ev_37c779c2a6e7278ee544674a50d0881261907e0fc1f1756cda304ed057fab58d`。AIToolAPI.dll SHA-256 为 `dbfdfd8d28e2580d8defdeee710f9bf5a251c3d2ef5ddc323aeeeb5198ebb5fc`，相关原生汇编为 `logo-native.asm` 和 `logo-firmware-read.asm`。

## 原厂模块提取

### 开机画面

`modules/LenovoLogo` 中保留原厂 LogoDiy.dll、AIToolAPI.dll、AICommon.dll、LogEncryption.dll、Newtonsoft.Json.dll 和Images/pc.png，原文件合计1570639字节。`VendorLogoHost.exe` 为本项目编写的 x86 WPF 宿主，直接创建原厂 `LogoDiy.LogoDiyView`。

这一选择避开通用宿主的其他插件初始化。原 `LenovoToolMonitoring.exe` 会动态读取PluginConfig、加载BatteryPolicyManager并初始化所有配置插件。单窗口宿主的源码位于 `src/VendorLogoHost.cs`，提取脚本为 `tools/extract-logo.ps1`。

厂商DLL保留原字节，manifest记录源路径、相对目标路径、大小和SHA-256。窗口初始化执行过固件读取、EFI图片读取及临时系统分区挂载流程，自动截图后正常关闭。磁盘列表检查前后均为C、D、E。原窗口普通权限下曾显示无效的负数高度，管理员权限下显示2560×1600并读出当前Logo，默认启动入口因此请求UAC管理员权限。

AICommon声明了相对路径DataPlugin.dll的遥测调用。相关clickEvent工作线程有异常处理，当前包中保留的是界面与硬件API必需集合。保存按钮之外的全部交互路径尚未覆盖，传递依赖最小性仍需更多场景验证。

### 电池

`modules/LenovoBattery` 已提取7个文件，共7329120字节。包括 BatterySetting.exe、WrapPlugin.dll、libmcfcamera.dll、libcurl.dll、libmcfcore.dll、VCRUNTIME140.dll、MSVCP140.dll。

无参数启动存在服务转发和本进程窗口回退两条路径。启动可能写 `batteryadmin` 注册表并向 LenovoPcManagerService 发送自定义控制码0x87，完整窗口还包括USB和ITS等额外入口。`-menu` 只显示导航菜单。当前将其作为候选包交付，GUI提供文件状态核对。实际启动、额外入口限制和卸载后独立性仍待验证。详见 `extraction-battery.md`。

### 右键与风扇

右键UI实际属于主进程CEF页面。ContextMenu目录中的DLL会构造 `LenovoPcManager.exe` 路径并通过ShellExecuteEx打开该页面。原业务后端 WSRMMgrPlugin 支持宿主候选路径，WSPluginHost中的independentprocess能绕过父进程等待，但IPC与初始化ABI仍未完全恢复。详见 `extraction-context-menu.md`。

风扇的当前UI也属于主进程页面。其WSHardwarePlugin与旧IdeaFanPlugin是插件后端，当前选择复原其硬件接口。原插件的存在本身未证明存在可独立启动的风扇GUI。

## 各功能依赖与卸载后判断

| 功能 | 已确认依赖 | 当前方案的最小保留组件 | 卸载后判断 |
| --- | --- | --- | --- |
| 开机画面 | 机型固件支持、EFI系统分区、固件变量权限。原厂UI采用.NET Framework WPF | 提取的Logo模块及自编x86宿主，Windows .NET Framework 4.x | 静态接口及独立窗口表明具备脱离主程序基础。实际保存与卸载后重启显示尚未验收 |
| 右键管理 | Windows Explorer注册表机制 | 自编LiteLX与Windows .NET Framework | 原生方案具有独立基础。卸载前应核对原软件迁移到unshell/unshellex的位置，恢复这些条目仍需专项处理 |
| 风扇除尘 | 本机WMI类与对应ACPI固件接口。旧式路径使用ACPIVPC/EnergyDrv | 正常安装的联想ACPI/电源驱动包，当前为oem130.inf。主GUI与自编WMI适配器 | WMI5秒启停已通过。卸载后运行尚未验收，旧式启停验证失败 |
| 电池三模式 | ACPIVPC、EnergyDrv、固件GBMD/SBMC | 正常安装的完整ACPIVPC签名驱动包及自编后端。原厂候选可另外保留 | 静态调用链具有独立基础。实际模式切换及卸载后驱动保留情况尚未验收 |

驱动最小保留单位是正常安装的INF、SYS、CAT及PnP关联。本轮没有进行驱动复制安装或重注册。当前驱动 AcpiVpc.sys 版本15.11.30.14，SHA-256 `c02b879e601c1689425bdd71ec6fc2ccf66d75436e973ab4f209d45473e58c95`，签名有效。工具的私有协议写路径限定82TF、该磁盘哈希和EnergyDrv设备映射。磁盘文件身份无法独立证明当前已加载内核镜像的身份。

LnCoolit、LnvDrvFdn及管家服务的运行状态已记录。LnCoolit的静态设备为独立LnCoolit设备，包含PCI/温度/物理内存逻辑。目前四功能必要依赖的证据集中在上表的接口与模块，其他后台组件的必要性尚未建立。

## 右键菜单实现边界

普通命令使用LegacyDisable，传统COM handler使用当前用户的Shell Extensions\Blocked。禁用前持久化恢复日志，记录条目来源视图、指纹和唯一标记，恢复时核对所有权和并发变化。Windows原生菜单扩展机制与COM handler注册结构有公开文档。[Microsoft菜单处理器文档](https://learn.microsoft.com/en-us/windows/win32/shell/context-menu-handlers)

本机只读扫描4552项，其中普通命令4132项、COM handler420项，未发现unshell/unshellex遗留。32/64位共享项可能重复显示，MSIX现代菜单与Explorer内置命令尚未完整枚举。日志与互斥锁处理本工具内的并发，最终检查与写入之间仍存在外部程序修改同一注册表值的竞态窗口。

## 电池参数与恢复

协议为IOCTL `0x831020f8`。输入FF走驱动GBMD查询，输入其他控制字节走SBMC设置。养护位为0x20，快充位为0x4，快充支持位为0x20000。养护启停为03/05，特定0x4000变体追加0D/0F。快充启停为07/08。

切换逻辑遵循原厂命令顺序，随后回读目标位，部分失败时尝试恢复原模式并核实结果。原插件初始化还会写注册表缓存，工具直接调用已经区分的设备查询和设置路径。完整静态地址与测试说明见 `battery.md`。养护阈值取决于固件，本轮没有把某个固定百分比写进控制逻辑。联想公开说明将ACPI驱动列为相关功能依赖，但未取得这些私有IOCTL的公开厂商协议文档。[Lenovo养护模式与ACPI驱动说明](https://datacentersupport.lenovo.com/in/en/products/solutions-and-software/solutions/ht516889-lenovo-vantage-conservation-mode-missing-windows)

## 风扇运行记录与后端选择

原厂当前WSHardwarePlugin有WMI优先、EnergyDrv回退路径。精确门槛使用 LENOVO_OTHER_METHOD.GetFeatureValue，回退还检查Get_Support_LegionZone_Version。早期按相近名称推断GetIfSupportOrVersion的门槛判断已经在静态复核中纠正。获批的UTILITY getter输出Data=0只作为原始观察记录。

用户批准的一次EnergyDrv周期在初始raw=1后启动阶段报错，提前停止。后续raw=0包含支持位丢失，首次CLI还丢失了内层异常。该缺口和停止判定修复详见 `docs/fan-test-result.md`。

随后经用户批准的管理员WMI查询取得 GetFanCoolingStatus Data=0、Fan_Get_FullSpeed Status=false，二者ReturnValue均为true。用户也确认风扇声音或转速恢复到测试前状态。

当前代码显式选择WMI FullSpeed接口。参数已与静态调用和本机架构核对。2026年11月10日19时05分的获批测试在启动后回读True，约5秒后停止并回读False，追加独立查询也为False，进程退出码0。CLI加 --energy 才选择旧式诊断路径。这属于本项目的明确后端选择，原厂本机最终自动选路仍需精确能力门槛的运行证据。

类限定符确认上述联想WMI类为dynamic，provider均为Windows的WmiProv。LENOVO_FAN_METHOD的GUID为 `{92549549-4bde-4f06-ac04-ce8bf898dbaa}`，GAMEZONE_DATA为 `{887B54E3-DDDC-4B2C-8B88-68A26A8835D0}`。PnP同时存在多个服务为WmiAcpi的ACPI\PNP0C14设备，包括GMZN、WM00等实例。具体类GUID到固件AML实现的完整绑定尚未提取。证据为 `wmi-provider-qualifiers.json` 和 `wmi-acpi-devices.json`。

风扇WMI路径的物理依赖与EnergyDrv路径应分别判断。当前构建额外保留ACPIVPC机型/驱动哈希门槛，属于本项目的兼容性约束，因此交付版本仍要求其正常安装。不能仅凭这一门槛认定WMI类由ACPIVPC提供。

正常取消与异常会请求停止，强制杀进程、进程崩溃或内核调用阻塞可能中断清理。当前没有独立看门狗。原厂名义18秒动画结束后也由客户端请求停止，尚无固件自行超时停止的证据。

## 尚待验收的项目

- 原厂Logo的保存、恢复、校验结构及重启显示。
- 提取的电池综合设置页启动副作用、独立性和入口范围。
- 电池三模式真实写入与恢复。
- WMI FullSpeed的更长周期、真实运行中取消及卸载后停止保障。
- 真实右键项的禁用/恢复、Blocked的Explorer实际效果及现代菜单适配。
- 用户另行批准卸载后，驱动保留与四功能完整运行验证。
