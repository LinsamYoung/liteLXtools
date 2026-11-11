using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace LiteLX
{
    public static class Inspection
    {
        public static object Capture()
        {
            var classes = new Dictionary<string, object>();
            foreach (string name in new[] { "LENOVO_GAMEZONE_DATA", "LENOVO_FAN_METHOD", "LENOVO_UTILITY_DATA" })
            {
                try
                {
                    using (var type = new ManagementClass(@"root\wmi", name, null))
                    {
                        type.Get();
                        var methods = new List<string>();
                        foreach (MethodData method in type.Methods) methods.Add(method.Name);
                        classes[name] = methods;
                    }
                }
                catch (Exception ex) { classes[name] = ex.Message; }
            }
            string profile;
            try { profile = EnergyDevice.VerifyProfile(); }
            catch (Exception ex) { profile = ex.Message; }
            var power = new PowerStatus();
            if (!GetSystemPowerStatus(out power)) throw new Win32Exception(Marshal.GetLastWin32Error());
            return new {
                CapturedUtc = DateTime.UtcNow.ToString("o"), HardwareProfile = profile, MachineName = MachineIdentity.ReadName(),
                AcLine = power.AcLine, BatteryPercent = power.Percent == 255 ? (int?)null : power.Percent,
                BatteryFlags = power.Flags, WmiSchemas = classes,
                EvidenceLevel = "仅元数据和 Windows 电量状态。硬件私有 getter 和写入均需单独调用。",
                Firmware = "开机 Logo，EFI\\Lenovo\\Logo + LBLDESP/LBLDVC 的固件机制已静态定位。"
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PowerStatus
        {
            public byte AcLine, Flags, Percent, Saver;
            public uint Remaining, Full;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemPowerStatus(out PowerStatus status);
    }

    public static class LogoTools
    {
        public static object ReadFirmware()
        {
            // 只读取已定位的变量，权限调整在本进程令牌内完成并恢复。
            using (var privilege = new FirmwareReadPrivilege())
            {
                var data = new byte[32768];
                uint attributes;
                uint size = GetFirmwareEnvironmentVariableEx("LBLDESP", "{871455D0-5576-4FB8-9865-AF0824463B9E}", data, (uint)data.Length, out attributes);
                if (size == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "读取固件 Logo 参数失败");
                if (size != 10) throw new NotSupportedException("LBLDESP 长度与已分析结构不同 " + size);
                return new { Enabled = data[0], Width = BitConverter.ToInt32(data, 1), Height = BitConverter.ToInt32(data, 5),
                    FormatMask = data[9], Attributes = attributes, Raw = BitConverter.ToString(data, 0, (int)size),
                    Scope = "已读取固件变量。EFI 图片和实际开机显示需单独核实。" };
            }
        }

        public static string PrepareImage(string source, string outputDirectory, int width, int height)
        {
            if (width < 1 || height < 1 || width > 8192 || height > 8192)
                throw new ArgumentOutOfRangeException("width", "分辨率须在 1 到 8192 之间。");
            string destination = Path.GetFullPath(outputDirectory);
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            foreach (string protectedPath in new[] { windows, programFiles, programFilesX86 })
                if (!String.IsNullOrEmpty(protectedPath) && (destination + "\\").StartsWith(protectedPath.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("请选择工作目录或用户文件夹保存准备好的图片。");
            if (destination.IndexOf("\\EFI", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("图片准备输出目录不能为 EFI 路径。");
            if (new FileInfo(source).Length > 52L * 1024 * 1024) throw new IOException("图片超过 52 MiB。");
            string output = Path.Combine(destination, "mylogo_" + width + "x" + height + ".bmp");
            using (var input = File.OpenRead(source))
            using (var image = Image.FromStream(input, true, true))
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                if (image.RawFormat.Guid != ImageFormat.Png.Guid && image.RawFormat.Guid != ImageFormat.Bmp.Guid)
                    throw new NotSupportedException("图片准备目前支持 PNG 和 BMP。");
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.Black);
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    double ratio = Math.Min((double)width / image.Width, (double)height / image.Height);
                    int w = Math.Max(1, (int)(image.Width * ratio));
                    int h = Math.Max(1, (int)(image.Height * ratio));
                    graphics.DrawImage(image, (width - w) / 2, (height - h) / 2, w, h);
                }
                Directory.CreateDirectory(destination);
                using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write)) bitmap.Save(file, ImageFormat.Bmp);
            }
            return output;
        }

        [DllImport("kernel32.dll", EntryPoint = "GetFirmwareEnvironmentVariableExW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetFirmwareEnvironmentVariableEx(string name, string guid, byte[] data, uint size, out uint attributes);

        internal sealed class FirmwareReadPrivilege : IDisposable
        {
            private IntPtr token;
            private TokenPrivileges previous;
            private bool changed;
            public FirmwareReadPrivilege()
            {
                try
                {
                    if (!OpenProcessToken(GetCurrentProcess(), 0x28, out token)) throw new Win32Exception(Marshal.GetLastWin32Error());
                    var state = new TokenPrivileges { Count = 1, Attributes = 2 };
                    if (!LookupPrivilegeValue(null, "SeSystemEnvironmentPrivilege", out state.Id)) throw new Win32Exception(Marshal.GetLastWin32Error());
                    uint returned;
                    if (!AdjustTokenPrivileges(token, false, ref state, (uint)Marshal.SizeOf(state), out previous, out returned))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    changed = true;
                    int error = Marshal.GetLastWin32Error();
                    if (error != 0) throw new Win32Exception(error, "当前令牌缺少固件读取权限，可从管理员终端执行只读查询");
                }
                catch { Dispose(); throw; }
            }
            public void Dispose()
            {
                if (token == IntPtr.Zero) return;
                if (changed)
                {
                    TokenPrivileges ignored;
                    uint size;
                    AdjustTokenPrivileges(token, false, ref previous, (uint)Marshal.SizeOf(previous), out ignored, out size);
                }
                CloseHandle(token);
                token = IntPtr.Zero;
            }
            [StructLayout(LayoutKind.Sequential)]
            private struct Luid { public uint Low; public int High; }
            [StructLayout(LayoutKind.Sequential)]
            private struct TokenPrivileges { public uint Count; public Luid Id; public uint Attributes; }
            [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
            [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr value);
            [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool LookupPrivilegeValue(string system, string name, out Luid value);
            [DllImport("advapi32.dll", SetLastError = true)] private static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TokenPrivileges value, uint length, out TokenPrivileges oldValue, out uint returned);
        }
    }

    public sealed class HardwareLease : IDisposable
    {
        private readonly Mutex mutex = new Mutex(false, @"Global\LiteLX.Hardware");
        private bool owned;
        public HardwareLease()
        {
            try { owned = mutex.WaitOne(0); }
            catch (AbandonedMutexException) { owned = true; }
            if (!owned) { mutex.Dispose(); throw new InvalidOperationException("已有本工具硬件操作正在运行。"); }
        }
        public void Dispose() { if (owned) { mutex.ReleaseMutex(); owned = false; } mutex.Dispose(); }
    }
}
