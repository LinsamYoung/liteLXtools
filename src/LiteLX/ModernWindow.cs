using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LiteLX
{
    internal sealed class ModernWindow
    {
        public readonly Window Window;
        public bool Busy { get; private set; }
        private string detail = "就绪";
        private string selectedLogo;
        private BitmapSource initialLogo;
        private CancellationTokenSource fan;
        private bool closeAfterWork;
        private List<MenuDisplay> menus = new List<MenuDisplay>();

        public ModernWindow()
        {
            using (var stream = typeof(ModernWindow).Assembly.GetManifestResourceStream("LiteLX.ModernWindow.xaml"))
                Window = (Window)XamlReader.Load(stream);
            using (var stream = typeof(ModernWindow).Assembly.GetManifestResourceStream("LiteLX.App.ico"))
                Window.Icon = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            Window.Loaded += async delegate {
                string machineName = await Task.Run(new Func<string>(MachineIdentity.ReadName));
                Get<TextBlock>("MachineModel").Text = machineName + "  ·  本地运行";
            };
            string[] pages = { "Logo", "Menu", "Fan" };
            foreach (string page in pages)
            {
                string selected = page;
                Get<RadioButton>("Nav" + page).Checked += delegate { Select(selected); };
            }
            Window.SourceInitialized += delegate { ModernStyle.WindowCorners(new WindowInteropHelper(Window).Handle); };
            Window.Closing += delegate(object sender, System.ComponentModel.CancelEventArgs e)
            {
                if (!Busy) return;
                e.Cancel = true; closeAfterWork = true;
                if (fan != null) fan.Cancel();
                SetStatus("正在结束操作并完成清理…");
            };
            Window.Closed += delegate { foreach (var item in menus) item.Dispose(); MenuPresentation.ClearCache(); };
            Click("Details", delegate { MessageBox.Show(Window, detail, "操作详情"); });
            LoadInitialLogo();
            Click("UploadLogo", delegate {
                var input = new Microsoft.Win32.OpenFileDialog { Filter = "BMP 图片|*.bmp" };
                if (input.ShowDialog(Window) != true) return;
                try { PreviewLogo(input.FileName); }
                catch (Exception ex) { MessageBox.Show(Window, ex.Message, "图片读取失败"); }
            });
            Click("CancelLogo", delegate { selectedLogo = null; Get<Image>("LogoPreview").Source = initialLogo; SetStatus("已取消本次图片选择"); });
            Click("SaveLogo", delegate {
                if (selectedLogo == null) { SetStatus("请先选择要保存的 BMP 图片"); return; }
                string source = selectedLogo;
                Run(delegate { return OriginalModules.ApplyLogo(source); }, delegate { initialLogo = Get<Image>("LogoPreview").Source as BitmapSource; selectedLogo = null; });
            });
            Click("RestoreLogo", delegate {
                Run(delegate { return OriginalModules.ApplyLogo(null); }, value => { selectedLogo = null; LoadInitialLogo(false); SetStatus(value); });
            });
            Click("ReadFan", delegate { Run(delegate {
                using (var lease = new HardwareLease()) using (var device = new WmiFanDevice(false))
                    return device.FanStatus() == 3 ? "风扇全速正在运行" : "风扇全速已关闭";
            }); });
            Click("StopFan", delegate { if (fan != null) fan.Cancel(); });
            Click("RunFan", delegate {
                int seconds = Int32.Parse((string)((ComboBoxItem)Get<ComboBox>("FanDuration").SelectedItem).Tag);
                fan = new CancellationTokenSource(); Get<Button>("StopFan").IsEnabled = true;
                Run(delegate {
                    using (var lease = new HardwareLease()) using (var device = new WmiFanDevice(true))
                        HardwareControl.RunFan(device, seconds, fan.Token);
                    return "除尘结束，已回读确认停止。";
                });
            });
            Click("RefreshMenus", delegate { LoadMenus(); });
            Get<TextBox>("MenuSearch").TextChanged += delegate { BindMenus(); };
            Get<ComboBox>("MenuScope").SelectionChanged += delegate { BindMenus(); };
        }

        public void PreviewLogo(string path)
        {
            Get<Image>("LogoPreview").Source = LoadBitmap(path);
            selectedLogo = path;
            SetStatus("已更新预览，点击确认保存开机画面");
        }
        private static BitmapSource LoadBitmap(string path)
        {
            using (var stream = File.OpenRead(path)) {
                var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze(); return image;
            }
        }
        private void LoadInitialLogo(bool useCached = true)
        {
            string cached = Path.Combine(Path.GetTempPath(), "mylogo_2560x1600.bmp");
            initialLogo = null;
            try { if (useCached && File.Exists(cached)) initialLogo = LoadBitmap(cached); }
            catch (Exception ex) { detail = ex.Message; }
            Get<Image>("LogoPreview").Source = initialLogo;
            SetStatus(initialLogo == null ? "请选择 BMP 图片以预览开机画面" : "预览为上次读取的缓存；选择图片后替换预览");
        }

        private T Get<T>(string name) where T : class { return (T)Window.FindName(name); }
        private void Click(string name, Action action) { Get<Button>(name).Click += delegate { if (!Busy || name == "StopFan" || name == "Details") action(); }; }
        private void SetStatus(string text) { Get<TextBlock>("Status").Text = text.Replace("\r", " ").Replace("\n", " "); }
        public void Select(string selected)
        {
            foreach (string page in new[] { "Logo", "Menu", "Fan" }) Get<FrameworkElement>(page + "Page").Visibility = page == selected ? Visibility.Visible : Visibility.Collapsed;
            Get<RadioButton>("Nav" + selected).IsChecked = true;
            if (!Busy && selected != "Logo") SetStatus("就绪");
            if (selected == "Menu" && menus.Count == 0 && !Busy) LoadMenus();
        }
        private void Run(Func<string> action, Action<string> completed = null, bool refreshMenus = false)
        {
            RunAsync(() => Task.Run(action), completed, refreshMenus);
        }
        private async void RunAsync(Func<Task<string>> action, Action<string> completed = null, bool refreshMenus = false)
        {
            if (Busy) return;
            Busy = true; SetStatus("正在处理…");
            try {
                try { detail = await action(); SetStatus(detail); if (completed != null) completed(detail); }
                catch (OperationCanceledException ex) { detail = ex.Message; SetStatus(detail); }
                catch (Exception ex) { detail = ex.ToString(); SetStatus("操作未完成，点击详情查看原因"); }
                // 部分写入后失败也要回读，保持忙碌状态直到列表同步完成。
                if (refreshMenus) {
                    try { await RefreshMenus(); }
                    catch (Exception ex) {
                        detail += "\n列表自动更新失败：" + ex;
                        SetStatus("列表自动更新失败，点击详情查看原因");
                    }
                }
            }
            finally {
                Busy = false;
                Get<Button>("StopFan").IsEnabled = false;
                if (fan != null) { fan.Dispose(); fan = null; }
                if (closeAfterWork) Window.Close();
            }
        }
        private void LoadMenus()
        {
            RunAsync(RefreshMenus);
        }
        private async Task<string> RefreshMenus()
        {
            string status = null;
            var result = await Task.Run(delegate {
                var scan = ContextMenus.Scan();
                MenuPresentation.ClearCache();
                status = "已读取 " + scan.Items.Count + " 个注册项。名称来自系统资源、COM注册信息及应用描述。";
                return scan.Items.Select(x => MenuPresentation.IsCommon(x) ? MenuPresentation.Describe(x) : new MenuDisplay { Entry = x, Name = x.Label, Scope = "其他", Icon = System.Drawing.SystemIcons.Application.ToBitmap() }).ToList();
            });
            // 在界面线程替换列表，避免搜索和筛选访问已释放的图标。
            foreach (var item in menus) item.Dispose();
            menus = result;
            BindMenus();
            return status;
        }
        private static BitmapSource Picture(System.Drawing.Image image)
        {
            using (var stream = new MemoryStream()) {
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Png); stream.Position = 0;
                var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze(); return bitmap;
            }
        }
        private void ChangeMenuState(MenuToggle toggle, bool enabled)
        {
            var selected = toggle.Entries;
            var paths = new HashSet<string>(selected.Select(x => x.Hive + "|" + x.KeyPath), StringComparer.OrdinalIgnoreCase);
            var clsids = new HashSet<string>(selected.Where(x => x.Kind == "Handler").Select(x => x.Clsid), StringComparer.OrdinalIgnoreCase);
            var affected = menus.Select(x => x.Entry).Where(x => paths.Contains(x.Hive + "|" + x.KeyPath) ||
                (x.Kind == "Handler" && clsids.Contains(x.Clsid))).ToList();
            RunAsync(async delegate {
                toggle.ShowPending(enabled);
                Exception failure = null;
                MenuScan states = null;
                await Task.Run(delegate {
                    try {
                        if (!enabled) ContextMenus.DisableGroup(selected.Where(x => x.CanDisable));
                        else foreach (var entry in selected.Where(x => x.CanEnable)) ContextMenus.Enable(entry);
                    }
                    catch (Exception ex) { failure = ex; }
                    // 部分写入失败也只回读相关条目，保留图标、分组和滚动位置。
                    try {
                        states = ContextMenus.ReadEntries(affected);
                        if (states.Warnings.Count != 0) throw new InvalidOperationException(String.Join("\n", states.Warnings));
                    }
                    catch (Exception ex) { failure = failure == null ? ex : new AggregateException(failure, ex); }
                });
                if (states != null) {
                    var current = states.Items.ToDictionary(x => x.Id);
                    foreach (var entry in affected) {
                        var state = current[entry.Id];
                        entry.Disabled = state.Disabled;
                        entry.CanDisable = state.CanDisable;
                        entry.CanEnable = state.CanEnable;
                        entry.Status = state.Status;
                    }
                }
                foreach (Grid row in Get<ListBox>("MenuList").Items)
                    foreach (var visibleToggle in row.Children.OfType<MenuToggle>()) visibleToggle.RefreshState(true);
                if (failure != null) throw new InvalidOperationException("菜单操作未完成，请查看详情。", failure);
                return enabled ? "已启用，状态已更新。" : "已禁用，状态已更新。";
            });
        }

        private void BindMenus()
        {
            var list = Get<ListBox>("MenuList"); list.Items.Clear();
            int scope = Get<ComboBox>("MenuScope").SelectedIndex;
            string search = Get<TextBox>("MenuSearch").Text;
            var groups = menus.Where(x => (scope == 2 || x.Scope == (scope == 0 ? "文件" : "桌面")) && (x.Name + x.Entry.KeyPath).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .GroupBy(MenuPresentation.GroupKey).OrderBy(x => x.First().Name).ToList();
            var repeatedNames = new HashSet<string>(groups.GroupBy(x => x.First().Name, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1).Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            var repeatedSources = new HashSet<string>(groups.GroupBy(x => x.First().Name + "|" + MenuPresentation.SourceName(x.First()), StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1).Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            Get<TextBlock>("MenuCount").Text = groups.Count + " 项右键入口";
            foreach (var group in groups)
            {
                var entries = group.Select(x => x.Entry).ToList(); var first = group.First();
                string displayName = first.Name;
                if (repeatedNames.Contains(displayName))
                {
                    // 不同功能使用同一名称时，补充注册来源以便分别管理。
                    string source = MenuPresentation.SourceName(first);
                    if (repeatedSources.Contains(displayName + "|" + source))
                        source = first.Entry.KeyPath.Substring(@"Software\Classes\".Length) + " · " + first.Entry.Hive + " " + first.Entry.View;
                    displayName += "（" + source + "）";
                }
                var row = new Grid { Margin = new Thickness(4, 0, 2, 0), Height = 30, ToolTip = String.Join("\n", entries.Select(x => x.Hive + " " + x.View + " " + x.KeyPath)) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) }); row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
                row.Children.Add(new Image { Source = Picture(first.Icon), Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center });
                var label = new TextBlock { Text = displayName, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, ToolTip = displayName };
                Grid.SetColumn(label, 1); row.Children.Add(label);
                var locations = new TextBlock { Text = entries.Count + " 个注册位置", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(10, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(locations, 2); row.Children.Add(locations);
                var toggle = new MenuToggle(displayName, entries);
                toggle.Click += delegate {
                    if (Busy) return;
                    ChangeMenuState(toggle, entries.All(x => x.Disabled));
                };
                Grid.SetColumn(toggle, 3); row.Children.Add(toggle); list.Items.Add(row);
            }
        }
    }
}
