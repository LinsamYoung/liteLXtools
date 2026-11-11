# BatterySetting.exe 原子模块提取可行性

2026-11-10 对 PCManager 5.1.210.10222 的静态调查与后续指定文件提取。分析阶段只读检查原安装目录，提取阶段仅复制指定文件到工作区并校验。原厂模块启动与硬件运行验证待确认。

## 当前结论

BatterySetting.exe 具有自己的 GUI 实现和服务失败后的本进程启动路径。它可以列为独立原厂模块候选。已确认的普通 PE 加载依赖下界是 BatterySetting.exe 与同目录 WrapPlugin.dll，两者合计约 5.65 MiB。把 PE 延迟导入一起纳入传递闭包后，候选共有 7 个本地文件，合计 7329120 字节，约 6.99 MiB。电池三模式控制本身使用系统已有的 EnergyDrv/ACPIVPC 驱动。

完整运行闭包仍包含条件动态加载和管家初始化逻辑，本轮静态检查尚不能把这个两文件候选标为“卸载后已验证独立运行”。界面同时包含 USB 供电、电源策略、ITS/智能散热等其他功能。原样提取会保留这些入口。独立运行及入口限制均需后续核对。

## 文件身份

| 文件 | 字节数 | SHA-256 |
| --- | --- | --- |
| BatterySetting.exe | 2713936 | 05ea2a5407ee2ce919e71982861454535e736129ac0bbb2d9f7e8f01c99b1acd |
| WrapPlugin.dll | 3211088 | 806f7d5f76fda7921089bad2469976e7754df99ed6f2b0f0ccc26539f41e68ab |

二者均为原生 x86 PE。BatterySetting.exe 的 REA 原始记录为 ev_cb1c1de87a4f279125839a35779a5fdbd6360ac2c19782b1d35b3d98305bea47。加载和功能调用分析采用 pefile/Capstone，本轮 REA 原生反编译提供程序未配置。

## 入口与服务分支

PE entry 为 0x51c6f6。CRT 初始化后 0x51c66d 调用 0x4315a0，也就是本程序的 WinMain。

- 命令行包含 `restoresetting` 时转到 0x431bd0 执行恢复设置逻辑，其中可调用设置函数。该分支有写入风险。
- 命令行包含 `-menu` 或 `/menu` 时进入电池菜单/弹出页分支，经 0x431855 构造菜单对象，0x43186b 进入窗口调用。重复启动会向已有 BatteryMenu 窗口发消息。
- 不带这些参数时进入 0x4318ea。该分支读取 `HKCU\Software\Lenovo\PcManager\batteryadmin` 和管理员状态。在相应条件下将 batteryadmin 写为 1，打开服务管理器，打开 LenovoPcManagerService，随后发送 ControlService 控制码 0x87。
- 发送服务控制成功时，本次进程结束，显示工作可能由服务派发。控制码处理端本轮未分析，不能解释为停止服务，不能把它当作只读消息。
- 服务不存在、打开失败或控制失败时，明确继续到本进程回退路径。0x431a2c 写 batteryadmin=0，0x431b30 调用 0x424dc0 构造 CBatterySetting，0x431b44 调用 0x426ec0 进入其 UI 路径。因此管家服务存在转发分支，但服务并非这个窗口的唯一执行宿主。

以上地址、导入和字符串引用在 evidence/battery-extraction-main-assembly.txt、battery-extraction-start-assembly.txt、battery-extraction-xrefs.json 可复核。

## 可复核的启动形式

下列形式由入口命令行分支确认，尚未实际运行。将工作目录设为模块目录，可以使相对加载路径按原布局解析。

```powershell
# 候选完整设置窗口，运行前需要确认其初始化副作用。
$moduleRoot = 'E:\LinsamYoung\Desktop\Desktop Files\liteLXtools\modules\LenovoBattery'
Start-Process -FilePath (Join-Path $moduleRoot 'BatterySetting.exe') -WorkingDirectory $moduleRoot

# 原厂导航菜单分支，用于设备状态、电池信息、设置中心导航。
Start-Process -FilePath (Join-Path $moduleRoot 'BatterySetting.exe') -ArgumentList '-menu' -WorkingDirectory $moduleRoot
```

上面的命令作为后续验证的审阅材料。`restoresetting` 应保留为明确的写操作。

## 加载依赖与动态闭包

BatterySetting.exe 的正常导入表中唯一厂商 DLL 为 WrapPlugin.dll。其余为 Windows 系统 DLL 和 API-set DLL，包括 USER32、GDI32、ADVAPI32、SHELL32、OLE32、COMCTL32、GDIPlus、POWRPROF、SETUPAPI 等。本次 Windows 上加载器层面没有发现它静态导入管家主进程、管家服务或 PCMBatterySettingsPlugin.dll。

WrapPlugin.dll 的正常导入表也是 Windows 系统 DLL/API-set。WrapPlugin.Initialize 导出位于 0x1000a1b0，其函数体只是 `xor eax,eax; ret`。不能仅凭导出名推断它会执行初始化。

普通导入与 PE 延迟导入的本地文件闭包如下。哈希、逐项依赖和 x86 架构字段已保存于 evidence/battery-extraction-pe-closure.json。

