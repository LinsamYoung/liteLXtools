using System;
using System.IO;
using System.Management;

namespace LiteLX
{
    public sealed class WmiFanDevice : IFanDevice
    {
        private readonly ManagementObject instance;
        private readonly bool allowWrites;
        private readonly Action<string> trace;

        public WmiFanDevice(bool permitWrites, Action<string> traceOutput = null)
        {
            EnergyDevice.VerifyProfile();
            allowWrites = permitWrites;
            trace = traceOutput;
            // 本机已验证此 getter 返回布尔 Status，明确选择该后端避免猜测自动选路。
            using (var search = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM LENOVO_FAN_METHOD"))
            using (var items = search.Get())
            {
                if (items.Count != 1) throw new NotSupportedException("LENOVO_FAN_METHOD 实例数需人工核实 " + items.Count);
                foreach (ManagementObject item in items)
                {
                    using (item) instance = new ManagementObject(item.Path);
                }
            }
            instance.Options.Timeout = TimeSpan.FromSeconds(10);
            instance.Get();
        }

        public uint FanStatus()
        {
            using (var result = instance.InvokeMethod("Fan_Get_FullSpeed", null, new InvokeMethodOptions { Timeout = TimeSpan.FromSeconds(10) }))
            {
                VerifyReturn(result, true);
                object status = result["Status"];
                if (!(status is bool)) throw new IOException("WMI Status 不是 Boolean。");
                Record("Fan_Get_FullSpeed Status=" + status);
                // 统一为控制器的支持/运行位，这里的支持位表示本次调用有效。
                return (bool)status ? 3u : 1u;
            }
        }

        public void FanSet(bool enabled)
        {
            if (!allowWrites) throw new InvalidOperationException("当前 WMI 会话只允许查询。");
            using (var input = instance.GetMethodParameters("Fan_Set_FullSpeed"))
            {
                input["Status"] = enabled;
                Record("Fan_Set_FullSpeed Status=" + enabled);
                using (var result = instance.InvokeMethod("Fan_Set_FullSpeed", input, new InvokeMethodOptions { Timeout = TimeSpan.FromSeconds(10) })) VerifyReturn(result, false);
            }
        }

        internal static void VerifyReturn(ManagementBaseObject result, bool requireOutput)
        {
            if (result == null)
            {
                if (requireOutput) throw new IOException("WMI getter 未返回输出对象。");
                // setter 只有输入参数时可以正常返回空对象，状态以随后的 getter 为准。
                return;
            }
            // .NET WMI 输出可能省略 ReturnValue，枚举查找以免缺失属性触发异常。
            foreach (PropertyData property in result.Properties)
                if (property.Name.Equals("ReturnValue", StringComparison.OrdinalIgnoreCase) &&
                    property.Value is bool && !(bool)property.Value)
                    throw new IOException("WMI ReturnValue 为 false。");
        }

        private void Record(string message)
        {
            if (trace != null) try { trace(DateTime.Now.ToString("o") + " " + message); } catch { }
        }

        public void Dispose() { instance.Dispose(); }
    }
}
