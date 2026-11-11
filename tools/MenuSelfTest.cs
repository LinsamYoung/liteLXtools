using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LiteLX;
using Microsoft.Win32;

internal static class MenuSelfTest
{
    private const string Classes = @"Software\Classes\";
    private const string Blocked = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";
    private static readonly string FixtureName = "LiteLXMenuSelfTest_" + Guid.NewGuid().ToString("N");
    private static readonly string VerbPath = Classes + @"*\shell\" + FixtureName;
    private static readonly string HandlerPath = Classes + @"*\shellex\ContextMenuHandlers\" + FixtureName;
    private static readonly string HandlerPath2 = Classes + @"Directory\shellex\ContextMenuHandlers\" + FixtureName;
    private static readonly string Clsid = Guid.NewGuid().ToString("B").ToUpperInvariant();
    private static readonly string ClsidPath = Classes + @"CLSID\" + Clsid;
    private static readonly MethodInfo ReadEntry = typeof(ContextMenus).GetMethod("ReadEntry", BindingFlags.NonPublic | BindingFlags.Static);
    private static int assertions;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        assertions++;
    }

    private static MenuEntry Read(string path, string kind, RegistryView view = RegistryView.Registry64)
    {
        return (MenuEntry)ReadEntry.Invoke(null, new object[] { RegistryHive.CurrentUser, view, path, kind });
    }

    private static void ExpectRejected(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { assertions++; return; }
        throw new Exception("应拒绝定义已变化的过期条目。");
    }

    private static string BackupFiles()
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LiteLX", "MenuBackups");
        return Directory.Exists(directory) ? String.Join("\n", Directory.GetFiles(directory).OrderBy(x => x)) : "";
    }

    private static void TestVerb(RegistryKey root)
    {
        var entry = Read(VerbPath, "Verb");
        Check(entry.CanDisable && !entry.Disabled && !entry.CanEnable, "初始命令状态错误。");
        ContextMenus.DisableGroup(new[] { entry, entry });
        entry = Read(VerbPath, "Verb");
        Check(entry.Disabled && entry.CanEnable && !entry.CanDisable, "禁用后应直接支持启用。");
        using (var key = root.OpenSubKey(VerbPath))
            Check((string)key.GetValue("LegacyDisable") == "", "禁用应直接写入标准空字符串值。");
        ContextMenus.Enable(entry);
        ContextMenus.Enable(entry);
        Check(!Read(VerbPath, "Verb").Disabled, "重复启用应正常完成。");

        using (var key = root.OpenSubKey(VerbPath, true))
        {
            // 验证旧版工具标记及其他程序设置的不同类型禁用值。
            key.SetValue("LegacyDisable", "LiteLX:" + Guid.NewGuid().ToString("D"));
            entry = Read(VerbPath, "Verb");
            Check(entry.CanEnable, "旧版标记识别失败。");
            ContextMenus.Enable(entry);
            Check(!Read(VerbPath, "Verb").Disabled, "旧版标记撤销失败。");

            key.SetValue("LegacyDisable", "ExternalTool");
            entry = Read(VerbPath, "Verb");
            Check(entry.CanEnable, "外部禁用项应支持直接启用。");
            ContextMenus.Enable(entry);
            Check(!Read(VerbPath, "Verb").Disabled, "外部禁用状态清除失败。");
            key.SetValue("LegacyDisable", 0, RegistryValueKind.DWord);
            ContextMenus.Enable(Read(VerbPath, "Verb"));
            Check(!key.GetValueNames().Contains("LegacyDisable"), "数值类型禁用值应支持清除。");

            entry = Read(VerbPath, "Verb");
            key.SetValue("ProgrammaticAccessOnly", "ExternalTool");
            Check(Read(VerbPath, "Verb").CanEnable, "仅隐藏的入口应支持直接启用。");
            ContextMenus.Enable(entry);
            Check(!Read(VerbPath, "Verb").Disabled, "隐藏状态清除失败。");

            ContextMenus.Disable(Read(VerbPath, "Verb"));
            entry = Read(VerbPath, "Verb");
            key.SetValue("MUIVerb", "Changed fixture");
            ExpectRejected(() => ContextMenus.Enable(entry));
            Check(Read(VerbPath, "Verb").CanEnable, "失败后应保留工具标记。");
            ContextMenus.Enable(Read(VerbPath, "Verb"));

            ContextMenus.Disable(Read(VerbPath, "Verb"));
            key.SetValue("ProgrammaticAccessOnly", "");
            entry = Read(VerbPath, "Verb");
            ContextMenus.Enable(entry);
            ContextMenus.Enable(entry);
            entry = Read(VerbPath, "Verb");
            Check(!entry.Disabled && entry.CanDisable, "叠加禁用和隐藏状态应同时清除。");
            Check(!key.GetValueNames().Contains("ProgrammaticAccessOnly"), "隐藏值应清除。");
        }
    }

    private static void TestHandler(RegistryKey root)
    {
        var entry = Read(HandlerPath, "Handler");
        var related = Read(HandlerPath2, "Handler");
        Check(entry.CanDisable && related.CanDisable, "测试扩展应可禁用。");
        ContextMenus.DisableGroup(new[] { entry, related });
        entry = Read(HandlerPath, "Handler");
        related = Read(HandlerPath2, "Handler");
        Check(entry.CanEnable && related.CanEnable, "同一 CLSID 的入口应同步禁用。");
        ContextMenus.Enable(entry);
        ContextMenus.Enable(related);
        Check(!Read(HandlerPath, "Handler").Disabled && !Read(HandlerPath2, "Handler").Disabled, "同一 CLSID 的入口应同步启用。");

        var otherView = Read(HandlerPath, "Handler", RegistryView.Registry32);
        if (otherView != null)
        {
            ContextMenus.DisableGroup(new[] { Read(HandlerPath, "Handler"), otherView });
            ContextMenus.Enable(Read(HandlerPath, "Handler"));
            ContextMenus.Enable(Read(HandlerPath, "Handler", RegistryView.Registry32));
            Check(!Read(HandlerPath, "Handler", RegistryView.Registry32).Disabled, "跨视图切换失败。");
        }

        using (var blocked = root.CreateSubKey(Blocked))
        {
            blocked.SetValue(Clsid, "ExternalTool");
            entry = Read(HandlerPath, "Handler");
            Check(entry.Disabled && entry.CanEnable, "外部扩展禁用项应支持直接启用。");
            ContextMenus.Enable(entry);
            Check(!blocked.GetValueNames().Contains(Clsid), "外部扩展禁用值应清除。");
            Check(!Read(HandlerPath2, "Handler").Disabled, "相关扩展入口应同步启用。");
            blocked.SetValue(Clsid, new byte[] { 1, 2 }, RegistryValueKind.Binary);
            ContextMenus.Enable(Read(HandlerPath, "Handler"));
            Check(!Read(HandlerPath, "Handler").Disabled, "二进制类型的扩展禁用值应支持清除。");
        }
    }

    private static void PumpUntil(Func<bool> completed)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(5) };
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        timer.Tick += delegate { if (completed() || DateTime.UtcNow >= deadline) frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
        timer.Stop();
        Check(completed(), "界面操作等待超时。");
    }

    private static MenuDisplay Display(MenuEntry entry, string name, string scope)
    {
        return new MenuDisplay { Entry = entry, Name = name, Scope = scope, Icon = System.Drawing.SystemIcons.Application.ToBitmap() };
    }

    private static MenuToggle ToggleFor(ListBox list, string path)
    {
        return list.Items.Cast<Grid>().SelectMany(x => x.Children.OfType<MenuToggle>()).Single(x => x.Entries.Any(entry => entry.KeyPath == path));
    }

    private static long ClickToggle(ModernWindow model, MenuToggle toggle)
    {
        var list = (ListBox)model.Window.FindName("MenuList");
        var rows = list.Items.Cast<object>().ToArray();
        var timer = Stopwatch.StartNew();
        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(model.Busy && !toggle.IsHitTestVisible, "点击后应立即反馈并防止重复提交。");
        PumpUntil(() => !model.Busy);
        timer.Stop();
        Check(rows.SequenceEqual(list.Items.Cast<object>()), "切换过程中应保留现有列表行。");
        Check(toggle.IsHitTestVisible, "操作结束后应恢复开关交互。");
        return timer.ElapsedMilliseconds;
    }

    private static void TestToggle(RegistryKey root)
    {
        var entry = Read(VerbPath, "Verb");
        var motion = new MenuToggle("动画测试", new List<MenuEntry> { entry });
        var position = (TranslateTransform)((Border)((Border)motion.Content).Child).RenderTransform;
        // 隐形宿主提供真实的 WPF 动画时钟，避免未挂载控件的时钟停留在首帧。
        var animationHost = new Window { Content = motion, Width = 100, Height = 60, Opacity = 0, ShowInTaskbar = false, ShowActivated = false };
        try {
            animationHost.Show();
            motion.ShowPending(false);
            if (SystemParameters.ClientAreaAnimation) {
                Check(position.HasAnimatedProperties && position.X > 13.9, "动画首帧应保持原来的滑块位置。");
                PumpUntil(() => position.X > 0 && position.X < 14);
                Check(position.X > 0 && position.X < 14, "动画应逐步更新滑块位置。");
            }
            motion.RefreshState(true);
            PumpUntil(() => Math.Abs(position.X - 14) < 0.01);
            Check(Math.Abs(position.X - 14) < 0.01, "反向动画应回到正确位置。");
        }
        finally { animationHost.Close(); }

        var model = new ModernWindow();
        var menus = (List<MenuDisplay>)typeof(ModernWindow).GetField("menus", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(model);
        var bind = typeof(ModernWindow).GetMethod("BindMenus", BindingFlags.Instance | BindingFlags.NonPublic);
        var list = (ListBox)model.Window.FindName("MenuList");
        menus.Add(Display(entry, "测试命令", "文件"));
        menus.Add(Display(Read(HandlerPath, "Handler"), "测试扩展", "文件"));
        menus.Add(Display(Read(HandlerPath2, "Handler"), "测试扩展", "桌面"));
        try
        {
            bind.Invoke(model, null);
            var toggle = ToggleFor(list, VerbPath);
            var icon = menus[0].Icon;
            long disableTime = ClickToggle(model, toggle);
            Check(menus[0].Entry.Disabled && menus[0].Entry.CanEnable, "禁用后应局部更新实际状态。");
            long enableTime = ClickToggle(model, toggle);
            Check(!menus[0].Entry.Disabled && toggle.IsEnabled, "同一开关应支持连续反向切换。");
            Check(Object.ReferenceEquals(icon, menus[0].Icon), "切换过程中应复用图标。");
            Check(menus.Count == 3, "局部回读应保持现有菜单集合。");
            Console.WriteLine("Menu toggle fixture disable=" + disableTime + "ms, enable=" + enableTime + "ms.");

            using (var key = root.OpenSubKey(VerbPath, true)) key.SetValue("MUIVerb", "Changed UI fixture");
            ClickToggle(model, toggle);
            Check(!menus[0].Entry.Disabled && (string)toggle.ToolTip == "禁用此入口", "写入失败后应回到实际启用状态。");
            Check(((TextBlock)model.Window.FindName("Status")).Text.Contains("操作未完成"), "写入失败应保留错误提示。");

            menus[0].Entry = Read(VerbPath, "Verb");
            var missing = Read(VerbPath, "Verb");
            missing.Id += "_missing";
            missing.KeyPath += "_missing";
            menus.Add(Display(missing, "测试命令", "文件"));
            bind.Invoke(model, null);
            toggle = ToggleFor(list, VerbPath);
            ClickToggle(model, toggle);
            Check(menus[0].Entry.Disabled && toggle.IsEnabled, "部分写入失败后应显示已完成的禁用状态。");
            Check(menus[3].Entry.Disabled && !menus[3].Entry.CanEnable, "已消失的注册位置应标记为不可操作。");
            ClickToggle(model, toggle);
            Check(!menus[0].Entry.Disabled, "部分失败后的有效入口应支持直接启用。");

            toggle = ToggleFor(list, HandlerPath);
            ClickToggle(model, toggle);
            Check(menus[1].Entry.Disabled && menus[2].Entry.Disabled, "其他页面的同一扩展入口也应更新。");
            ClickToggle(model, toggle);
            Check(!menus[1].Entry.Disabled && !menus[2].Entry.Disabled, "相关扩展入口应同步启用。");
        }
        finally { model.Window.Close(); }
    }

    [STAThread]
    private static int Main()
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        string before = BackupFiles();
        using (var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
        {
            try
            {
                // 所有写入限定在本次随机创建的 HKCU 测试条目和 CLSID。
                using (var key = root.CreateSubKey(VerbPath + @"\command")) key.SetValue("", "LiteLXMenuSelfTest %1");
                using (var key = root.CreateSubKey(HandlerPath)) key.SetValue("", Clsid);
                using (var key = root.CreateSubKey(HandlerPath2)) key.SetValue("", Clsid);
                using (var key = root.CreateSubKey(ClsidPath + @"\InprocServer32")) key.SetValue("", "LiteLXMenuSelfTest.dll");
                TestVerb(root);
                TestHandler(root);
                TestToggle(root);
                Check(BackupFiles() == before, "切换菜单不应生成备份文件。");
                Console.WriteLine("PASS " + assertions + " menu assertions. Isolated temporary HKCU entries only.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally
            {
                root.DeleteSubKeyTree(VerbPath, false);
                root.DeleteSubKeyTree(HandlerPath, false);
                root.DeleteSubKeyTree(HandlerPath2, false);
                root.DeleteSubKeyTree(ClsidPath, false);
                foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                using (var viewRoot = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view))
                using (var blocked = viewRoot.OpenSubKey(Blocked, true))
                    if (blocked != null) blocked.DeleteValue(Clsid, false);
            }
        }
    }
}