| 文件 | 字节数 | 依赖关系 |
| --- | --- | --- |
| BatterySetting.exe | 2713936 | 普通导入 WrapPlugin.dll，延迟导入系统 VERSION.dll |
| WrapPlugin.dll | 3211088 | 延迟导入 libmcfcamera.dll 和 libcurl.dll |
| libmcfcamera.dll | 51744 | 普通导入 libmcfcore.dll 和 VCRUNTIME140.dll |
| libcurl.dll | 708360 | 此闭包中其余导入由 Windows 提供 |
| libmcfcore.dll | 129568 | 普通导入 MSVCP140.dll 和 VCRUNTIME140.dll |
| VCRUNTIME140.dll | 67584 | 此闭包中其余导入由 Windows 提供 |
| MSVCP140.dll | 446840 | 普通导入 VCRUNTIME140.dll |

延迟导入可能仅在特定功能被调用时加载。当前机器实际安装的 x86 VC 运行库可能满足部分运行库项，便携候选闭包将原安装目录内匹配的两个 VC 运行库一起列入。7 文件闭包仍需另行覆盖显式 LoadLibrary 调用。

动态加载需要按功能分支区分。

| 相对组件 | 已观察到的证据 | 对两文件候选的影响 |
| --- | --- | --- |
| Modules/ThinkPowerPlugin.dll | WrapPlugin.LoadPowerPlugin 经 0x10105180 调用 LoadLibraryW。缺失时直接返回，QueryChargeThreshold 的空函数指针分支返回 false | ThinkPad 电池扩展的条件依赖。本机 82TF 是否进入所有相关分支尚未实际运行 |
| DataPlugin.dll | 主启动 0x431a6c 调用 0x4c4370，后者经 0x4c3f90 尝试加载。缺失返回 0 后走统计初始化备用路径 | 该启动路径忽略统计初始化返回值，仍继续创建 UI，因此该路径可降级 |
| plugins/IdeaFanPlugin.dll | EXE 的风扇控制入口包含路径及动态加载封装 | 原厂窗口同时带除尘入口，其是否显示取决于功能配置。电池模式代码直接操作 EnergyDrv |
| ludp.dll | 统计初始化备用路径 0x4c4420 拼接程序目录下 ludp.dll。加载或签名检查失败时返回 -1 | 主启动忽略该返回值并继续，因此该路径可降级 |
| configs 等 | EXE 有语言/产品配置相关代码及嵌入默认配置 | 外部配置可能影响功能呈现，实际展示效果待运行核对 |

相关可降级分支已保存于 evidence/battery-extraction-dynamic-assembly.txt。WrapPlugin.dll 中还包含大量其他产品功能的动态组件字符串，不能把所有字符串都加入最小闭包。完整动态依赖确认需要限定启动分支并采集实际模块加载和文件访问。本轮未执行这些启动观察。

## 界面资源

EXE 自带 ZIPRES 资源，资源名称 3027 的 ZIP 负载为 420529 字节，含 main.xml、Menu.xml、mainnocharge.xml、main_Threshold.xml 及图片。资源在内存中只读解包检查，没有向安装目录写入。界面 XML 证据保存在 evidence/battery-extraction-ui.json。

main.xml 确有正常模式、养护模式、快充模式按钮，同时有 USB持续供电、电源模式、ITS模式、智能温控、降温除尘等控制。Menu.xml 则有设备状态、电池信息、设置中心等项。因此这个可执行文件的 UI 范围超过用户要求保留的三个电池模式。

进一步核对 `-menu` 分支后，已确认它构造 CBatteryMenu。构造器 0x41de60 设置虚表 0x58876c，该类皮肤函数在 0x421059 引用 Menu.xml，窗口类名函数 0x421090 返回 BatteryMenu。`-menu` 直接显示设备状态、电池信息、设置中心的导航菜单，三模式控制由无参数完整窗口的 main.xml 提供。资源关联汇编保存在 evidence/battery-menu-resource-assembly.txt。

## 副作用与下一步验证边界

启动路径已确认写 batteryadmin 注册表，可能向仍运行的 LenovoPcManagerService 发送自定义控制。此前电池公共 helper 的初始化已确认会创建和更新 BatteryChargeMode 缓存注册表。普通显示分支下其他硬件初始化是否全部只读尚未完成调用闭包证明，所以不能在当前“只读分析”授权下直接试启动原程序。

7 文件 PE 闭包可作为后续提取实验的起点，保持哈希和原有相对目录。验证需确认允许启动原厂模块及其初始化副作用，再在已提取目录观察实际模块加载/缺失文件并核对三模式控制。它还需要在系统正常注册的完整 ACPIVPC 驱动包。卸载后的运行结论须由该验证结果给出。

当前提取状态为静态候选，独立运行验证待确认。

## 已执行的提取

已按具体提取指令运行 tools/extract-battery.ps1，仅复制上述 7 个文件到 modules/LenovoBattery，复制前和复制后逐一验证 SHA-256，并再次运行脚本核对可重复提取。模块总大小为 7329120 字节。modules/LenovoBattery/manifest.json 为数组，逐文件记录 Source、RelativePath、SHA256、Bytes。原厂程序运行验证仍待后续确认。
