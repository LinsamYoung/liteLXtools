using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace LiteLX
{
    internal static class Program
    {
        public static string Json(object value)
        {
            return new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024 }.Serialize(value);
        }

        [STAThread]
        private static int Main(string[] args)
        {
            ModernStyle.EnableDpi();
            if (args.Length == 0)
            {
                var application = new System.Windows.Application();
                var window = new ModernWindow();
                application.Run(window.Window);
                return 0;
            }
            try
            {
                if (args[0] != "--preview") Console.OutputEncoding = new UTF8Encoding(false);
                if (args[0] == "status") Console.WriteLine(Json(Inspection.Capture()));
                else if (args[0] == "menu") Menu(args);
                else if (args[0] == "logo") Logo(args);
                else if (args[0] == "fan") Fan(args);
                else if (args[0] == "module")
                {
                    if (args.Length == 2 && args[1] == "list") Console.WriteLine(Json(OriginalModules.Inventory()));
                    else throw new ArgumentException("module list");
                }
                else if (args[0] == "--preview" && args.Length >= 2)
                {
                    var application = new System.Windows.Application();
                    var window = new ModernWindow();
                    var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                    window.Window.ContentRendered += delegate {
                        if (args.Length > 2) window.Select(new[] { "Logo", "Menu", "Fan" }[Int32.Parse(args[2])]);
                        if (args.Length > 3) window.PreviewLogo(args[3]);
                        timer.Start();
                    };
                    timer.Tick += delegate {
                        if (window.Busy) return;
                        timer.Stop();
                        var content = window.Window;
                        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        var background = new System.Windows.Media.DrawingVisual();
                        using (var drawing = background.RenderOpen()) drawing.DrawRectangle(window.Window.Background, null, new System.Windows.Rect(0, 0, content.ActualWidth, content.ActualHeight));
                        bitmap.Render(background);
                        bitmap.Render(content);
                        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                        using (var file = File.Create(Path.GetFullPath(args[1]))) encoder.Save(file);
                        window.Window.Close();
                    };
                    application.Run(window.Window);
                }

                else { Console.WriteLine(Help); return args[0] == "help" || args[0] == "--help" ? 0 : 2; }
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex.ToString()); return 1; }
        }

        private static void Menu(string[] args)
        {
            string verb = args.Length > 1 ? args[1] : "list";
            if (verb == "list")
            {
                var scan = ContextMenus.Scan();
                string filter = args.Length > 2 ? args[2] : "";
                Console.WriteLine(Json(new { Items = scan.Items.Where(x => (x.Label + x.KeyPath + x.Command).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0), scan.Warnings }));
            }
            else if ((verb == "disable" || verb == "enable") && args.Length >= 3)
            {
                var entry = ContextMenus.Scan().Items.SingleOrDefault(x => x.Id == args[2]);
                if (entry == null) throw new ArgumentException("未找到菜单 ID，请刷新列表。");
                if (!args.Contains("--apply")) { Console.WriteLine(Json(new { Plan = (verb == "disable" ? "禁用" : "启用") + "所选菜单。添加 --apply 后写入注册表。", Entry = entry })); return; }
                if (verb == "disable") Console.WriteLine(ContextMenus.Disable(entry));
                else { ContextMenus.Enable(entry); Console.WriteLine("已启用。"); }
            }
            else throw new ArgumentException("菜单参数无效，参阅 help。");
        }

        private static void Logo(string[] args)
        {
            if (args.Length == 2 && args[1] == "status") Console.WriteLine(Json(LogoTools.ReadFirmware()));
            else if (args.Length == 6 && args[1] == "prepare")
                Console.WriteLine(LogoTools.PrepareImage(args[2], args[3], Int32.Parse(args[4], CultureInfo.InvariantCulture), Int32.Parse(args[5], CultureInfo.InvariantCulture)));
            else if (args.Length == 2 && args[1] == "plan") Console.WriteLine(LogoPlan);
            else throw new ArgumentException("Logo 参数无效，参阅 help。");
        }

        private static void Fan(string[] args)
        {
            if (args.Length == 2 && args[1] == "plan") { Console.WriteLine(FanPlan); return; }
            if (args.Length >= 2 && args[1] == "status")
            {
                if (!args.Contains("--confirm-hardware-query")) { Console.WriteLine(FanPlan + Environment.NewLine + "查询需显式添加 --confirm-hardware-query。"); return; }
                using (var lease = new HardwareLease())
                using (var device = CreateFan(args, false)) Console.WriteLine("fan normalized=0x" + device.FanStatus().ToString("x8"));
                return;
            }
            if (args.Length >= 3 && args[1] == "run")
            {
                int seconds = Int32.Parse(args[2], CultureInfo.InvariantCulture);
                if (seconds < 1 || seconds > 18) throw new ArgumentOutOfRangeException("seconds");
                if (!IsConfirmed(args)) { Console.WriteLine(FanPlan + Environment.NewLine + "计划周期 " + seconds + " 秒。"); return; }
                using (var cancellation = new CancellationTokenSource())
                {
                    ConsoleCancelEventHandler onCancel = delegate(object sender, ConsoleCancelEventArgs e) { e.Cancel = true; cancellation.Cancel(); };
                    Console.CancelKeyPress += onCancel;
                    try
                    {
                        using (var lease = new HardwareLease())
                        using (var device = CreateFan(args, true))
                        {
                            Console.WriteLine(DateTime.Now.ToString("o") + " 准备查询并执行除尘周期。");
                            HardwareControl.RunFan(device, seconds, cancellation.Token);
                            Console.WriteLine(DateTime.Now.ToString("o") + " 已发送停止并确认状态为关闭。");
                        }
                    }
                    finally { Console.CancelKeyPress -= onCancel; }
                }
                return;
            }
            if (args.Length >= 2 && args[1] == "stop")
            {
                if (!IsConfirmed(args)) { Console.WriteLine("人工停止会影响当前所有发起方的除尘状态。确认后添加 --execute --confirm-hardware。"); return; }
                using (var lease = new HardwareLease())
                using (var device = CreateFan(args, true))
                {
                    device.FanSet(false);
                    uint state = device.FanStatus();
                    Console.WriteLine("fan raw=0x" + state.ToString("x8"));
                    if ((state & 3) != 1) throw new IOException("停止回读未确认支持且关闭。");
                }
                return;
            }
            throw new ArgumentException("风扇参数无效，参阅 help。");
        }

        private static IFanDevice CreateFan(string[] args, bool writes)
        {
            if (args.Contains("--energy")) return new EnergyDevice(writes, Console.WriteLine);
            return new WmiFanDevice(writes, Console.WriteLine);
        }

        private static bool IsConfirmed(string[] args) { return args.Contains("--execute") && args.Contains("--confirm-hardware"); }

        public const string LogoPlan = "开机画面页已接入无界面的 x86 后台执行器，直接调用已校验的原厂 AIToolAPI 接口。保存和恢复直接修改 EFI 图片及固件设置，完成文件校验和状态回读。写入阶段失败时尝试关闭自定义画面并返回错误。图片按固件尺寸准备为 BMP。编译与模拟测试通过，实机写入和重启显示尚未验收。另提供固件参数读取与离线 BMP 准备。";
        public const string FanPlan = "默认后端为 WMI FullSpeed。本机管理员权限下5秒启停已通过，启动回读True、停止回读False，独立追加查询也为False。旧式 EnergyDrv 验证失败，仅显式加 --energy 时作为诊断选项，记录见 docs/fan-test-result.md。所有后端限定82TF与已核实驱动。正常取消会请求停止并回读，强制终止或内核阻塞可能中断清理。人工停止命令应选与启动相同的后端。";
        private const string Help = @"LiteLX Windows 轻量工具
无参数启动 GUI。LiteLX.Cli.exe 用于终端。
status                                      元数据检查
logo plan | status                          机制说明 / 固件只读查询
logo prepare <PNG/BMP> <输出目录> <宽> <高>  离线准备 BMP
menu list [过滤词]                         注册表只读枚举，输出JSON
menu disable <ID> [--apply]                 默认显示计划
menu enable <ID> [--apply]                  清除所选菜单的禁用状态
fan plan                                    显示测试方案
fan status --confirm-hardware-query          显式硬件查询
fan run <1到18秒>                           默认显示计划
fan stop                                    默认显示人工停止说明
风扇默认 WMI FullSpeed，--energy 显式选择旧式 EnergyDrv 诊断后端。
module list                                 核对子模块文件和验证状态
风扇启停需同时传 --execute --confirm-hardware。
固件写入、卸载后可用性和真实菜单变更仍待现场验证。";
    }
}
