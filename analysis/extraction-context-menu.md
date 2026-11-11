# 原厂右键子模块提取调查

## 调查范围

目标为 `C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222`。本次仅静态读取目录、PE 导入导出、字符串、MSIX 清单与选定函数反汇编，没有启动原厂 EXE、加载原厂 DLL、注册 COM、安装 MSIX 或改动真实菜单。

使用已存在的 REA 资源证据和工作区 `tools/pe_inspect.py` 中的 pefile、Capstone 继续分析，完整结果保存在 `analysis/evidence/context-extraction`。

## 可提取对象与可用性

| 对象 | 已验证事实 | 独立运行判断 |
| --- | --- | --- |
| `ContextMenu/ContextMenuExt64.dll` | x64 COM shell 扩展，MSIX 清单把它注册为 `windows.fileExplorerContextMenus` | 提供 Explorer 中的入口与命令处理，未发现独立 GUI EXE |
| `ContextMenu/PCMContextMenu64.dll` | x64 传统 `IContextMenu` 扩展，导出 `DllGetClassObject`、`DllRegisterServer`、`DllInstall` 等 | 由 Explorer/COM 宿主加载，原厂管理界面最终跳转管家主程序 |
| `ContextMenu/ContextMenuExtPkg64.msix` | 11720 字节的外部内容清单包，含 manifest、resources.pri、块表与签名 | 包内没有管理工具 EXE。安装包会变更系统注册，本次未执行 |
| `plugins/WSRMMgrPlugin.dll` | x86 原生业务插件，913232 字节，导出五个 `pcm_plugin_*` 入口 | 有作为自编宿主子模块复用的静态基础，ABI 与初始化副作用尚未充分验证 |
| `plugins/WSRMMgrPlugin64.dll` | x64 原生业务插件，1071440 字节，同类五个导出 | 同上，可作为 64 位候选 |
| `WSPluginHost.exe`、`WSPluginHost64.exe` | 原厂通用插件宿主，直接导入表为 Windows DLL，包含命名管道服务器与插件加载器 | 静态观察到独立进程选项，可作为无界面宿主候选。完整启动参数和管道协议尚未恢复 |

`ContextMenu` 目录的完整文件清单只有两份 DLL 和一份 MSIX。该范围内没有可直接复制后打开的独立右键管理 EXE。

## 原厂界面与后端边界

`skin/dist/json/defaultToolList.json` 的 `rightclickmanagement` 使用 `pcm_router`，页面为 `tools_rightclick_management`。`skin/dist/js/main.js` 创建 `pcmRightClickMenuModule`，选择宿主和架构后通过 `asyncSendIPCMessage` 调用下面的动作。

| 前端方法 | IPC 动作 |
| --- | --- |
| `getRightClickMenuList` | `NEWPLUGIN_WSRMMGRPLUGIN{64}_GETMENUINFO` |
| `setRightClickMenuItem` | `NEWPLUGIN_WSRMMGRPLUGIN{64}_SETMENUINFO` |
| `resetRightClickMenu` | `NEWPLUGIN_WSRMMGRPLUGIN{64}_RESTOREFACTORY` |

界面传给设置动作的数据包括 `type`、`enable`、`keyname`，`type` 在当前 UI 中区分文件与桌面。插件也保留 `MENURECOVER` 动作字符串。数据字段只建立前端消息形状，未执行动作以确认完整原生校验规则。

现代和传统 shell 扩展均包含 `LenovoPcManager.exe`、`pcmtabpage` 与 `tools_rightclick_management`。进一步在传统扩展中追到实际静态调用链。

```text
PCMContextMenu64.dll 0x180019600
  → 把 tools_rightclick_management 传给 0x18001d7c0
  → 0x180010d60 构建 pcmtabname / pcmsourcetab / pcmtabtype / pcmtabpage 等参数
  → 0x180015f50 构建含 LenovoPcManager.exe 的路径并调用 PathFileExistsW
  → 0x18001d050
  → ShellExecuteExW，调用点 0x18001d149
```

