using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace LiteLX
{
    public sealed class MenuEntry
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Kind { get; set; }
        public RegistryHive Hive { get; set; }
        public RegistryView View { get; set; }
        public string KeyPath { get; set; }
        public string Command { get; set; }
        public string Clsid { get; set; }
        public bool Disabled { get; set; }
        public bool CanDisable { get; set; }
        public bool CanEnable { get; set; }
        public string Status { get; set; }
        public string Fingerprint { get; set; }
    }

    public sealed class MenuScan
    {
        public List<MenuEntry> Items = new List<MenuEntry>();
        public List<string> Warnings = new List<string>();
    }

    public static class ContextMenus
    {
        private const string Classes = @"Software\Classes\";
        private const string Blocked = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

        public static MenuScan Scan()
        {
            var result = new MenuScan();
            var views = Environment.Is64BitOperatingSystem
                ? new[] { RegistryView.Registry64, RegistryView.Registry32 }
                : new[] { RegistryView.Registry32 };
            foreach (var view in views)
            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            {
                using (var root = RegistryKey.OpenBaseKey(hive, view))
                using (var classes = root.OpenSubKey(@"Software\Classes", false))
                {
                    if (classes == null) continue;
                    string[] names;
                    try { names = classes.GetSubKeyNames(); }
                    catch (Exception ex) { result.Warnings.Add(hive + " " + view + " " + ex.Message); continue; }
                    var scopes = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
                    scopes.Add(@"Directory\Background");
                    scopes.Add(@"LibraryFolder\Background");
                    using (var associations = classes.OpenSubKey("SystemFileAssociations", false))
                    {
                        if (associations != null)
                            foreach (var name in associations.GetSubKeyNames()) scopes.Add(@"SystemFileAssociations\" + name);
                    }
                    foreach (var scope in scopes)
                    {
                        ReadContainer(result, root, hive, view, Classes + scope + @"\shell", "Verb");
                        ReadContainer(result, root, hive, view, Classes + scope + @"\shellex\ContextMenuHandlers", "Handler");
                        // 联想管家可能把原条目移至这些位置，保留为只读提示供人工核对。
                        ReadContainer(result, root, hive, view, Classes + scope + @"\unshell", "ArchivedVerb");
                        ReadContainer(result, root, hive, view, Classes + scope + @"\unshellex\ContextMenuHandlers", "ArchivedHandler");
                    }
                }
            }
            result.Items = result.Items.OrderBy(x => x.Label, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
            return result;
        }

        private static void ReadContainer(MenuScan scan, RegistryKey root, RegistryHive hive,
            RegistryView view, string path, string kind)
        {
            try
            {
                using (var container = root.OpenSubKey(path, false))
                {
                    if (container == null) return;
                    foreach (var name in container.GetSubKeyNames())
                    {
                        try
                        {
                            var entry = ReadEntry(hive, view, path + "\\" + name, kind);
                            if (entry != null) scan.Items.Add(entry);
                        }
                        catch (Exception ex) { scan.Warnings.Add(hive + " " + view + " " + path + "\\" + name + " " + ex.Message); }
                    }
                }
            }
            catch (Exception ex) { scan.Warnings.Add(hive + " " + view + " " + path + " " + ex.Message); }
        }

        private static MenuEntry ReadEntry(RegistryHive hive, RegistryView view, string path, string kind)
        {
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            using (var key = root.OpenSubKey(path, false))
            {
                if (key == null) return null;
                string name = path.Substring(path.LastIndexOf('\\') + 1);
                string title = ReadText(key, "MUIVerb");
                if (String.IsNullOrEmpty(title)) title = ReadText(key, "");
                string command = "";
                string clsid = "";
                bool isHandler = kind.EndsWith("Handler", StringComparison.Ordinal);
                bool archived = kind.StartsWith("Archived", StringComparison.Ordinal);
                bool disabled;
                if (isHandler)
                {
                    Guid guid;
                    string candidate = ReadText(key, "");
                    if (!Guid.TryParse(candidate, out guid)) Guid.TryParse(name, out guid);
                    if (guid != Guid.Empty) clsid = guid.ToString("B").ToUpperInvariant();
                    command = ResolveServer(view, clsid);
                    disabled = archived || IsBlocked(view, clsid);
                }
                else
                {
                    using (var commandKey = key.OpenSubKey("command", false))
                        if (commandKey != null) command = ReadText(commandKey, "");
                    if (String.IsNullOrEmpty(command)) command = ReadText(key, "ExplorerCommandHandler");
                    disabled = archived || HasValue(key, "LegacyDisable") || HasValue(key, "ProgrammaticAccessOnly");
                }
                var entry = new MenuEntry {
                    Id = Hash(hive + "|" + view + "|" + path.ToUpperInvariant()),
                    Label = String.IsNullOrEmpty(title) ? name : title,
                    Kind = kind, Hive = hive, View = view, KeyPath = path,
                    Command = command, Clsid = clsid, Disabled = disabled,
                    CanDisable = !archived && !disabled && (!isHandler || clsid.Length != 0),
                    CanEnable = !archived && disabled && (!isHandler || clsid.Length != 0),
                    Status = archived ? "管家迁移位置，仅供核对" : disabled ? "已有禁用或隐藏标记" : "已注册，实际显示由 Explorer 决定"
                };
                if (isHandler && !archived)
                    entry.Status += "；禁用会按 CLSID 影响当前用户的全部该扩展入口";
                entry.Fingerprint = Fingerprint(key, !isHandler, command);
                return entry;
            }
        }

        internal static MenuScan ReadEntries(IEnumerable<MenuEntry> entries)
        {
            var result = new MenuScan();
            foreach (var entry in entries)
            {
                try {
                    var current = ReadEntry(entry.Hive, entry.View, entry.KeyPath, entry.Kind);
                    result.Items.Add(current ?? new MenuEntry { Id = entry.Id, Disabled = true, Status = "注册项已不存在，请刷新列表。" });
                }
                catch (Exception ex) {
                    result.Items.Add(new MenuEntry { Id = entry.Id, Disabled = entry.Disabled, Status = "状态读取失败，请刷新列表。" });
                    result.Warnings.Add(ex.ToString());
                }
            }
            return result;
        }

        internal static void DisableGroup(IEnumerable<MenuEntry> entries)
        {
            foreach (var entry in entries)
            {
                // 同一扩展的其他入口或共享注册表视图可能已被前一次写入禁用。
                var current = ReadEntry(entry.Hive, entry.View, entry.KeyPath, entry.Kind);
                if (current != null && current.Disabled) continue;
                Disable(entry);
            }
        }

        public static string Disable(MenuEntry selected)
        {
            if (selected == null) throw new ArgumentNullException("selected");
            ValidateEntry(selected);
            using (var mutex = AcquireLock())
            {
                var current = ReadEntry(selected.Hive, selected.View, selected.KeyPath, selected.Kind);
                if (current == null || current.Fingerprint != selected.Fingerprint || current.Id != selected.Id)
                    throw new InvalidOperationException("菜单条目已变化，请刷新列表后重试。");
                if (current.Disabled) return "已禁用。";
                if (!current.CanDisable) throw new InvalidOperationException("该条目缺少有效 CLSID。");
                var targetHive = current.Kind == "Handler" ? RegistryHive.CurrentUser : current.Hive;
                var targetPath = current.Kind == "Handler" ? Blocked : current.KeyPath;
                var valueName = current.Kind == "Handler" ? current.Clsid : "LegacyDisable";
                using (var root = RegistryKey.OpenBaseKey(targetHive, current.View))
                using (var target = current.Kind == "Handler"
                    ? root.CreateSubKey(targetPath) : root.OpenSubKey(targetPath, true))
                {
                    if (target == null) throw new InvalidOperationException("目标注册表项已不存在。");
                    var latest = ReadEntry(current.Hive, current.View, current.KeyPath, current.Kind);
                    if (latest == null || latest.Fingerprint != current.Fingerprint)
                        throw new InvalidOperationException("写入前发现条目发生变化，请刷新。");
                    target.SetValue(valueName, "", RegistryValueKind.String);
                    if (!HasValue(target, valueName))
                        throw new InvalidOperationException("禁用标记校验失败，请刷新列表核对状态。");
                }
                return "已禁用。";
            }
        }

        public static void Enable(MenuEntry entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");
            ValidateEntry(entry);
            using (var mutex = AcquireLock())
            {
                var current = ReadEntry(entry.Hive, entry.View, entry.KeyPath, entry.Kind);
                if (current == null || current.Fingerprint != entry.Fingerprint || current.Id != entry.Id)
                    throw new InvalidOperationException("菜单条目已变化，请刷新列表后重试。");
                if (!current.Disabled) return;
                bool isHandler = entry.Kind == "Handler";
                var targetHives = isHandler ? new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine } : new[] { entry.Hive };
                string targetPath = isHandler ? Blocked : entry.KeyPath;
                var valueNames = isHandler ? new[] { entry.Clsid } : new[] { "LegacyDisable", "ProgrammaticAccessOnly" };
                // 启用时清除所有参与状态判断的禁用值，包括当前用户和整机的扩展屏蔽项。
                foreach (var hive in targetHives)
                    RemoveDisableValues(hive, entry.View, targetPath, valueNames);
                var latest = ReadEntry(entry.Hive, entry.View, entry.KeyPath, entry.Kind);
                if (latest == null || latest.Disabled)
                    throw new InvalidOperationException("启用后仍检测到禁用状态，请刷新列表核对。");
            }
        }

        private static void RemoveDisableValues(RegistryHive hive, RegistryView view, string path, string[] valueNames)
        {
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            {
                // 先只读检查，存在对应值时再申请写权限。
                using (var key = root.OpenSubKey(path, false))
                    if (key == null || !valueNames.Any(name => HasValue(key, name))) return;
                using (var target = root.OpenSubKey(path, true))
                {
                    if (target == null) return;
                    foreach (string name in valueNames) target.DeleteValue(name, false);
                    if (valueNames.Any(name => HasValue(target, name)))
                        throw new InvalidOperationException("禁用状态清除失败，请刷新列表核对。");
                }
            }
        }

        private static void ValidateEntry(MenuEntry entry)
        {
            if (entry.Hive != RegistryHive.CurrentUser && entry.Hive != RegistryHive.LocalMachine)
                throw new InvalidOperationException("不支持此注册表根。");
            if (entry.View != RegistryView.Registry32 && entry.View != RegistryView.Registry64)
                throw new InvalidOperationException("必须指定注册表视图。");
            if (String.IsNullOrEmpty(entry.KeyPath) || !entry.KeyPath.StartsWith(Classes, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("菜单注册表路径无效。");
            string section = entry.Kind == "Handler" ? @"\shellex\ContextMenuHandlers\" : @"\shell\";
            int position = entry.KeyPath.LastIndexOf(section, StringComparison.OrdinalIgnoreCase);
            if ((entry.Kind != "Verb" && entry.Kind != "Handler") || position < Classes.Length ||
                entry.KeyPath.Substring(position + section.Length).IndexOf('\\') >= 0 ||
                entry.KeyPath.Length == position + section.Length)
                throw new InvalidOperationException("仅支持直接注册的菜单命令与 ContextMenuHandlers。");
            Guid clsid;
            if (entry.Kind == "Handler" && (!Guid.TryParse(entry.Clsid, out clsid) || clsid == Guid.Empty))
                throw new InvalidOperationException("扩展 CLSID 无效。");
        }

        private static MutexLease AcquireLock()
        {
            return new MutexLease();
        }

        private sealed class MutexLease : IDisposable
        {
            private readonly Mutex mutex;
            public MutexLease()
            {
                string sid = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
                mutex = new Mutex(false, @"Local\LiteLX.ContextMenus." + sid);
                bool acquired;
                try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(10)); }
                catch (AbandonedMutexException) { acquired = true; }
                if (!acquired) { mutex.Dispose(); throw new InvalidOperationException("另一个菜单操作仍在进行。"); }
            }
            public void Dispose() { mutex.ReleaseMutex(); mutex.Dispose(); }
        }

        private static string ResolveServer(RegistryView view, string clsid)
        {
            if (String.IsNullOrEmpty(clsid)) return "";
            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            using (var key = root.OpenSubKey(Classes + @"CLSID\" + clsid + @"\InprocServer32", false))
            {
                if (key != null) return ReadText(key, "");
            }
            return "";
        }

        private static bool IsBlocked(RegistryView view, string clsid)
        {
            if (String.IsNullOrEmpty(clsid)) return false;
            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            using (var key = root.OpenSubKey(Blocked, false))
                if (key != null && HasValue(key, clsid)) return true;
            return false;
        }

        private static bool HasValue(RegistryKey key, string name)
        {
            return key.GetValueNames().Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        private static string ReadText(RegistryKey key, string name)
        {
            return key.GetValue(name, "", RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? "";
        }

        private static string Fingerprint(RegistryKey key, bool excludeDisableState, string command)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                int nodes = 0;
                WriteKey(writer, key, excludeDisableState, 0, ref nodes);
                writer.Write(command ?? "");
                writer.Flush();
                using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "");
            }
        }

        private static void WriteKey(BinaryWriter writer, RegistryKey key, bool excludeDisableState, int depth, ref int nodes)
        {
            if (++nodes > 1000 || depth > 32) throw new InvalidOperationException("菜单定义过大，已跳过。");
            foreach (var name in key.GetValueNames().OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                if (depth == 0 && excludeDisableState &&
                    (String.Equals(name, "LegacyDisable", StringComparison.OrdinalIgnoreCase) || String.Equals(name, "ProgrammaticAccessOnly", StringComparison.OrdinalIgnoreCase))) continue;
                writer.Write("value"); writer.Write(name); writer.Write((int)key.GetValueKind(name));
                object value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                var bytes = value as byte[];
                var strings = value as string[];
                if (bytes != null) { writer.Write(bytes.Length); writer.Write(bytes); }
                else if (strings != null) { writer.Write(strings.Length); foreach (var item in strings) writer.Write(item); }
                else writer.Write(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "");
            }
            foreach (var name in key.GetSubKeyNames().OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                writer.Write("key"); writer.Write(name);
                using (var child = key.OpenSubKey(name, false))
                {
                    if (child == null) throw new InvalidOperationException("菜单定义正在变化。");
                    WriteKey(writer, child, false, depth + 1, ref nodes);
                }
            }
            writer.Write("end");
        }

        private static string Hash(string text)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
        }
    }
}
