# 降温除尘的只读分析

## 调查范围与状态

本节记录安装版本5.1.210.10222、型号82TF的静态取证阶段。该阶段将原厂EXE、DLL、SYS作为字节读取。后续经用户批准的查询、启停及运行时结果见 `../docs/fan-test-result.md`，最终WMI5秒周期已经通过。以下汇编地址均为PE首选映像基址下的静态VA，实际加载地址可能重定位。

补充的类限定符确认provider为WmiProv，LENOVO_FAN_METHOD GUID为 `{92549549-4bde-4f06-ac04-ce8bf898dbaa}`。系统存在服务为WmiAcpi的多个ACPI\PNP0C14设备。完整GUID到固件AML绑定仍待分析，证据见evidence/wmi-provider-qualifiers.json与wmi-acpi-devices.json。

REA 已由主调查会话识别 IdeaFanPlugin.dll。REA 原生分析引擎未配置，当前原生调用链来自 pefile 2024.8.26 和 Capstone 5.0.9 的静态 PE/机器码解析。独立调用 REA inspect_pe_resources 时 MCP 回报工具不可用，此调用未产生证据 ID。托管 DissipateHeat.dll 的 REA P/Invoke 证据为 ev_0d494e2a252e97a9d302e621a1c0b171112a579a8d679424714aa2be6c4389cd，其 GetSmartFanMode / SetSmartFanMode 对应另一项智能散热调节。

## 入口与当前版本调用路径

已验证的配置入口为 skin/dist/json/defaultToolList.json 中 id=coolclean，名称为降温除尘，type=2，category=pcm_router，page=tools_coolclean。id=dissipateheat 对应智能散热调节，入口参数1035，不应合并。

当前版本 UI 的 main.js 通过 pcmHardWareModule.DoFanRun / DoFanStop 发出 PCM_MESSAGE_HARDWARE_FAN_RUN / PCM_MESSAGE_HARDWARE_FAN_STOP，请求模块为 WSHardwarePlugin。旧 IdeaFanPlugin 的 get-dustremovalpermission、do-dustremoval、cancel-dustremoval 接口也存在于安装包，但仅凭安装存在不能认定当前入口使用该旧插件。

WSHardwarePlugin.dll 的 SHA256 为 d9c6bfa4294ba02a83dcbacf905bf6c491113cb77d3b28d851143df2aa6dc986。其初始化函数 VA 0x1003c410 附近先请求 WMI 辅助对象，能力成功后记录后端2。失败时在 0x1003c4b9 调用 CreateFileW 打开 EnergyDrv，查询能力成功才记录后端1。

WMI 辅助对象内部有两个接口分支。VA 0x1003e317 使用 IDs=0x90000 调用 LENOVO_OTHER_METHOD.GetFeatureValue，读取输出 value。value 至少3且 GetFanCoolingStatus 可用时选择 LENOVO_GAMEZONE_DATA。否则要求 LENOVO_OTHER_METHOD.Get_Support_LegionZone_Version 的 Version 恰为2，再成功调用 LENOVO_FAN_METHOD.Fan_Get_FullSpeed，才选择该 fallback。WMI 辅助对象任何一条路径可用都会使上层优先使用 WMI。仅当整个 WMI 能力探测失败时，上层才尝试 EnergyDrv。

当前 82TF 的 root/wmi 已存在上述两类的架构，但本次未调用能力 getter，实际选中的后端仍未确定。

## WMI 接口

只读架构确认了以下方法及参数方向。

| 类 | 方法 | 参数 |
| --- | --- | --- |
| LENOVO_OTHER_METHOD | Get_Support_LegionZone_Version | Version UInt32 out |
| LENOVO_GAMEZONE_DATA | GetFanCoolingStatus | Data UInt32 out |
| LENOVO_GAMEZONE_DATA | SetFanCooling | Data UInt32 in |
| LENOVO_FAN_METHOD | Fan_Get_FullSpeed | Status Boolean out |
| LENOVO_FAN_METHOD | Fan_Set_FullSpeed | Status Boolean in |