这个入口的原厂管理 UI 依赖管家主程序和其前端资源。复制 shell 扩展后注册 COM，只能保留入口与扩展逻辑，尚不能形成完整管理窗口。

对应原始反汇编见 `PCMContextMenu64-launch-xrefs.asm.txt`、`PCMContextMenu64-function-0x1d7c0.asm.txt`、`PCMContextMenu64-function-0x15f50.asm.txt`、`PCMContextMenu64-function-0x1d050.asm.txt`。地址采用该文件默认 ImageBase `0x180000000`。

## 通用插件宿主的独立进程候选

`WSPluginHost.exe` 的字符串和代码同时提供了独立运行的线索。

- 宿主通过 `GetProcAddress` 查找 `pcm_plugin_ininitialize`、`pcm_plugin_action`、`pcm_plugin_buffer_allocate`、`pcm_plugin_buffer_free`、`pcm_plugin_deininitialize`，还支持若干 `_ex` 变体。查找调用位于 `0x41ba1c` 开始的序列。
- 宿主含 `pluginName`、`plugin_dir`、`pluginpath` 参数名。`plugin_dir` 在 `0x41dcfd` 被传给解析函数，`pluginpath` 在 `0x41dd15` 和 `0x42220b` 被解析。
- 宿主导入 `CreateNamedPipeW`、`ConnectNamedPipe`、`DisconnectNamedPipe`，RTTI 暴露 `CNewPluginPipeServer` 与 `OnReceivePackage`。因此 IPC 服务器实现确实存在于宿主二进制中。
- `independentprocess` 在 `0x4299aa` 传入布尔选项解析函数 `0x4285f0`，返回值保存到 `bl`。`0x429ac3` 检查该值，`0x429ac5` 的非零分支跳到 `0x429c54`，绕过后面的 `pcmparentprocessid` 解析和父句柄等待分支。
- 宿主还使用 `quiteventname` 控制退出。独立进程分支只证明它能够绕过一段父进程生命周期逻辑，不能据此证明没有其他管家安装状态依赖。

上述证据意味着原厂业务后端存在脱离主 GUI 的潜力。当前不能给出经过验证的一条独立启动命令，因为命令行词法、管道握手、消息头格式、权限模型及初始化回调尚未完整恢复。

相关原始证据为 `WSPluginHost-protocol.json` 与 `WSPluginHost-independentprocess.asm.txt`。`independentprocess` 分支使用 x86 宿主默认 ImageBase `0x400000`。

## 原厂 DLL 自编宿主候选

`WSRMMgrPlugin.dll` 的导出表如下。

| 导出 | RVA |
| --- | --- |
| `pcm_plugin_action` | `0x4a100` |
| `pcm_plugin_buffer_allocate` | `0x4a300` |
| `pcm_plugin_buffer_free` | `0x4a310` |
| `pcm_plugin_deininitialize` | `0x4a330` |
| `pcm_plugin_ininitialize` | `0x4a400` |

x86 `pcm_plugin_action` 在 `0x1004a110`、`0x1004a114`、`0x1004a118` 读取三个栈参数。第一个非空参数被当作零结尾窄字符串扫描，第二个可为空，第三个作为输出指针使用。函数末尾是普通 `ret`，与 cdecl 风格兼容。它构建内部字符串并调用 `0x100497b0`，随后分配结果缓冲区并写入第三参数指向的位置。

这是“动作名、JSON 或文本参数、结果缓冲区”的 ABI 候选，仍需结合初始化与实际宿主调用点确认完整声明、编码、回调表、线程要求、结果释放与失败约定。尚未据此生成或调用 P/Invoke 声明。

## 依赖下界与候选最小保留集合

下表是静态依赖下界，完整动态依赖尚待验证。

