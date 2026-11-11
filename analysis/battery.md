# 电池充电模式只读逆向记录

核对日期为2026-11-10。目标版本为PCManager 5.1.210.10222，主机型号为LENOVO 82TF。本记录主体为静态取证阶段，地址均为PE首选映像基址上的静态VA。后续自编工具仅执行了电池FF逻辑查询，返回raw=0x10820202，养护与快充均关闭、快充支持位为1，证据为evidence/battery-live-query.txt。电池模式写入尚未执行。

## 二进制与 REA 证据

| 文件 | SHA-256 | REA 记录 |
| --- | --- | --- |
| plugins/PCMBatterySettingsPlugin.dll | fbcadf58c28cb0df2296a0931fed99ee56599423be23a075503427355464b51a | ev_f5c724d319212339a8eb8e85ddf83c41a916c0d8355e1325e316efdd09655d9b |
| BatterySetting.exe | 05ea2a5407ee2ce919e71982861454535e736129ac0bbb2d9f7e8f01c99b1acd | ev_cb1c1de87a4f279125839a35779a5fdbd6360ac2c19782b1d35b3d98305bea47 |
| GameSettingsPlugin.dll | 7e1a22635a0b0d8fed5196b2ae4ca36f204e77eff7e42a0ef35f8d2676a55316 | ev_1c30a14e7a4cc4f51eb6a9f93c1b519a548dfcdbaeb07626244b25797b35464e |
| DriverStore/acpivpc.inf_amd64_07db38bc9e02b27d/AcpiVpc.sys | c02b879e601c1689425bdd71ec6fc2ccf66d75436e973ab4f209d45473e58c95 | 本地 pefile/Capstone 静态证据 |

REA 确认前三者均为原生 x86 PE，未包含 CLI 元数据。本机 REA 的原生提供程序没有可用 Ghidra/IDA 配置，因此后续为 pefile 2024.8.26 和 Capstone 5.0.9 的静态解析。完整导入、导出、字符串、立即数交叉引用位于 evidence/battery-*.json。关注范围的汇编位于 evidence/battery-control-assembly.txt 和 evidence/battery-mode-assembly.txt。驱动汇编位于 evidence/battery-acpivpc-assembly.txt。

## 已验证的调用路径

1. PCMBatterySettingsPlugin.dll 导出 pcm_plugin_action 等管家插件入口。其命令字符串包含 GET_PCM_SUPPORT_MODE、GET_PCM_CHARGE_MODE、SET_PCM_CHARGE_MODE、GET_QUICKCHARGE_SUPPORT、GET_QUICKCHARGE_STATE、SET_QUICKCHARGE_STATE，均带 NEWPLUGIN_PCMBATTERYSETTINGSPLUGIN_ 前缀。
2. 插件通过 CreateFileW 打开 `\\.\EnergyDrv`，访问掩码为 0xc0000000，分享掩码为 3，OPEN_EXISTING。读取和写入共用 DeviceIoControl 0x831020f8，输入为 1 字节，输出缓冲区为 4 字节。通用设备调用函数位于 0x10002bd0。
3. QueryDosDeviceW 的只读结果为 `EnergyDrv -> \Device\EnergyDrv`。已运行服务为 ACPIVPC，其驱动来自 Windows DriverStore。该驱动包含 `\Device\EnergyDrv` 和 `\DosDevices\EnergyDrv`，并有 IoCreateDevice、IoCreateSymbolicLink 调用。设备创建位于 0x1400105e2，符号链接创建位于 0x14001071b。
4. AcpiVpc.sys 的 0x140011609 比较 IOCTL 0x831020f8，并跳到 0x1400117e6。处理程序要求输入至少 1 字节、输出至少 4 字节。0x140011801 区分输入 0xff 与其他输入。
5. 输入 0xff 调用 0x14000240c，以 ACPI_INPUT_BUFFER 的 GBMD 方法、IOCTL_ACPI_EVAL_METHOD 0x32c004 调用下层 ACPI 驱动，将整数输出存入设备扩展偏移 0x148 并返回。方法名 GBMD 的原始立即数为 0x444d4247，地址为 0x140002477。
6. 其他输入调用 0x1400100b8，以 ACPI_INPUT_BUFFER_SIMPLE_INTEGER 的 SBMC 方法、IOCTL_ACPI_EVAL_METHOD 0x32c004 调用下层驱动。输入字节被零扩展成整数参数。方法名 SBMC 的原始立即数为 0x434d4253，地址为 0x140010136。

上面的读写分离由驱动分支和参数构造共同确认。GBMD/SBMC 的固件内部实现未提取，静态读路径不能覆盖固件实现缺陷。

返回长度也可从驱动确认。0x14001116c 将 IRP.Information 初始化为 0。GBMD 查询成功后在 0x14001139c 写回 uint32 并设置 Information=4。SBMC 写入完成后经 0x140011493 仅保存下层 NTSTATUS，Information 保持 0。独立实现应要求查询返回 4 字节，允许写入成功时返回 0 字节，并用后续查询验证目标状态。原厂写路径只检查 DeviceIoControl 的 Win32 BOOL，没有解析输出缓冲区业务值。驱动 SBMC 封装也没有解析 ACPI 方法返回整数，仅透传 ACPI 传输完成状态。

## 本版本参数映射