WSHardwarePlugin VA 0x1003efc0 的分支2将目标布尔状态归一为 Data 0或1，调用 LENOVO_GAMEZONE_DATA.SetFanCooling，本机架构参数类型为 UInt32。分支1转入 Fan_Set_FullSpeed 的 Status Boolean 参数。相应 getter 用于读回状态。

这些名称和签名同时获得原厂调用机器码及本机 CIM 类架构支持。它们不是本次已取得公开厂商文档的接口，也没有进行运行时调用验证。原厂将 WMI full-speed 接口作为同一 UI 功能的可选后端是已验证事实。WMI full-speed 与旧式 dust 在固件中的电机策略完全相同，仍无法据此断定。

## EnergyDrv 旧式协议

IdeaFanPlugin.dll 的 SHA256 为 79ad4ad6b6f81794af72505424ec52d336c71f62ffb01b11ee155e785e22c734。旧插件与 WSHardwarePlugin 的 EnergyDrv 分支采用相同 IOCTL 与缓冲区布局。

| 操作 | IOCTL | 输入 | 输出 | 原厂证据位置 |
| --- | --- | --- | --- | --- |
| 支持与状态查询 | 0x831020c4 | 4字节小端 UInt32 14 | 4字节 UInt32 | IdeaFan 0x10001922、0x1000280a，WSHardware 0x1003c4ed、0x1003d96a |
| 开始除尘 | 0x831020c0 | 12字节小端 UInt32[6,1,1] | NULL，长度0 | IdeaFan 0x10002418 至0x1000243e，WSHardware 0x1003d19e 至0x1003d1c4 |
| 结束除尘 | 0x831020c0 | 12字节小端 UInt32[6,1,0] | NULL，长度0 | IdeaFan 0x10001fa8 至0x10001fce，WSHardware 0x1003d26f 至0x1003d295 |

原厂 CreateFileW 使用 GENERIC_READ|GENERIC_WRITE，共享读写，OPEN_EXISTING，访问目标为 \\.\EnergyDrv。此为历史调用事实，不能将所需打开权限解释为调用后的固件无副作用。

原厂状态解码位于 WSHardware VA 0x1003d940。

- IOCTL 失败或 bit0为0，返回枚举7。
- bit0为1且 bit1为1，返回枚举1，原厂据此作为正在运行。
- bit1为0且 bit2为1，返回 2 | ((~(raw >> 3)) & 1)。bit3为1时得到2，否则得到3。
- bit1为0且 bit2为0，返回枚举4。

bit0支持、bit1运行的含义已由机器码控制流及调用上下文确认。bit2、bit3和枚举2、3、4的业务名称尚未恢复，因此本次不把所有 bit1为0的状态认定为已经过验证的可启动状态。

## 设备与驱动映射

当前 ACPIVPC 服务对应文件 C:\Windows\System32\DriverStore\FileRepository\acpivpc.inf_amd64_07db38bc9e02b27d\AcpiVpc.sys，服务处于 Running、按需启动。PnP 显示 Lenovo ACPI-Compliant Virtual Power Controller，INF=oem130.inf，驱动版本15.11.30.14。INF 将 ACPI\VEN_VPC&DEV_2004 关联至 ACPIVPC，并安装 AcpiVpc.sys。

该 SYS 中 UTF-16 字符串文件偏移0x6a10为 \\Device\\EnergyDrv，0x6a40为 \\DosDevices\\EnergyDrv。实际字符串采用单反斜线设备路径。静态映射与服务文件形成了对应关系，仍需区分磁盘驱动文件与当前已加载内核镜像。

驱动 SHA256 为 c02b879e601c1689425bdd71ec6fc2ccf66d75436e973ab4f209d45473e58c95。Get-AuthenticodeSignature 报告 Valid，签名主体为 Microsoft Windows Hardware Compatibility Publisher。

LnCoolit.sys 的 SHA256 为 763ed660fb01e1e50916d39115a61ef657a7a4fb9a834dc8355d37762111a46e。该文件声明另一设备 LnCoolit，字符串包含 PCI 配置读取、AMD CPU 温度、PhysicalMemory。当前除尘核心链没有调用 LnCoolit 的证据，不能把它列为除尘最小依赖。

## 查询到 ACPI 的底层边界