| 复用途径 | 已确认的原厂文件下界 | 尚待确认 |
| --- | --- | --- |
| 自编 x86 宿主 | `plugins/WSRMMgrPlugin.dll`、安装根目录 `libcurl.dll` | 插件初始化回调，动态加载库与本地配置，安装路径解析 |
| 自编 x64 宿主 | `plugins/WSRMMgrPlugin64.dll`、安装根目录 `libcurl64.dll` | 同上 |
| 原厂 x86 宿主 | 前项加 `WSPluginHost.exe` | 命令行与命名管道协议，退出事件，安装状态查询 |
| 原厂 x64 宿主 | 前项加 `WSPluginHost64.exe` | 同上 |
| 保留原厂 UI | 管家主程序、相关前端与宿主、业务插件 | CEF 和主程序依赖进一步闭包，整体体积将明显扩大 |

两份 libcurl 的导入表仅列 Windows DLL，包括 Winsock、LDAP、证书与加密接口，未发现额外第三方直导依赖。宿主与业务插件的直导表都没有驱动或联想硬件接口依赖。

插件字符串还引用 `WrapPlugin.dll`、`ludp.dll`、`ecombase.dll`、`libSoftware.dll`、`ShellMenuDic.dat`、配置版本 `rbm_cfgVer` 和远端 `rbm_meta.json`、`rcmenu_meta.json`。这些属于动态依赖或数据来源候选，尚未从引用位置证明每次初始化都必需。没有获取远端配置。

本次保留了 `plugins/ShellMenuDic.dat` 和 `plugins/ContextMenu.dat` 的哈希，未解码数据格式。复制候选文件可用于后续隔离验证，当前无法把这个集合标注为“已验证最小可运行包”。

## 关键文件身份

| 文件 | SHA-256 |
| --- | --- |
| `WSPluginHost.exe` | `d69dda20a805fd77829a162ca4fdd6d13c5bd317a2b61c4466efa2dc65d85341` |
| `WSPluginHost64.exe` | `c9c2df67a335414b5cc85338c4a4b890c186ed7600643fae61a5a393bef6c3fe` |
| `WSRMMgrPlugin.dll` | `9396d3b4942e74e9b059a5ee3da3a2844bdbefcc64a737575512c626cde6a8de` |
| `WSRMMgrPlugin64.dll` | `1a8072a6e6be264393f4a10e096b043b444e6cf372d7323a97377ed954d91ac5` |
| `libcurl.dll` | `e820c3fc10de2d2999b84a669a8c9c64d99cdb3159118cf701bb7d29cde8ac1f` |
| `libcurl64.dll` | `9c1f9d69bab7f10094b9b121691ae554f3764649f273618a2b7c974cecfaa973` |
| `PCMContextMenu64.dll` | `1d5363e2082c218cfad2bdeced04b98ad7b824e6d6773d92347d863ab87d640c` |
| `ContextMenuExtPkg64.msix` | `bc754d01dd9f0b12c011ce047ef541a726f5eda926e8ca07daab02aaac5464d8` |
| `ShellMenuDic.dat` | `dbd1292a0293c47016a37c71ef5f676b3bad28f58522803e21e715d03690d0f4` |
| `ContextMenu.dat` | `286b13ade900c65ef67491d11c56bf883d9d307659ab0ea08ad3c15db8661e78` |

## 后续验证边界

尚未进行任何原厂模块运行测试。下一阶段先静态补齐 `pcm_plugin_ininitialize` 回调结构、宿主启动解析和 IPC 包格式，再在隔离账户或虚拟机中观察只读列表动作。插件初始化自身可能进行缓存写入或网络请求，需要把这些副作用纳入运行验证范围。右键设置与恢复动作继续保留为显式用户操作。

卸载管家后的独立原厂管理 UI 尚无验证结果。无 GUI 的原厂插件复用是当前可继续推进的具体候选，Windows 原生实现已可独立编译与只读扫描。
