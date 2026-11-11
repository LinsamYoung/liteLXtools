using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LiteLX;

internal static class HardwareSelfTest
{
    private static int assertions;

    private sealed class FakeFanDevice : IFanDevice
    {
        public uint Raw = 1;
        public int Queries;
        public bool IgnoreStart;
        public bool IgnoreStop;
        public bool LoseSupportOnStop;
        public IOException StartFailure;
        public IOException StopFailure;
        public CancellationTokenSource CancelOnStart;
        public readonly List<bool> Writes = new List<bool>();
        public readonly List<string> Calls = new List<string>();

        public uint FanStatus()
        {
            Queries++;
            Calls.Add("query");
            return Raw;
        }

        public void FanSet(bool enabled)
        {
            Writes.Add(enabled);
            Calls.Add(enabled ? "start" : "stop");
            // 模拟设备已接受命令，但传输层随后报告失败。
            if (enabled && !IgnoreStart) Raw |= 2;
            if (!enabled && !IgnoreStop) Raw &= ~2u;
            if (!enabled && LoseSupportOnStop) Raw = 0;
            if (enabled && CancelOnStart != null) CancelOnStart.Cancel();
            if (enabled && StartFailure != null) throw StartFailure;
            if (!enabled && StopFailure != null) throw StopFailure;
        }

        public void Dispose() { }
    }

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    private static void FanGuards()
    {
        var unsupported = new FakeFanDevice { Raw = 0 };
        bool rejected = false;
        try { HardwareControl.RunFan(unsupported, 5, CancellationToken.None); }
        catch (NotSupportedException) { rejected = true; }
        Check(rejected, "风扇支持位为零未拒绝");
        Check(unsupported.Queries == 1 && unsupported.Writes.Count == 0, "不支持的风扇仍收到写入");

        var running = new FakeFanDevice { Raw = 3 };
        rejected = false;
        try { HardwareControl.RunFan(running, 5, CancellationToken.None); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "已在运行的除尘周期未拒绝");
        Check(running.Queries == 1 && running.Writes.Count == 0, "接管了已在运行的除尘周期");

        foreach (uint raw in new uint[] { 5, 9, 13 })
        {
            var unknown = new FakeFanDevice { Raw = raw };
            rejected = false;
            try { HardwareControl.RunFan(unknown, 5, CancellationToken.None); }
            catch (NotSupportedException) { rejected = true; }
            Check(rejected, "未知风扇状态位未拒绝 " + raw);
            Check(unknown.Queries == 1 && unknown.Writes.Count == 0, "未知风扇状态仍收到写入 " + raw);
        }

        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            var cancelled = new FakeFanDevice();
            rejected = false;
            try { HardwareControl.RunFan(cancelled, 5, cancellation.Token); }
            catch (OperationCanceledException) { rejected = true; }
            Check(rejected, "预先取消的除尘周期未拒绝");
            Check(cancelled.Queries == 0 && cancelled.Writes.Count == 0, "预先取消后仍访问模拟设备");
        }

