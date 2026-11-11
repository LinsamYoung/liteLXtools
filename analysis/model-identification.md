# 对外型号识别

## 本机读取结果

2026-11-10 在本机读取到以下 Windows 元数据。

| 来源 | 字段 | 值 |
| --- | --- | --- |
| `HKLM\HARDWARE\DESCRIPTION\System\BIOS` | `SystemProductName` | `82TF` |
| 同上 | `SystemFamily` | `Legion Y9000X IAH7` |
| 同上 | `SystemSKU` | `LENOVO_MT_82TF_BU_idea_FM_Legion Y9000X IAH7` |
| `root\cimv2` 的 `Win32_ComputerSystem` | `Model` / `SystemFamily` | `82TF` / `Legion Y9000X IAH7` |
| `root\cimv2` 的 `Win32_ComputerSystemProduct` | `Version` | `Legion Y9000X IAH7` |
| `root\wmi` 的 `MS_SystemInformation` | `SystemProductName` / `SystemFamily` | `82TF` / `Legion Y9000X IAH7` |

微软文档说明 `Win32_ComputerSystem.SystemFamily` 来自 SMBIOS Type 1 的 Family 字段。本机固件已经提供了完整名称，可以离线识别。联想知识库也提供了同名设置指南。[微软字段说明](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-computersystem#systemfamily)、[联想设置指南](https://iknow.lenovo.com.cn/app/detail/201490)

## 电脑管家的静态证据

检查目标为 `C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222\LYSDK.dll`，x86 PE，首选映像基址为 `0x10000000`，SHA-256 为 `19a73f7cfe575090c3c50056190ed80146935b3f0bcc3ad20f4dc55fa3cb524c`。

REA 成功识别目标，但原生分析提供器未配置。以下证据通过项目已有的 `pefile` 和 Capstone 工具只读提取，地址为文件首选映像基址下的虚拟地址。

| 位置 | 观察 |
| --- | --- |
| `0x1000D32F` | 压入字符串 `root\WMI`，随后用于 WMI 连接 |
| `0x1000D0ED` | 压入查询 `SELECT * FROM MS_SystemInformation` |
| `0x1000D564` | 压入 `SystemProductName`，随后调用对象虚表偏移 `0x10`，符合 `IWbemClassObject::Get` 参数顺序 |
| `0x1000D5D0` | 同样读取 `SystemSKU` |
| `0x1000D63C` | 同样读取 `SystemFamily` |
| `0x1000D65A` | 将读取到的 Family 字符串写入目标对象偏移 `0x30` 对应的字符串字段 |

这些调用表明电脑管家随附 SDK 会从 Windows 的 `MS_SystemInformation` 获取系统名称、系列和 SKU。已确认 SDK 的采集路径，主窗口最终展示名称的调用链仍未追踪。

## LiteLX 接入

`MachineIdentity.ReadName()` 优先读取 BIOS 注册表副本的 `SystemFamily`，可解析联想 SKU 中的 `_FM_` 系列字段。名称缺失或只有机器类型码时，再尝试 `Win32_ComputerSystemProduct.Version`。过滤常见 OEM 占位文本后回退到机器型号，均缺失时显示“本机”。

主窗口异步更新型号并对长名称使用省略号，悬停可查看全文。CLI 的 `status` 输出新增 `MachineName` 字段，方便核对识别结果。
