# 首次风扇测试记录

测试日期为2026年11月10日，使用 Asia/Shanghai 时间。用户明确批准方案中的状态查询和一次5秒启停。测试前仅完成静态分析和内存模拟。

## 观察结果

1. 初始状态查询成功，返回 `raw=0x00000001`，即该次查询的支持位为1、运行位为0，未知位2/3为0。
2. 18时35分22秒运行 `fan run 5 --execute --confirm-hardware`。程序在启动或启动回读阶段抛出异常，提前进入停止阶段，因此预定的5秒运行尚未完成。
3. 当时程序输出“除尘运行发生错误，停止请求已完成并回读”，进程退出码为1。
4. 随后的追加查询返回 `raw=0x00000000`。这同时清除了支持位，单凭运行位为零不足以确认该次回读有效。
5. 用户明确反馈“已恢复到测试前状态”，观察对象为风扇声音或转速。
6. 追加只读采集显示 ACPIVPC、LnCoolit、LnvDrvFdn 和相关管家服务仍为 Running，启动方式与采集前一致。

原始输出保存在 `analysis/evidence/fan-live-query.txt`、`fan-live-cycle.txt` 和 `fan-live-after.txt`。服务与驱动复核保存在 `services-after-fan.json`、`drivers-after-fan.json`。

## 日志缺口与修正

首次版本的 CLI 只输出外层异常消息，内层异常类型及具体失败位置已经丢失。因此无法据本次日志区分启动 IOCTL 失败、启动回读失败、启动回读未置位。该缺口应保留在结论中。

当时的停止判定只检查运行位为零，会把支持位同时为零的状态也视为已确认关闭。已改成支持位为1且运行位为0才算确认关闭。原始测试版本的成功措辞不能作为固件已恢复的独立证明。

后续版本已加入每个 IOCTL 的输入、返回长度、Win32错误码、原始输出和时间戳，CLI保留完整异常链。日志输出异常会被隔离，避免干扰停止请求。对应纯模拟回归覆盖支持位丢失的情形。

## 静态复核

原厂两代 EnergyDrv 路径在启动 IOCTL 返回后立即查询，未发现显式延时重试。原厂当前 WSHardware 请求处理层忽略核心返回的状态枚举并发送固定 OK，前端本地运行状态也不构成硬件确认。

当前驱动内部排队并等待请求完成。请求完成与风扇物理转速爬升、固件状态位更新之间的关系仍需运行时证据。

尚待区分的原因包括 EnergyDrv 旧式路径的当前固件兼容性、状态时序、其他软件覆盖和驱动错误。首次日志无法选定原因。补充汇编证据保存在 `fan-success-timing.json` 和 `fan-handler-response.json`。

## 验收状态

当前结论为该 EnergyDrv 风扇后端未通过本机功能验收。支持查询曾成功，用户观察恢复已记录。严格的固件停止状态验证仍有缺口。

本次获批的一次启停已经执行。继续真机启停需另一个具体测试方案及批准。

## 后续获批的WMI查询

用户随后批准三个逻辑getter。普通权限均返回拒绝访问，管理员权限取得以下结果。

| 方法 | 输出 | ReturnValue |
| --- | --- | --- |
| LENOVO_UTILITY_DATA.GetIfSupportOrVersion，datatype=0x90000 | Data=0 | true |
| LENOVO_GAMEZONE_DATA.GetFanCoolingStatus | Data=0 | true |
| LENOVO_FAN_METHOD.Fan_Get_FullSpeed | Status=false | true |

两项风扇getter均报告关闭，与用户观察相符。UTILITY方法的结果仅记录原始值，它并非精确复核后确认的原厂版本门槛。详见 `analysis/fan.md`。

这一阶段自编适配器显式选择WMI FullSpeed，沿用已验证的getter及已静态确认的Boolean setter签名，启停尚未执行。随后获批的写入与最终通过结果记录在下一节。旧式EnergyDrv仅通过 `--energy` 显式选择。

自编适配器首次查询曾因.NET输出中缺少可选ReturnValue属性而报“找不到”。已改为枚举可选属性，避免把缺失属性当作查询失败。修复后在管理员权限下再次通过getter验证，返回Status=False、归一状态0x00000001，证据为 `fan-wmi-adapter-query.txt`。原始适配错误保存在 `fan-wmi-adapter-initial-error.txt`。

## WMI启停测试与修正后验收

用户另行批准后，19时02分的WMI启停尝试调用了Status=True及Status=False。但适配器把只有输入参数的setter返回空对象误判为错误，导致提前停止，未完成5秒等待。随后独立getter返回False。对应原始日志为 `fan-wmi-live-cycle-initial-error.txt`。

已允许setter的空输出正常完成，getter仍要求有效状态字段，并加入无硬件回归断言。管理员脚本同时修正原生UTF-8输出的解码，后续日志保留正确中文。

用户再次明确批准后，在19时05分执行了完整的WMI周期。

| 时间 | 事件 |
| --- | --- |
| 19时05分09.847秒 | 初始getter为False，发送Status=True |
| 19时05分09.872秒 | 启动getter为True |
| 19时05分14.876秒 | 发送Status=False |
| 19时05分14.901秒 | 停止getter为False，进程正常结束 |
| 19时05分15.043秒 | 新进程独立getter再次为False |

最终周期退出码0。证据为 `fan-wmi-live-cycle.txt` 与 `fan-wmi-after-cycle.txt`。当前WMI后端在本机已通过5秒启停和停止回读验收。更长周期、真实运行中取消、卸载后运行和固件异常恢复仍待分别验证。