当前 AcpiVpc.sys 的 VA 0x14001120d 将查询 IOCTL 分派到0x140011258，输入14在0x140011352映射为命令字节0x2b。随后通过0x140010ffc进入驱动队列，结果作为 UInt32 返回。设置 IOCTL 分派至0x1400113ab，控制类型6在0x1400113df映射为命令0x22，数据为输入第三个 DWORD 的低字节。

驱动通用协议引擎0x140010c7c会对查询也先通过 VPCW 提交命令码，再通过 VPCR 接收结果。ACPI 调用助手0x14000fa98中 r9=0选择 VPCW，r9=1选择 VPCR，并向下层发送 IOCTL_ACPI_EVAL_METHOD。因而逻辑 getter 不等于底层绝无写入。静态分析可区分0x2b状态请求与0x22风扇设置请求，但未分析固件 AML，无法证明 getter 在所有固件上的全部副作用。

本轮默认探测限于文件、服务配置、注册表和 CIM 架构。包含逻辑 getter 的任何后续测试方案应明确列出请求内容。

## 生命周期

main.js 在界面挂载时开始除尘并播放一次 coolcleanstart.svga。该资源 zlib 解压后的 protobuf 参数为 fps=30、frames=540，因此动画名义时长为18秒。onFinished 调用 finish(false)，finish 在前端 isRunning 为真时调用 DoFanStop。返回按钮调用相同 finish。pcmClosedNotify 也在 isRunning 时请求 DoFanStop 后关闭窗口。

前端 isRunning 在发送启动请求前就设为 true，因此不能作为硬件已经启动的证据。静态链证明客户端承担停止请求，未获得固件自动超时的证据。18秒来自动画资源，实际调度时长可能受运行环境影响。

独立实现应拒绝接管已在运行的其他除尘周期，使用跨进程锁，在启动失败不明确、用户取消、正常到时和窗口关闭路径都处理停止及回读，并报告停止失败。普通 finally 不能覆盖进程崩溃、系统强制结束等情况。5秒试运行属于人为选取的缩短测试周期，未在本轮执行。

## 卸载后可用性与最小保留组件

| 路径 | 最小候选组件 | 卸载后判断 | 尚待验证 |
| --- | --- | --- | --- |
| 原生 WMI 重实现 | Windows WMI/ACPI 栈、固件导出的对应类及适配机型所需 OEM 驱动 | 不必依赖原厂 UI、插件宿主或其常驻服务，按当前静态调用推断可独立运行 | 能力返回值、参数执行结果、卸载是否影响设备驱动与类 |
| EnergyDrv 重实现 | 已验证版本的 ACPIVPC 驱动包 AcpiVpc.inf/cat/sys、适配固件、Windows ACPI 栈 | 调用链无须保留 IdeaFanPlugin/WSHardwarePlugin/管家服务，静态上可独立实现 | 当前机型实际支持位、权限、原始状态、5秒启停及回读 |
| 直接搬运原厂插件 | 插件及其动态依赖、宿主和原有接口约定 | 依赖面更大，不作为当前轻量实现方案 | 动态加载和宿主状态尚未恢复完整 |

此处的最小组件为实现候选边界，没有经过卸载实验。用户明确禁止本轮卸载、停用驱动和服务，因此无法报告实测卸载后可用。

当前工具若选择固定机型和驱动哈希的 EnergyDrv 后端，应标注为经静态复原的旧式协议实验后端。该选择与原厂现版优先 WMI 的运行策略存在差异。

## 证据索引

- evidence/fan-pe.json 保存三项 PE 的身份、导入导出和带文件偏移的字符串。
- evidence/fan-xrefs.json 保存 IdeaFan 的字符串引用与 IOCTL 参数附近机器码。
- evidence/fan-ui.json、fan-ui-all.json 保存当前资源中可检索到的 UI 调用片段及字符偏移。
- evidence/fan-hardware-pe.json、fan-hardware-xrefs.json 保存 WSHardware 的静态证据。
- evidence/fan-wmi-disassembly.json、fan-wmi-disassembly-more.json 保存 WMI 分支和状态解码的静态反汇编。
- evidence/fan-wmi-schema.json 保存 root/wmi 类和方法架构，仅有元数据读取。
- evidence/fan-driver-xrefs.json、fan-driver-disassembly.json、fan-driver-helper.json、fan-driver-acpi.json、fan-driver-worker.json 保存当前 AcpiVpc 的分派和协议引擎反汇编。
- evidence/fan-lifecycle.json 保存动画参数与计算结果。
- evidence/fan-host.json 保存资源、驱动、INF 的 SHA256、文件长度、驱动服务与签名检查结果。