        foreach (int seconds in new[] { 0, 19 })
        {
            var invalid = new FakeFanDevice();
            rejected = false;
            try { HardwareControl.RunFan(invalid, seconds, CancellationToken.None); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected && invalid.Queries == 0 && invalid.Writes.Count == 0, "无效风扇时长仍访问模拟设备");
        }
    }

    private static void FanCancellation()
    {
        using (var cancellation = new CancellationTokenSource())
        {
            // 启动接受后立即取消，使等待路径无需真实延时。
            var device = new FakeFanDevice { CancelOnStart = cancellation };
            HardwareControl.RunFan(device, 5, cancellation.Token);
            Check(cancellation.IsCancellationRequested, "模拟周期未触发取消");
            Check(device.Writes.SequenceEqual(new[] { true, false }), "取消周期的启停顺序错误");
            Check(device.Calls.SequenceEqual(new[] { "query", "start", "query", "stop", "query" }), "取消周期缺少启动或停止回读");
            Check(device.Raw == 1 && device.Queries == 3, "取消周期结束后未确认停止");
        }
    }

    private static void FanFailures()
    {
        var startFailure = new IOException("模拟启动传输失败");
        var startFailed = new FakeFanDevice { StartFailure = startFailure };
        IOException caught = null;
        try { HardwareControl.RunFan(startFailed, 5, CancellationToken.None); }
        catch (IOException error) { caught = error; }
        Check(caught != null && Object.ReferenceEquals(caught.InnerException, startFailure), "启动错误未保留");
        Check(startFailed.Writes.SequenceEqual(new[] { true, false }), "启动结果不明确后未发送停止");
        Check(startFailed.Calls.SequenceEqual(new[] { "query", "start", "stop", "query" }), "启动失败后的停止回读顺序错误");
        Check(startFailed.Raw == 1, "启动部分生效后未恢复停止状态");

        var startUnread = new FakeFanDevice { IgnoreStart = true };
        caught = null;
        try { HardwareControl.RunFan(startUnread, 5, CancellationToken.None); }
        catch (IOException error) { caught = error; }
        Check(caught != null && caught.InnerException != null, "启动回读未运行时未报告错误");
        Check(startUnread.Writes.SequenceEqual(new[] { true, false }) && startUnread.Queries == 3, "启动回读失败后未执行停止并回读");

        var stopFailure = new IOException("模拟停止传输失败");
        var bothFailed = new FakeFanDevice { StartFailure = startFailure, StopFailure = stopFailure };
        AggregateException combined = null;
        try { HardwareControl.RunFan(bothFailed, 5, CancellationToken.None); }
        catch (AggregateException error) { combined = error; }
        Check(combined != null && combined.InnerExceptions.Count == 2, "启停双错误未合并保留");
        Check(Object.ReferenceEquals(combined.InnerExceptions[0], startFailure) && Object.ReferenceEquals(combined.InnerExceptions[1], stopFailure), "启停错误内容或顺序丢失");
        Check(bothFailed.Writes.SequenceEqual(new[] { true, false }), "启动失败后停止请求次数错误");

        using (var cancellation = new CancellationTokenSource())
        {
            var stopFailed = new FakeFanDevice { StopFailure = stopFailure, CancelOnStart = cancellation };
            caught = null;
            try { HardwareControl.RunFan(stopFailed, 5, cancellation.Token); }
            catch (IOException error) { caught = error; }
            Check(Object.ReferenceEquals(caught, stopFailure), "停止单阶段错误未保留");
            Check(stopFailed.Writes.SequenceEqual(new[] { true, false }) && stopFailed.Queries == 2, "停止失败时访问顺序错误");
        }

        using (var cancellation = new CancellationTokenSource())
        {
            var stopUnread = new FakeFanDevice { IgnoreStop = true, CancelOnStart = cancellation };
            caught = null;
            try { HardwareControl.RunFan(stopUnread, 5, cancellation.Token); }
            catch (IOException error) { caught = error; }
            Check(caught != null && caught.Message.Contains("回读未确认"), "停止回读仍运行时未报告错误");
            Check(stopUnread.Queries == 3 && (stopUnread.Raw & 2) != 0, "停止回读故障场景未覆盖");
        }
        using (var cancellation = new CancellationTokenSource())
        {
            // 本机测试曾在停止后丢失支持位，必须报告无法确认而非成功。
            var unsupportedAfterStop = new FakeFanDevice { LoseSupportOnStop = true, CancelOnStart = cancellation };
            caught = null;
            try { HardwareControl.RunFan(unsupportedAfterStop, 5, cancellation.Token); }
            catch (IOException error) { caught = error; }
            Check(caught != null && caught.Message.Contains("raw=0x00000000"), "停止后的零状态被误判为确认关闭");
            Check(unsupportedAfterStop.Writes.SequenceEqual(new[] { true, false }), "支持位丢失后出现额外写入");
        }
    }

    private static int Main()
    {
        try
        {
            FanGuards();
            FanCancellation();
            FanFailures();
            WmiFanDevice.VerifyReturn(null, false);
            Check(true, "无输出的 WMI setter 应接受正常完成");
            bool missingGetter = false;
            try { WmiFanDevice.VerifyReturn(null, true); }
            catch (IOException) { missingGetter = true; }
            Check(missingGetter, "WMI getter 缺失输出必须报错");
            Console.WriteLine("PASS " + assertions + " assertions. Simulated IFanDevice only.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL " + error.Message);
            return 1;
        }
    }
}