| 项目 | 参数或位 | 静态锚点 |
| --- | --- | --- |
| 读取原始状态 | 输入 0xff，输出 uint32 | 插件 0x100036c0、0x100038d0，驱动 0x140011801 |
| 养护状态 | raw & 0x20 非零为启用 | 插件 0x10003945 至 0x10003951 |
| 快充状态 | raw & 0x4 非零为启用 | 插件 0x10003735 至 0x10003741 |
| 快充支持 | (raw >> 17) & 1 | 插件 0x10004765 至 0x1000476b |
| 特定养护变体 | raw & 0x4000 | 插件 0x10001fb9，决定内部对象标志 |
| 养护启用 | 输入 0x03，变体追加 0x0d | 插件 0x10005640 至 0x10005705 |
| 养护关闭 | 输入 0x05，变体追加 0x0f | 同上 |
| 快充启用 | 输入 0x07 | 插件 0x10005470 至 0x10005509 |
| 快充关闭 | 输入 0x08 | 同上 |
| PCM 层常规模式 | 整数 0 | 插件 0x10005310 的分派及 0x10004c50 的复位分支 |
| PCM 层养护模式 | 整数 1，缓存字符串 Storage | 插件 0x10005353、0x10005393，字符串 0x10024880 |
| PCM 层快充模式 | 整数 2，缓存字符串 Quick | 插件 0x1000543d、0x10004e70，字符串 0x100248d8 |

读取模式时，0x10002f90 先检查养护，再检查快充，两者都关则常规。插件内部状态函数返回 1 表示启用，2 表示未启用，0 表示查询失败。独立实现应保留原始输出和错误，查询失败不能映射成常规模式。

原软件切换养护时先启用养护，再在原模式为快充的情况下等待 80ms 后关闭快充。切换快充时先启用快充，再在原模式为养护的情况下等待 80ms 后关闭养护。常规模式关闭原先启用的模式。多条命令中途失败后的固件状态尚未真机验证，独立实现需要在失败时报告部分完成和重新读取结果，不能将 Win32 IOCTL 传输成功直接当作最终模式正确。

## 注册表副作用与读取边界

`HKCU\SOFTWARE\Lenovo\iMController\Contracts\SystemManagement.BatteryMgmt\BatteryChargeMode` 保存字符串 Storage 或 Quick 等缓存。还存在 `HKCU\SOFTWARE\Lenovo\iMController\PluginData\IdeaNoteBookPlugin` 的兼容镜像路径。

插件单例初始化最终调用的 0x10003af0 附近包含 RegCreateKeyExW 与 RegSetValueExW 路径，写值封装为 0x10005510。GetBatteryChargingMode 对应的 0x10002f90 首先调用单例初始化 0x10003210。因此直接加载原插件并调用看似读取的入口会带来初始化注册表写入。只读调查和独立工具状态查询应直接使用已区分的 GBMD 设备查询路径。

只读读取到的注册表缓存为 Quick。该缓存只可作为软件历史信息，不能证明硬件当前模式。

## WMI 与公开文档

本机 `root\wmi:LENOVO_GAMEZONE_DATA` 具有 GetPowerChargeMode，Data 为 UInt32 输出。本轮只检查方法架构。GameSettingsPlugin.dll 的 GetPowerChargeMode 导出位于 0x1002fbd0，包含 root\wmi 及 GetPowerChargeMode 字符串。没有发现把它与上述 GBMD 三模式精确等同的静态证据，因此应单独记录其原始语义待证。

联想公开知识库确认养护模式及其阈值因机型而异，并提示部分机型需要电源管理驱动。联想手册也确认 normal、conservation、rapid 三种模式的用户语义。公开资料未在本轮检索中提供上述 EnergyDrv 私有 IOCTL 的支持承诺或版本兼容合同。[联想知识库 431337](https://iknow.lenovo.com.cn/detail/431337)、[联想 conservation mode 手册](https://download.lenovo.com/pccbbs/pubs/ip5_pro/html_en/en/intro_battery_mode_conservation.html)

Lenovo BIOS WMI Interface Guide 面向其列出的产品范围。该文档不能用于将 ThinkPad 的 BIOS 配置接口直接套到本机 IdeaPad/Legion 电池模式。Commercial Vantage 的电池信息 WMI 策略和充电阈值策略有其服务和产品前提。[Lenovo BIOS WMI 文档](https://docs.lenovocdrt.com/ref/bios/wmi/wmi_guide/)、[Commercial Vantage 配置](https://docs.lenovocdrt.com/guides/lcv/configuration/)

## 最小依赖与卸载判断

重写的三模式后端只需 Windows 原生 CreateFileW/DeviceIoControl、联想签名的 ACPIVPC 驱动包，以及支持 GBMD/SBMC 的对应机型 ACPI 固件。工具本身不需要 PCMBatterySettingsPlugin.dll、BatterySetting.exe、GameSettingsPlugin.dll、管家服务或 iMController 的缓存注册表才能发送相同设备请求。

当前 ACPIVPC 是 DriverStore 中运行的硬件驱动，静态路径与设备映射表明它具备独立于 PCManager 安装目录的条件。卸载后保留该正常安装的驱动包和 ACPI 设备，三模式后端预期可用。实际卸载程序是否删除或影响驱动包尚未测试，不能保证卸载结果。最小保留单位是完整签名驱动包及其系统注册，不能仅拷贝 SYS 文件。

本轮尚未对三个切换操作做真机验证，未验证休眠恢复、重启保持、并发控制、固件拒绝、适配器条件和卸载后的状态。实现可提供只读能力页和显式确认后的硬件控制入口，硬件写测试须按用户要求再次确认后执行。

## 纯模拟测试

tools/HardwareSelfTest.cs 只构造 SimulatedEnergyDevice，实现 IEnergyDevice 并在内存中模拟状态位。编译后的 build-tests/HardwareSelfTest.exe 运行通过 135 项断言。覆盖全部 3×3 模式转换、普通与扩展养护配置、命令顺序、保留无关状态位、每个写命令已生效后返回异常的恢复、快充支持位缺失、矛盾状态和未知枚举值拒绝。模拟测试能够检查控制逻辑，硬件和固件执行结果仍需后续用户确认的真机写测试。