未决项包括当前82TF运行时后端选择、完整固件AML副作用、状态bit2/3的业务语义、崩溃恢复保证、实际卸载后驱动保留状态以及实际启停效果。

## 启动失败后的只读时序复核

本节仅复核静态文件与当前源代码，不增加硬件查询、启停或其他控制操作。

原厂 IdeaFanPlugin 在 VA 0x1000243e 调用设置 IOCTL，返回 TRUE 后先写日志，再于0x10002473调用状态 getter。WSHardwarePlugin 的对应位置为0x1003d1c4与0x1003d1f9。两条核心路径之间均没有显式 Sleep、定时器或状态重试循环。原厂停止路径同样在 IOCTL 成功后立即查询状态。

核心返回值是状态枚举。旧插件默认返回7，IOCTL 成功则返回状态 getter 的枚举。当前 WSHardware 核心0x1003d150也是相同形式。上层请求处理函数0x1003e9b0调用0x1003eaf0后没有检验其枚举结果，而是构造固定响应 {"result":"OK"}，并于0x1003ea2f无条件返回1。停止请求处理函数有相同模式。因此请求处理层 TRUE/OK不能作为风扇运行bit1已确认置位的证据。

当前 LiteLX.RunFan 要求设置后首次回读的bit1必须置位，否则立即执行清理停止。这个成功条件比当前原厂请求处理层严格。静态文件不能证明是否需要添加额外延迟，也不能以原厂界面的动画已经播放认定启动成功。

AcpiVpc 的通用队列助手0x140010ffc先排入请求、驱动队列引擎，然后调用 KeWaitForSingleObject 等待请求完成。工作引擎0x140010c7c有 VPC 协议状态轮询及 KeDelayExecutionThread 延迟，完成后才设置请求结果并唤醒等待者。因此设备句柄未开启 OVERLAPPED 时，DeviceIoControl 成功表示该请求已完成驱动协议处理。它不直接证明风扇转速已完成物理爬升，也未证明固件所暴露的运行状态位必须在同一时刻更新。

对失败原因的边界判断如下。

- 如果原始异常是 Win32Exception，则属于 IOCTL 调用或驱动返回失败，添加 getter 延迟无法解释该异常。
- 如果原始异常是启动后回读仍为关闭，则设置 IOCTL 返回成功但首次状态未满足工具成功条件。可能包括固件状态延迟、旧式设置路径未实际生效、另一服务覆盖、模型或接口差异。以上均为推测，本次没有足够运行日志区分。
- 当前源代码的 FanSet 输入12字节 UInt32[6,1,1]、停止输入[6,1,0]、设置输出NULL/0和 getter 输入输出长度，与所分析的原厂调用一致。未发现这部分缓冲区布局错误。
- 查询 raw=1只能确认此 getter 所报告的支持位和当次未运行状态，不能证明 WMI 优先能力探测会选择同一个后端，亦不能保证旧式 setter对当前固件可用。

新增证据为 evidence/fan-success-timing.json 和 evidence/fan-handler-response.json。单次试运行结果及其日志应由主调查报告保存，本节没有独立复现。


## WMI 选择条件与返回值的精确复核

本节通过现存 PE 与已保存架构完成，额外 CIM 查询仅获取类方法元数据，没有调用任何 WMI 方法或硬件命令。

需要纠正早期调查中相近版本接口的推断。原厂当前 WSHardware 分支使用 LENOVO_OTHER_METHOD.GetFeatureValue(IDs=0x90000)，输出键为小写 value。它不是 LENOVO_UTILITY_DATA.GetIfSupportOrVersion。字符串位置分别为 VA 0x100c635c、0x100c63f0、0x100c63e8和0x100c6410。对应调用参数构造位于0x1003e54b至0x1003e60a，读取 value 位于0x1003e6cd至0x1003e6d8。本机82TF的 LENOVO_OTHER_METHOD 架构未出现 GetFeatureValue，因此当前机器不能仅按该分支名称假定可调用。

