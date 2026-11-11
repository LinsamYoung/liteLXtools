using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Web.Script.Serialization;

namespace LiteLX
{
    public static class OriginalModules
    {
        private static string Root(string name)
        {
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "modules", name));
        }

        public static object Inventory()
        {
            return new {
                Logo = Inspect("LenovoLogo", "开机画面后台接口，固件保存尚未实机验收。")
            };
        }

        public static string Inspect(string name, string state)
        {
            string root = Root(name);
            string manifest = Path.Combine(root, "manifest.json");
            if (!File.Exists(manifest)) return state + " 文件未提取，可运行 tools 中对应提取脚本。";
            var entries = new JavaScriptSerializer().Deserialize<ModuleFile[]>(File.ReadAllText(manifest));
            long bytes = 0;
            foreach (var entry in entries)
            {
                string path = Path.GetFullPath(Path.Combine(root, entry.RelativePath));
                if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("子模块清单包含越界路径。");
                using (var stream = File.OpenRead(path))
                using (var sha = SHA256.Create())
                {
                    string digest = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
                    if (!digest.Equals(entry.SHA256, StringComparison.OrdinalIgnoreCase)) throw new IOException("子模块文件校验失败 " + path);
                }
                bytes += new FileInfo(path).Length;
            }
            return state + " 已校验 " + entries.Length + " 个原文件，共 " + bytes + " 字节。目录 " + root;
        }

        public static string ApplyLogo(string image)
        {
            Inspect("LenovoLogo", "操作前校验");
            string root = Root("LenovoLogo");
            string executable = Path.Combine(root, "LogoOperationHost.exe");
            if (!File.Exists(executable)) throw new FileNotFoundException("请先运行 build.ps1 构建开机画面执行器。", executable);
            string operationRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "LiteLX", "LogoOperations"));
            string operation = Path.Combine(operationRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(operation);
            try
            {
                if (image != null)
                {
                    if (new FileInfo(image).Length > 52L * 1024 * 1024) throw new IOException("图片超过 52 MiB。");
                    string copy = Path.Combine(operation, "input.bmp");
                    File.Copy(image, copy, false);
                    LogoInstallation.VerifyFile(image, copy);
                }
                LogoInstallation.WriteJson(Path.Combine(operation, "request.json"), new { Action = image == null ? "restore" : "save" });
                var start = new ProcessStartInfo(executable, "--apply \"" + operation + "\"") {
                    WorkingDirectory = root, UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden
                };
                if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) start.Verb = "runas";
                try
                {
                    using (var process = Process.Start(start))
                    {
                        process.WaitForExit();
                        string resultPath = Path.Combine(operation, "result.json");
                        if (!File.Exists(resultPath)) throw new IOException("后台执行器未返回结果。");
                        var result = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(resultPath));
                        string message = Convert.ToString(result["Message"]);
                        if (process.ExitCode != 0 || !Convert.ToBoolean(result["Success"])) throw new IOException(message);
                        // 同步主窗口使用的预览缓存，缓存失败时仍保留真实保存结果。
                        try
                        {
                            string cached = Path.Combine(Path.GetTempPath(), "mylogo_2560x1600.bmp");
                            if (image == null) File.Delete(cached);
                            else File.Copy(Convert.ToString(result["Preview"]), cached, true);
                        }
                        catch (IOException error) { message += "\n预览缓存更新失败 " + error.Message; }
                        catch (UnauthorizedAccessException error) { message += "\n预览缓存更新失败 " + error.Message; }
                        return message;
                    }
                }
                catch (Win32Exception error)
                {
                    if (error.NativeErrorCode == 1223) throw new OperationCanceledException("已取消管理员授权。", error);
                    throw;
                }
            }
            finally
            {
                // 清理本次生成的临时工作目录，删除前核对其绝对路径和父目录。
                string fullPath = Path.GetFullPath(operation);
                if (!String.Equals(Path.GetDirectoryName(fullPath), operationRoot, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("临时操作目录越界。");
                try { Directory.Delete(fullPath, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        public sealed class ModuleFile
        {
            public string Source { get; set; }
            public string RelativePath { get; set; }
            public string SHA256 { get; set; }
            public long Bytes { get; set; }
        }
    }
}
