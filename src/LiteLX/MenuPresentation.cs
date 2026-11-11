using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace LiteLX
{
    internal sealed class MenuDisplay : IDisposable
    {
        public MenuEntry Entry;
        public string Name;
        public string Scope;
        public Image Icon;
        public void Dispose() { if (Icon != null) Icon.Dispose(); }
    }

    internal static class MenuPresentation
    {
        private static readonly Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Image> icons = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        public static void ClearCache()
        {
            foreach (var image in icons.Values) image.Dispose();
            icons.Clear(); names.Clear();
        }
        public static bool IsCommon(MenuEntry entry)
        {
            return Regex.IsMatch(entry.KeyPath, @"^Software\\Classes\\(\*|AllFilesystemObjects|Directory|DesktopBackground|Folder|Drive|SystemFileAssociations)\\", RegexOptions.IgnoreCase);
        }
        public static MenuDisplay Describe(MenuEntry entry)
        {
            string label = Resolve(entry.Label), icon = "", server = entry.Command;
            using (var root = RegistryKey.OpenBaseKey(entry.Hive, entry.View))
            using (var key = root.OpenSubKey(entry.KeyPath))
                if (key != null) icon = Convert.ToString(key.GetValue("Icon", "", RegistryValueOptions.DoNotExpandEnvironmentNames));
            // 优先使用系统本地化资源、COM注册名和文件版本信息。
            if (!String.IsNullOrEmpty(entry.Clsid))
                foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
                    using (var root = RegistryKey.OpenBaseKey(hive, entry.View))
                    using (var key = root.OpenSubKey(@"Software\Classes\CLSID\" + entry.Clsid))
                        if (key != null)
                        {
                            string name = Resolve(Convert.ToString(key.GetValue("")));
                            if (!String.IsNullOrWhiteSpace(name) && (label == entry.Clsid || label == Path.GetFileName(entry.KeyPath) || label.StartsWith("{"))) label = name;
                            using (var defaultIcon = key.OpenSubKey("DefaultIcon"))
                                if (String.IsNullOrEmpty(icon) && defaultIcon != null) icon = Convert.ToString(defaultIcon.GetValue(""));
                        }
            string binary = BinaryPath(server);
            string application = FindApplication(binary);
            if (entry.Kind.EndsWith("Handler", StringComparison.Ordinal))
            {
                try {
                    if (File.Exists(application)) {
                        var version = FileVersionInfo.GetVersionInfo(application);
                        string product = version.ProductName;
                        if (!String.IsNullOrWhiteSpace(product) && product.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) < 0 && product.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) < 0) label = product;
                    }
                } catch (System.ComponentModel.Win32Exception) { }
            }
            if (String.IsNullOrWhiteSpace(label) || label.StartsWith("@") || label.StartsWith("{") || label.IndexOf('\\') >= 0)
            {
                try { if (File.Exists(binary)) label = FileVersionInfo.GetVersionInfo(binary).FileDescription; } catch (ArgumentException) { }
            }
            if (String.IsNullOrWhiteSpace(label) || label.StartsWith("@")) label = Path.GetFileName(entry.KeyPath);
            if (label.Equals("3D Print", StringComparison.OrdinalIgnoreCase)) label = "3D 打印";
            if (label.Equals("ModernSharing", StringComparison.OrdinalIgnoreCase)) label = "共享";
            if (label.Equals("edit", StringComparison.OrdinalIgnoreCase))
            {
                string editor = Path.GetFileNameWithoutExtension(binary);
                if (editor.Equals("notepad", StringComparison.OrdinalIgnoreCase)) editor = "记事本";
                if (editor.Equals("powershell_ise", StringComparison.OrdinalIgnoreCase)) editor = "PowerShell ISE";
                if (!String.IsNullOrWhiteSpace(editor)) label = "编辑（" + editor + "）";
            }
            string relative = entry.KeyPath.Substring(@"Software\Classes\".Length);
            bool desktop = relative.StartsWith(@"Directory\Background\", StringComparison.OrdinalIgnoreCase) || relative.StartsWith(@"DesktopBackground\", StringComparison.OrdinalIgnoreCase);
            bool common = Regex.IsMatch(relative, @"^(\*|AllFilesystemObjects|Directory|Folder|Drive|SystemFileAssociations)\\", RegexOptions.IgnoreCase);
            string scope = desktop ? "桌面" : common ? "文件" : "其他";
            if (label.StartsWith("{")) { label = "未识别扩展 " + label; scope = "其他"; }
            return new MenuDisplay { Entry = entry, Name = label.Replace("&", ""), Scope = scope, Icon = LoadIcon(String.IsNullOrEmpty(icon) ? application : icon) };
        }

        private static string FindApplication(string binary)
        {
            if (!File.Exists(binary) || !binary.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) return binary;
            string directory = Path.GetDirectoryName(binary);
            foreach (string name in new[] { "7zFM.exe", "Acrobat.exe", "WinRAR.exe", "Everything.exe", "LockHunter.exe", "OneDrive.exe" }) {
                string candidate = Path.Combine(directory, name);
                if (File.Exists(candidate)) return candidate;
                candidate = Path.Combine(directory, "..", name);
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            }
            return binary;
        }

        internal static string Resolve(string value)
        {
            if (String.IsNullOrEmpty(value)) return "";
            value = Environment.ExpandEnvironmentVariables(value);
            if (!value.StartsWith("@")) return value;
            // 包资源交由后续包管理适配处理，避免失效包使菜单扫描长时间阻塞。
            if (value.StartsWith("@{")) return value;
            string cached;
            if (names.TryGetValue(value, out cached)) return cached;
            var text = new StringBuilder(2048);
            cached = SHLoadIndirectString(value, text, text.Capacity, IntPtr.Zero) == 0 ? text.ToString() : value;
            names[value] = cached;
            return cached;
        }

        private static string BinaryPath(string command)
        {
            command = Environment.ExpandEnvironmentVariables(command ?? "").Trim();
            var match = Regex.Match(command, "^\"([^\"]+)\"|^(.+?\\.(?:exe|dll|ico))(?:[ ,]|$)", RegexOptions.IgnoreCase);
            return match.Success ? (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value) : command;
        }

        internal static string SourceName(MenuDisplay item)
        {
            string path = item.Entry.KeyPath;
            string source = Path.GetFileName(path);
            int section = path.LastIndexOf(@"\shell\", StringComparison.OrdinalIgnoreCase);
            if (source.Equals(item.Name, StringComparison.OrdinalIgnoreCase) && section >= 0)
                source = path.Substring(@"Software\Classes\".Length, section - @"Software\Classes\".Length);
            return source;
        }

        internal static string GroupKey(MenuDisplay item)
        {
            // 同一 COM 扩展的不同位数使用不同 DLL，按 CLSID 合并并保留全部注册位置。
            if (item.Entry.Kind.EndsWith("Handler", StringComparison.Ordinal) && !String.IsNullOrEmpty(item.Entry.Clsid))
                return "Handler|" + item.Entry.Clsid.ToUpperInvariant();

            string command = Environment.ExpandEnvironmentVariables(item.Entry.Command ?? "").Trim();
            string binary = BinaryPath(command);
            int offset = binary.Length + (command.StartsWith("\"", StringComparison.Ordinal) ? 2 : 0);
            string arguments = command.Substring(Math.Min(offset, command.Length)).Trim();
            // 系统程序的短路径、环境变量及文件参数引号写法统一后再比较。
            if (Regex.IsMatch(binary, @"^[\w.-]+\.exe$", RegexOptions.IgnoreCase))
            {
                string systemBinary = Path.Combine(Environment.SystemDirectory, binary);
                if (File.Exists(systemBinary)) binary = systemBinary;
            }
            arguments = Regex.Replace(arguments, "\"(%[1lL])\"", "$1");
            return "Verb|" + item.Name.Trim().ToUpperInvariant() + "|" + binary.ToUpperInvariant() + "|" + arguments;
        }

        private static Image LoadIcon(string resource)
        {
            resource = Environment.ExpandEnvironmentVariables(resource ?? "").Trim();
            Image cached;
            if (icons.TryGetValue(resource, out cached)) return (Image)cached.Clone();
            Image loaded = ExtractImage(resource);
            icons[resource] = loaded;
            return (Image)loaded.Clone();
        }

        private static Image ExtractImage(string resource)
        {
            int index = 0;
            var suffix = Regex.Match(resource, @",\s*(-?\d+)\s*$");
            if (suffix.Success) { index = Int32.Parse(suffix.Groups[1].Value); resource = resource.Substring(0, suffix.Index); }
            resource = BinaryPath(resource.Trim('"'));
            IntPtr large = IntPtr.Zero, small = IntPtr.Zero;
            try
            {
                if (File.Exists(resource) && ExtractIconEx(resource, index, out large, out small, 1) > 0)
                    return System.Drawing.Icon.FromHandle(large != IntPtr.Zero ? large : small).ToBitmap();
            }
            catch (ArgumentException) { }
            finally { if (large != IntPtr.Zero) DestroyIcon(large); if (small != IntPtr.Zero) DestroyIcon(small); }
            return SystemIcons.Application.ToBitmap();
        }
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)] private static extern int SHLoadIndirectString(string source, StringBuilder output, int size, IntPtr reserved);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern uint ExtractIconEx(string file, int index, out IntPtr large, out IntPtr small, uint count);
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);
    }
}