原厂 WMI 能力选择有以下条件。

1. 在0x1003e317设置 IDs=0x90000。GetFeatureValue 的 WMI 调用成功且 value 存在，并且0x1003e32a比较 value 大于等于3，才继续测试 GameZone getter。
2. GameZone.GetFanCoolingStatus 调用成功且 Data 输出存在即可选择此后端。Data为0表示当前没有运行，不会使能力探测失败。VA 0x1003e336至0x1003e346将 getter 函数成功结果设为后端2。
3. 上述任一能力条件失败后，调用旧式 WMI 能力探测0x1003dc80。它要求 LENOVO_OTHER_METHOD 的 WMI 连接与实例初始化成功，Get_Support_LegionZone_Version 调用成功，输出 Version 存在，且 Version 恰好等于2。比较在0x1003de10。
4. 还必须成功调用 LENOVO_FAN_METHOD.Fan_Get_FullSpeed 并取得 Status 输出。当前 Status 是关闭也可以通过能力探测，0x1003de24至0x1003de40只检查 getter 函数是否成功。
5. 上层在两种 WMI 能力分支均失败时才尝试 EnergyDrv。

GameZone getter 0x1003dab0的成功值与状态值需要分开。函数成功需 WMI 调用成功且输出字典中存在 Data，0x1003dc27返回函数成功值1。实际开关值在0x1003dbfe精确比较 Data==1，仅等于1才为true，其余UInt32值均为false。

FullSpeed getter 0x1003dec0读取 Status 对应 VARIANT 的16位布尔值，0x1003e02e至0x1003e032直接复制。上层0x1003da67至0x1003da6c按非零判断运行。布尔原始表示可能为 VARIANT_TRUE的-1，应按布尔处理。

SetFanCooling 的输入在0x1003eff9至0x1003f00a由请求布尔归一成0或1，键为 Data，调用在0x1003f0bc。输入初始 VARIANT 为VT_UI4，经过 VariantChangeType 转VT_I4用于原生字典传递。本机 CIM 架构已声明 Data为UInt32，C#应按该架构提供 uint值。FullSpeed setter 的参数为Boolean Status。

公共 WMI 执行器0x1003d390通过 IWbemServices.GetObject、IWbemClassObject.GetMethod、SpawnInstance及Put准备参数，再于0x1003d7ae调用 IWbemServices.ExecMethod。返回HRESULT保存在0x1003d7b0，并于0x1003d90f按非负判断调用成功。执行器会枚举所有输出属性，但风扇消费者仅查 Data、Status、value或Version，没有依据 ReturnValue 判断是否成功。当前本机这些方法的 ReturnType均为Boolean，不能套用所有WMI方法都以数值 ReturnValue==0成功的规则。ReturnValue的业务语义本轮未恢复，应保留在诊断结果中，并区分调用异常与状态回读。

最小 C# 实现可使用 System.Management 的实际实例对象。先读取方法架构判断 GetFeatureValue 是否存在，按原厂分支门槛选择后端。对选定的实际实例调用 GetMethodParameters，设置声明的输入字段，再 InvokeMethod。HRESULT失败会以 ManagementException/COMException 等异常体现。调用成功后检查所需输出属性存在且类型符合架构，再执行上述 Data或Status解码。setter执行后由对应 getter单独确认实际状态。避免把类别对象的静态调用能力与原厂实际使用的实例路径混同。

新增证据为 evidence/fan-wmi-exact.json、fan-wmi-exact-strings.json、fan-wmi-invoker.json和fan-wmi-exact-schema.json。

补充核实发现 Active 属性读取不是原厂能力门槛。legacy fallback 在0x1003dd22读取 Active，调用后的返回值和局部属性值都未用于条件分支。FullSpeed getter也有同类读取。独立实现若按 Active 筛选实例，应标为自身的实例选择策略，并保留多实例歧义检查。该处证据见 evidence/fan-wmi-active-filter.json。
