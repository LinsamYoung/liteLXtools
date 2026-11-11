using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using LiteLX;

internal static class LogoSelfTest
{
    private static int assertions;

    private sealed class Firmware : ILogoFirmware
    {
        public LogoFirmwareState State = new LogoFirmwareState {
            Enabled = 1, Formats = 16, Width = 320, Height = 180
        };
        public readonly List<string> Calls = new List<string>();
        public string Failure;
        public bool DisableAlwaysFails;
        private bool failed;

        public LogoFirmwareState Read()
        {
            Calls.Add("read");
            var copy = Clone(State);
            if (Calls.Count > 1 && Failure == "readback" && !failed) { copy.Enabled = (byte)(State.Enabled == 0 ? 1 : 0); failed = true; }
            return copy;
        }

        public void SetEnabled(byte enabled)
        {
            Calls.Add("enable" + enabled);
            State.Enabled = enabled;
            if (enabled == 0 && DisableAlwaysFails) throw new IOException("模拟关闭失败");
            Fail(enabled == 0 ? "disable" : "enable");
        }

        public void SetCheck(string image)
        {
            Calls.Add("check");
            Check(File.Exists(image), "校验前已写入目标图片");
            Fail("check");
        }

        private void Fail(string stage)
        {
            if (stage == Failure && !failed) { failed = true; throw new IOException("模拟部分完成后失败 " + stage); }
        }

        private static LogoFirmwareState Clone(LogoFirmwareState state)
        {
            return new LogoFirmwareState { Enabled = state.Enabled, Formats = state.Formats, Width = state.Width, Height = state.Height };
        }
    }

    private sealed class Scenario : IDisposable
    {
        public readonly string Root = Path.Combine(Path.GetTempPath(), "LiteLX-LogoTest-" + Guid.NewGuid().ToString("N"));
        public string Logo { get { return Path.Combine(Root, "Logo"); } }
        public string Image { get { return Path.Combine(Root, "input.bmp"); } }
        public string Operation { get { return Path.Combine(Root, "operation"); } }
        public readonly Firmware Firmware = new Firmware();

        public Scenario()
        {
            Directory.CreateDirectory(Logo);
            Directory.CreateDirectory(Operation);
            File.WriteAllText(Path.Combine(Logo, "old.bmp"), "original image");
            using (var bitmap = new Bitmap(80, 60)) bitmap.Save(Image, ImageFormat.Bmp);
        }

        public string Run(bool restore) { return LogoInstallation.Apply(Firmware, Logo, restore ? null : Image, Operation); }
        public void Dispose()
        {
            // 测试仅清理本次生成、且完整路径位于独立临时目录内的文件。
            string prefix = Path.GetFullPath(Root) + Path.DirectorySeparatorChar;
            foreach (string file in Directory.GetFiles(Root, "*", SearchOption.AllDirectories))
            {
                if (!Path.GetFullPath(file).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new IOException("测试清理路径越界");
                File.Delete(file);
            }
            foreach (string directory in Directory.GetDirectories(Root, "*", SearchOption.AllDirectories).OrderByDescending(value => value.Length)) Directory.Delete(directory, false);
            Directory.Delete(Root, false);
        }
    }

    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception(message);
    }

    private static void CheckNoBackup(Scenario test)
    {
        Check(!Directory.Exists(Path.Combine(test.Operation, "backup")), "操作目录中不存在备份目录");
        Check(!File.Exists(Path.Combine(test.Operation, "state.json")), "操作目录中不存在固件备份记录");
    }

    private static void Fails(Action action)
    {
        bool failed = false;
        try { action(); } catch (Exception) { failed = true; }
        Check(failed, "预期的失败必须返回错误");
    }

