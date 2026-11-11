using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.Win32;
using LiteLX;

internal static class LogoOperationHost
{
    private static int Main(string[] args)
    {
        // 此执行器不构造任何 WPF 窗口，仅响应主程序生成的明确操作请求。
        if (args.Length != 2 || args[0] != "--apply") return 2;
        string directory = Path.GetFullPath(args[1]);
        EfiVolume volume = null;
        try
        {
            var request = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(directory, "request.json")));
            string action = request["Action"];
            if (action != "save" && action != "restore") throw new ArgumentException("开机画面操作无效。");
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                throw new UnauthorizedAccessException("保存开机画面需要管理员权限。");
            using (var bios = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS", false))
                if (bios == null || !String.Equals(Convert.ToString(bios.GetValue("SystemProductName")), "82TF", StringComparison.OrdinalIgnoreCase) ||
                    !String.Equals(Convert.ToString(bios.GetValue("SystemManufacturer")), "LENOVO", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("当前直接保存接口仅核实 Lenovo 82TF。");
            VerifyModule();
            using (var lease = new HardwareLease())
            using (var privilege = new LogoTools.FirmwareReadPrivilege())
            {
                string image = action == "save" ? Path.Combine(directory, "input.bmp") : null;
                string preview;
                volume = new EfiVolume();
                using (volume)
                    preview = LogoInstallation.Apply(new NativeLogoFirmware(), Path.Combine(volume.Root, @"EFI\Lenovo\Logo"), image, directory);
                string message = action == "save" ? "开机画面已保存并回读校验，请重启电脑查看效果。" : "已恢复默认开机画面并回读校验，请重启电脑查看效果。";
                LogoInstallation.WriteJson(Path.Combine(directory, "result.json"), new { Success = true, Message = message + volume.CleanupWarning, Preview = preview });
            }
            return 0;
        }
        catch (Exception error)
        {
            try { LogoInstallation.WriteJson(Path.Combine(directory, "result.json"), new { Success = false, Message = error.ToString() + (volume == null ? "" : volume.CleanupWarning), Preview = (string)null }); }
            catch (Exception) { }
            return 1;
        }
    }

    private static void VerifyModule()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AIToolAPI.dll");
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create())
            if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != "dbfdfd8d28e2580d8defdeee710f9bf5a251c3d2ef5ddc323aeeeb5198ebb5fc")
                throw new IOException("原厂开机画面接口文件校验失败。");
    }

    private sealed class EfiVolume : IDisposable
    {
        public string Root { get; private set; }
        public string CleanupWarning { get; private set; }
        private string volumeName;

        public EfiVolume()
        {
            string[] drives = Directory.GetLogicalDrives();
            char letter = Enumerable.Range('D', 'Z' - 'D' + 1).Select(value => (char)value).Reverse()
                .FirstOrDefault(value => !drives.Contains(value + @":\", StringComparer.OrdinalIgnoreCase));
            if (letter == '\0') throw new IOException("没有可用盘符用于读取 EFI 系统分区。");
            Root = letter + @":\";
            Mount("/s");
            try { volumeName = VolumeName(); }
            catch
            {
                Mount("/d");
                throw;
            }
        }

        private string VolumeName()
        {
            var value = new StringBuilder(1024);
            if (!GetVolumeNameForVolumeMountPoint(Root, value, value.Capacity)) throw new Win32Exception(Marshal.GetLastWin32Error());
            return value.ToString();
        }

        private void Mount(string option)
        {
            string system = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), Environment.Is64BitOperatingSystem ? "Sysnative" : "System32");
            var start = new ProcessStartInfo(Path.Combine(system, "mountvol.exe"), Root.Substring(0, 2) + " " + option) {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            using (var process = Process.Start(start))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new IOException("EFI 分区挂载操作失败。" + output + error);
            }
        }

        public void Dispose()
        {
            // 仅解除本次创建且仍指向同一卷的挂载点。
            try
            {
                if (volumeName != null && VolumeName() == volumeName) Mount("/d");
            }
            catch (Exception error) { CleanupWarning = "\n临时 EFI 盘符清理失败 " + Root + " " + error.Message; }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetVolumeNameForVolumeMountPoint(string mountPoint, StringBuilder name, int length);
    }

    private sealed class NativeLogoFirmware : ILogoFirmware
    {
        public LogoFirmwareState Read()
        {
            byte enabled = 0;
            uint formats = 0;
            int height = 0, width = 0;
            if (GetLogoDIYInfo(ref enabled, ref formats, ref height, ref width) <= 0) throw new IOException("读取原厂开机画面参数失败。");
            var descriptor = ReadVariable("LBLDESP", "{871455D0-5576-4FB8-9865-AF0824463B9E}");
            var check = ReadVariable("LBLDVC", "{871455D1-5576-4FB8-9865-AF0824463C9F}");
            if (descriptor.Length != 10 || check.Length != 40) throw new NotSupportedException("固件变量长度与已分析接口不符。");
            if (descriptor[0] != enabled || BitConverter.ToInt32(descriptor, 1) != width || BitConverter.ToInt32(descriptor, 5) != height || descriptor[9] != formats)
                throw new IOException("原厂接口与固件变量的读取结果不符。");
            return new LogoFirmwareState { Enabled = enabled, Formats = formats, Width = width, Height = height };
        }

        public void SetEnabled(byte enabled)
        {
            if (SetLogoDIYInfo(enabled) <= 0 || Read().Enabled != enabled) throw new IOException("设置开机画面启用状态或回读失败。");
        }

        public void SetCheck(string image)
        {
            if (SetLogDIYCheck(image) <= 0) throw new IOException("写入开机画面校验信息失败。");
            ReadVariable("LBLDVC", "{871455D1-5576-4FB8-9865-AF0824463C9F}");
        }

        private static byte[] ReadVariable(string name, string space)
        {
            byte[] data = new byte[32768];
            uint attributes;
            uint size = GetFirmwareEnvironmentVariableEx(name, space, data, (uint)data.Length, out attributes);
            if (size == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "读取固件变量失败 " + name);
            return data.Take((int)size).ToArray();
        }

        [DllImport("AIToolAPI.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int GetLogoDIYInfo(ref byte enabled, ref uint formats, ref int height, ref int width);
        [DllImport("AIToolAPI.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int SetLogoDIYInfo(byte enabled);
        [DllImport("AIToolAPI.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] private static extern int SetLogDIYCheck(string path);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern uint GetFirmwareEnvironmentVariableEx(string name, string space, byte[] data, uint length, out uint attributes);
    }
}
