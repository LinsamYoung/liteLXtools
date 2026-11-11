# 右键菜单管理

## 已验证的安装文件与入口

分析对象为 `C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222`，采用静态文件读取与只读注册表查询。

- `skin/dist/json/defaultToolList.json` 中的 `rightclickmanagement` 入口通过 `pcm_router` 打开 `tools_rightclick_management`。
- `skin/dist/js/main.js` 将界面操作传至 `pcmRightClickMenuModule`。查询调用 `NEWPLUGIN_WSRMMGRPLUGIN{64}_GETMENUINFO`，设置调用 `_SETMENUINFO`，恢复出厂调用 `_RESTOREFACTORY`。架构选择决定 `WSPluginHost.exe` 或 `WSPluginHost64.exe`，对应 `plugins/WSRMMgrPlugin.dll` 或 `WSRMMgrPlugin64.dll`。这些是前端可直接读取的调用关系，原生插件内部的具体注册表操作尚未反编译验证。
- 管家自身的“鼠标右键显示入口”复选框另走 `NEWPLUGIN_WRAPPLUGIN_GETRIGHTMENUSTATUS` 与 `NEWPLUGIN_WRAPPLUGIN_SETRIGHTMENUSTATUS`。
- `ContextMenu/ContextMenuExtPkg64.msix` 的 `AppxManifest.xml` 声明 `windows.fileExplorerContextMenus` 和 `windows.comServer`，`CLSID` 为 `{43F38682-CBB4-47BC-888F-8FBEAA382B04}`，服务器为 `ContextMenuExt64.dll`。适用位置为 `*`、`Directory`、`Directory\Background`。
- `ContextMenu/PCMContextMenu64.dll` 的 `REGISTRY` 类型资源注册传统 `ContextMenuHandlers/MenuManagerExtEx`，`CLSID` 为 `{E5428BAA-B363-4116-91F5-B2F86BA8BBF6}`。资源负载在原文件偏移 `441720`，长度 `1458`，为 ASCII 注册脚本。REA Evidence 为 `ev_527a77b4878d7a21e9108ae625e5929c2d6f1b9ecac0e69f539b9f0c961288b9`，文件 SHA-256 为 `1d5363e2082c218cfad2bdeced04b98ad7b824e6d6773d92347d863ab87d640c`。
- 当前机器的 HKLM 64 位 `CLSID/{E5428BAA-B363-4116-91F5-B2F86BA8BBF6}/InprocServer32` 指向 `C:\ProgramData\Lenovo\devicecenter\ContextMenu\PCMContextMenu64.dll`。因此实际注册路径与原安装目录内副本不同。32 位视图存在同名菜单注册，查询未找到对应 COM 服务器。

两份 ContextMenu DLL 的字符串包含 `unshell`、`unshellex\ContextMenuHandlers`、`SOFTWARE\Lenovo\PcManager\rbm\Blocked` 和 `Blocked disable`。这些只建立候选机制，尚不足以证明某个设置操作会移动注册表项。当前机器扫描结果中没有这些迁移位置下的菜单。

## 独立实现

`src/LiteLX/ContextMenus.cs` 使用 .NET Framework 自带的 `Microsoft.Win32.RegistryKey`。不需要管家的 EXE、DLL、服务或驱动。

- `ContextMenus.Scan()` 只读枚举 HKCU、HKLM `Software\Classes` 下注册的直接 `shell` 命令和 `shellex\ContextMenuHandlers`。64 位系统分别查询 32 位与 64 位视图。保留来源信息，因此共享注册表键可能在多个视图出现。
- 同时扫描 `Directory\Background`、`LibraryFolder\Background`、`SystemFileAssociations` 与管家的 `unshell/unshellex` 候选迁移位置。迁移项仅供核对。
- `ContextMenus.Disable(MenuEntry)` 对普通命令添加空字符串类型的 `LegacyDisable` 值。对传统 COM handler，在当前用户的 `Shell Extensions\Blocked` 下添加对应 CLSID 的空字符串值。
- COM 屏蔽以 CLSID 为单位，同一扩展的其他入口也可能受到影响。HKLM 普通命令写入和整机屏蔽项删除需要对应的写权限，权限失败信息会保留在操作详情中。
- `ContextMenus.Enable(MenuEntry)` 清除普通命令的 `LegacyDisable` 和 `ProgrammaticAccessOnly`，或清除 HKCU、HKLM 中对应 CLSID 的 `Blocked` 值。`CanEnable` 根据实际禁用状态返回，支持不同程序及旧版 LiteLX 写入的各种类型禁用值。
- 本工具内并发操作通过当前登录会话的命名互斥锁串行执行，外部程序修改通过写入前检查和启用时指纹检查检测。禁用和启用完成后回读注册表标记。外部程序若在最终检查与写入之间修改同一值，仍存在竞态窗口。

Windows 11 打包应用通过清单注册的现代菜单，以及 Explorer 内置命令，没有完整纳入当前枚举范围。实际菜单显示还受上下文、文件类型、策略和 Explorer 缓存影响。程序把注册状态显示为“已注册”，运行后的可见性需人工核对。更改后可关闭再打开资源管理器窗口，必要时注销重新登录。

Microsoft 文档说明静态命令和 COM handler 的注册模型。`LegacyDisable` 在 Microsoft 的经典 Windows 示例中有使用。`Blocked` 的通用实现已编码，当前机器上的实际屏蔽效果仍待显式变更测试验证。[静态与动态菜单注册](https://learn.microsoft.com/en-us/windows/win32/shell/context-menu-handlers) [Microsoft 经典示例](https://github.com/microsoft/Windows-classic-samples/blob/main/Samples/Win7Samples/winui/shell/appshellintegration/IdealPropertyHandler/RegisterExtension.cpp)

## 编译与测试记录

已用本机 `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` 编译为临时 DLL，引用 `System.Core.dll` 和 `System.Xml.dll`，编译通过。

最初的只读扫描实测返回 4552 项，其中普通命令 4132 项、handler 420 项、警告 0 条，耗时约 3 秒。

`build.ps1 -Test` 包含 `tools/MenuSelfTest.cs`，在随机创建的临时 HKCU 命令和 CLSID 上验证直接禁用、启用、重复操作、分组及跨视图切换、不同来源和类型的禁用值、叠加隐藏状态及过期定义检查。界面测试覆盖真实 WPF 动画时钟、连续反向切换、列表行及图标复用、写入失败回退、部分完成后的实际状态同步和跨页面的关联入口更新，结束后清理测试条目。测试同时检查操作前后的备份文件列表一致。

后续变更验证应在单独测试账户或虚拟机中创建人工菜单项，然后通过 GUI 或 CLI 显式禁用并恢复，核对下面几种情况。

1. 原有 `LegacyDisable`、`ProgrammaticAccessOnly` 和 `Blocked` 值可以通过开关清除。
2. 更新菜单定义后，旧扫描对象的禁用操作被拒绝。
3. 更新已禁用菜单定义后，过期扫描对象的启用操作被拒绝。
4. 重复恢复成功返回，多进程操作串行执行。
5. 禁用后重新启动程序，扫描仍可读取实际禁用状态并直接启用。
6. 非管理员修改 HKLM 命令时产生明确权限失败。
7. COM 扩展的相同 CLSID 多入口共同屏蔽效果与 32/64 位 Explorer 行为符合预期。