    private static int Main()
    {
        try
        {
            using (var test = new Scenario())
            {
                string preview = test.Run(false);
                Check(File.Exists(preview), "返回准备后的预览文件");
                Check(test.Firmware.State.Enabled == 1, "保存后启用自定义画面");
                CheckNoBackup(test);
                Check(Directory.GetFiles(test.Logo).Length == 1, "旧图片已替换");
                LogoInstallation.VerifyFile(preview, Directory.GetFiles(test.Logo)[0]);
                Check(String.Join(",", test.Firmware.Calls) == "read,enable0,check,enable1,read", "保存按停用、文件校验、启用、回读的顺序执行");
                using (var image = System.Drawing.Image.FromFile(preview)) Check(image.Width == 320 && image.Height == 180, "图片尺寸使用固件参数");
            }
            using (var test = new Scenario())
            {
                Check(test.Run(true) == null, "恢复默认不生成待保存图片");
                Check(test.Firmware.State.Enabled == 0, "恢复后关闭自定义画面");
                Check(Directory.GetFiles(test.Logo).Length == 0, "清理自定义图片");
                Check(!test.Firmware.Calls.Contains("check"), "恢复默认不改写图片校验信息");
                CheckNoBackup(test);
            }
            foreach (string stage in new[] { "disable", "check", "enable", "readback" })
                using (var test = new Scenario())
                {
                    test.Firmware.Failure = stage;
                    Fails(delegate { test.Run(false); });
                    Check(test.Firmware.State.Enabled == 0, "写入失败后关闭自定义画面");
                    CheckNoBackup(test);
                    if (stage != "disable")
                    {
                        Check(!File.Exists(Path.Combine(test.Logo, "old.bmp")), "失败后按直接写入流程处理原图片");
                        Check(File.Exists(Path.Combine(test.Logo, "mylogo_320x180.bmp")), "保留已写入的新图片供重试");
                    }
                }
            foreach (string stage in new[] { "disable", "readback" })
                using (var test = new Scenario())
                {
                    test.Firmware.Failure = stage;
                    Fails(delegate { test.Run(true); });
                    Check(test.Firmware.State.Enabled == 0, "恢复默认失败时保持关闭状态");
                    CheckNoBackup(test);
                }
            using (var test = new Scenario())
            {
                test.Firmware.DisableAlwaysFails = true;
                AggregateException failure = null;
                try { test.Run(false); } catch (AggregateException error) { failure = error; }
                Check(failure != null && failure.InnerExceptions.Count == 2, "保留原始写入失败与关闭失败");
                CheckNoBackup(test);
            }
            using (var test = new Scenario())
            {
                test.Firmware.State.Formats = 0;
                Fails(delegate { test.Run(false); });
                Check(test.Firmware.Calls.Count == 1, "不支持 BMP 时停止写入");
            }
            using (var test = new Scenario())
            {
                test.Firmware.State.Height = -1;
                Fails(delegate { test.Run(false); });
                Check(test.Firmware.Calls.Count == 1, "固件参数无效时停止写入");
            }
            using (var test = new Scenario())
            {
                Directory.CreateDirectory(Path.Combine(test.Logo, "unexpected"));
                Fails(delegate { test.Run(false); });
                Check(test.Firmware.Calls.Count == 1, "目录含有额外子目录时停止写入");
            }
            using (var test = new Scenario())
            {
                File.WriteAllText(test.Image, "invalid bitmap");
                Fails(delegate { test.Run(false); });
                Check(test.Firmware.Calls.Count == 1, "无效图片不触发固件写入");
            }
            using (var test = new Scenario())
            {
                File.Delete(Path.Combine(test.Logo, "old.bmp")); Directory.Delete(test.Logo, false);
                test.Firmware.State.Enabled = 0; test.Firmware.Failure = "check";
                Fails(delegate { test.Run(false); });
                Check(Directory.Exists(test.Logo), "失败后保留直接写入创建的图片目录");
                Check(test.Firmware.State.Enabled == 0, "新建目录的写入失败后关闭自定义画面");
                CheckNoBackup(test);
            }
            Console.WriteLine("PASS " + assertions + " logo assertions. Simulated firmware and temporary files only.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
