using System;
using System.ComponentModel;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace LiteLX
{
    public interface IFanDevice : IDisposable
    {
        uint FanStatus();
        void FanSet(bool enabled);
    }

    public sealed class EnergyDevice : IFanDevice
    {
        public const string VerifiedDriverHash = "c02b879e601c1689425bdd71ec6fc2ccf66d75436e973ab4f209d45473e58c95";
        private readonly SafeFileHandle handle;
        private readonly bool allowWrites;
        private readonly Action<string> trace;

        public EnergyDevice(bool permitWrites, Action<string> traceOutput = null)
        {
            // 私有协议仅对本次分析过的机型和驱动开放，升级后需重新核实。
            VerifyProfile();
            allowWrites = permitWrites;
            trace = traceOutput;
            handle = CreateFile(@"\\.\EnergyDrv", 0xc0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "打开 EnergyDrv 失败");
        }

        public static string DeviceTarget()
        {
            var buffer = new StringBuilder(4096);
            if (QueryDosDevice("EnergyDrv", buffer, buffer.Capacity) == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "EnergyDrv 设备不存在");
            return buffer.ToString();
        }

        public static string VerifyProfile()
        {
            string model = "";
            using (var search = new ManagementObjectSearcher("SELECT Model FROM Win32_ComputerSystem"))
            using (var items = search.Get())
                foreach (ManagementObject item in items) using (item) model = Convert.ToString(item["Model"]);
            if (!String.Equals(model, "82TF", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("当前硬件协议仅核实 Lenovo 82TF，检测到 " + model);
            string path;
            using (var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var service = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\ACPIVPC", false))
                path = service == null ? "" : Convert.ToString(service.GetValue("ImagePath", ""));
            if (path.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase))
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), path.Substring(12));
            path = Environment.ExpandEnvironmentVariables(path.Trim('"'));
            string hash;
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            if (hash != VerifiedDriverHash) throw new NotSupportedException("ACPIVPC 驱动与已分析版本不同，需重新核实。");
            if (DeviceTarget() != @"\Device\EnergyDrv") throw new NotSupportedException("EnergyDrv 设备映射不匹配。");
            return path;
        }

        public uint FanStatus()
        {
            return Control(0x831020c4, BitConverter.GetBytes(14u), true);
        }

        public void FanSet(bool enabled)
        {
            RequireWrite();
            var input = new byte[12];
            Buffer.BlockCopy(new uint[] { 6, 1, enabled ? 1u : 0u }, 0, input, 0, input.Length);
            Control(0x831020c0, input, false);
        }

        private void RequireWrite()
        {
            if (!allowWrites) throw new InvalidOperationException("当前设备会话只允许查询。");
        }

        private uint Control(uint code, byte[] input, bool requireOutput)
        {
            bool hasOutput = code != 0x831020c0;
            var output = hasOutput ? new byte[4] : null;
            uint returned;
            Record("IOCTL 0x" + code.ToString("x8") + " in=" + BitConverter.ToString(input));
            bool succeeded = DeviceIoControl(handle, code, input, (uint)input.Length, output, hasOutput ? 4u : 0u, out returned, IntPtr.Zero);
            int error = Marshal.GetLastWin32Error();
            Record("result=" + succeeded + " error=" + (succeeded ? 0 : error) + " returned=" + returned +
                " out=" + (output == null ? "none" : BitConverter.ToString(output)));
            if (!succeeded) throw new Win32Exception(error, "EnergyDrv 请求失败，IOCTL=0x" + code.ToString("x8"));
            if (requireOutput && returned != 4) throw new IOException("驱动响应长度异常 " + returned);
            return returned == 4 && output != null ? BitConverter.ToUInt32(output, 0) : 0;
        }

        private void Record(string message)
        {
            // 日志输出失败不能阻碍后续停止请求。
            if (trace != null) try { trace(DateTime.Now.ToString("o") + " " + message); } catch { }
        }

        public void Dispose() { handle.Dispose(); }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string name, uint access, uint sharing, IntPtr security, uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle device, uint code, byte[] input, uint inputSize, byte[] output, uint outputSize, out uint returned, IntPtr overlapped);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint QueryDosDevice(string name, StringBuilder target, int size);
    }

    public static class HardwareControl
    {
        public static void RunFan(IFanDevice device, int seconds, CancellationToken cancellation)
        {
            if (seconds < 1 || seconds > 18) throw new ArgumentOutOfRangeException("seconds", "除尘周期限定 1 到 18 秒。");
            cancellation.ThrowIfCancellationRequested();
            uint before = device.FanStatus();
            if ((before & 1) == 0) throw new NotSupportedException("除尘支持位为零。");
            if ((before & 2) != 0) throw new InvalidOperationException("除尘已在运行，需先核实其发起方。");
            if ((before & 12) != 0) throw new NotSupportedException("风扇返回了含义尚未核实的状态位，需先补充分析。");
            cancellation.ThrowIfCancellationRequested();
            Exception runningError = null;
            try
            {
                device.FanSet(true);
                uint started = device.FanStatus();
                if ((started & 3) != 3) throw new IOException("除尘启动回读未确认支持且运行。raw=0x" + started.ToString("x8"));
                cancellation.WaitHandle.WaitOne(TimeSpan.FromSeconds(seconds));
            }
            catch (Exception ex) { runningError = ex; }
            try
            {
                // 包括启动结果不明确的情形，均尝试结束本次发出的启停周期。
                device.FanSet(false);
                uint stopped = device.FanStatus();
                if ((stopped & 3) != 1) throw new IOException("停止请求已发送，回读未确认支持且关闭。raw=0x" + stopped.ToString("x8"));
            }
            catch (Exception stoppingError)
            {
                if (runningError != null) throw new AggregateException("除尘运行与停止均发生错误。", runningError, stoppingError);
                throw;
            }
            if (runningError != null) throw new IOException("除尘运行发生错误，停止请求已完成并回读。", runningError);
        }
    }
}
