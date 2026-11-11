using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using LiteLX;

internal static class EmbeddedLogoSelfTest
{
    private static int Main()
    {
        string input = Path.GetTempFileName();
        string operation = null;
        int checks = 0;
        string cache = Path.Combine(Path.GetTempPath(), "mylogo_2560x1600.bmp");
        byte[] previousCache = File.Exists(cache) ? File.ReadAllBytes(cache) : null;
        try
        {
            File.WriteAllText(input, "模拟输入");
            foreach (string mode in new[] { "success", "failure", "exit", "missing", "malformed", "cancel" })
            {
                bool failed = false;
                try
                {
                    string message = OriginalModules.ApplyLogo(input, delegate(ProcessStartInfo start) {
                        operation = start.WorkingDirectory;
                        if (!File.Exists(start.FileName) || !File.Exists(Path.Combine(operation, "AIToolAPI.dll")))
                            throw new Exception("依赖释放失败");
                        if (!File.Exists(Path.Combine(operation, "request.json"))) throw new Exception("请求缺失");
                        checks++;
                        if (mode == "success")
                        {
                            // 空参数只验证真实 x86 执行器能启动，不进入硬件操作。
                            using (var helper = Process.Start(new ProcessStartInfo(start.FileName) {
                                WorkingDirectory = operation, UseShellExecute = false, CreateNoWindow = true
                            }))
                            {
                                if (!helper.WaitForExit(10000)) { helper.Kill(); throw new Exception("执行器启动超时"); }
                                if (helper.ExitCode != 2) throw new Exception("执行器参数检查异常");
                            }
                            checks++;
                        }
                        if (mode == "cancel") throw new Win32Exception(1223);
                        if (mode == "missing") return 0;
                        string result = Path.Combine(operation, "result.json");
                        if (mode == "malformed") File.WriteAllText(result, "{");
                        else LogoInstallation.WriteJson(result, new {
                            Success = mode != "failure", Message = "模拟执行结果",
                            Preview = Path.Combine(operation, "input.bmp")
                        });
                        return mode == "exit" ? 1 : 0;
                    });
                    if (message != "模拟执行结果") throw new Exception("执行结果未读取");
                    checks++;
                }
                catch (Exception error)
                {
                    if (mode == "success") throw;
                    if (!(error is IOException || error is ArgumentException || error is OperationCanceledException)) throw;
                    failed = true;
                }
                if (failed != (mode != "success")) throw new Exception("执行结果判定错误 " + mode);
                if (Directory.Exists(operation)) throw new Exception("本次操作文件未清理 " + mode);
                checks++;
            }
            // 在释放依赖后模拟输入文件缺失，检查部分失败的清理路径。
            var before = Directory.GetDirectories(Path.GetTempPath(), "LiteLX-Logo-*");
            try { OriginalModules.ApplyLogo(input + ".missing", delegate { throw new Exception("不应启动执行器"); }); }
            catch (FileNotFoundException) { checks++; }
            if (Directory.GetDirectories(Path.GetTempPath(), "LiteLX-Logo-*").Length != before.Length)
                throw new Exception("输入失败后临时目录残留");
            checks++;
            Console.WriteLine("PASS " + checks + " embedded logo assertions. Simulated executor only.");
            return 0;
        }
        finally
        {
            File.Delete(input);
            if (previousCache == null) File.Delete(cache);
            else File.WriteAllBytes(cache, previousCache);
        }
    }
}
