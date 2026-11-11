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
        private static byte[] Resource(string name)
        {
            using (var stream = typeof(OriginalModules).Assembly.GetManifestResourceStream("LiteLX.Logo." + name))
            {
                if (stream == null) throw new FileNotFoundException("缺少内嵌开机画面模块，请重新构建。", name);
                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    return buffer.ToArray();
                }
            }
        }

        internal static List<FileStream> Extract(string directory)
        {
            var locks = new List<FileStream>();
            try
            {
                foreach (string name in new[] { "LogoOperationHost.exe", "AIToolAPI.dll" })
                {
                    byte[] data = Resource(name);
                    string path = Path.Combine(directory, name);
                    using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        file.Write(data, 0, data.Length);
                        file.Flush(true);
                    }
                    var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    locks.Add(held);
                    using (var sha = SHA256.Create())
                        if (!Convert.ToBase64String(sha.ComputeHash(held)).Equals(Convert.ToBase64String(sha.ComputeHash(data))))
                            throw new IOException("内嵌模块释放校验失败 " + name);
                }
                return locks;
            }
            catch
            {
                foreach (var held in locks) held.Dispose();
                throw;
            }
        }

        public static object Inventory()
        {
            return new {
                Logo = Inspect("LenovoLogo", "开机画面后台接口，固件保存尚未实机验收。")
            };
        }

        public static string Inspect(string name, string state)
        {
            if (name != "LenovoLogo") throw new ArgumentException("未知模块。", "name");
            var entries = new JavaScriptSerializer().Deserialize<ModuleFile[]>(System.Text.Encoding.UTF8.GetString(Resource("manifest.json")).TrimStart('\uFEFF'));
            long bytes = 0;
            foreach (var entry in entries)
            {
                if (entry.RelativePath != "AIToolAPI.dll") throw new IOException("子模块清单包含未知文件。");
                byte[] data = Resource(entry.RelativePath);
                using (var sha = SHA256.Create())
                {
                    string digest = BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
                    if (!digest.Equals(entry.SHA256, StringComparison.OrdinalIgnoreCase)) throw new IOException("内嵌原厂模块校验失败。");
                }
                bytes += data.Length;
            }
            return state + " 已校验 " + entries.Length + " 个原文件，共 " + bytes + " 字节，依赖已内嵌，操作时按需释放。";
        }

        public static string ApplyLogo(string image)
        {
            return ApplyLogo(image, delegate(ProcessStartInfo start) {
                using (var process = Process.Start(start))
                {
                    process.WaitForExit();
                    return process.ExitCode;
                }
            });
        }

        internal static string ApplyLogo(string image, Func<ProcessStartInfo, int> execute)
        {
            Inspect("LenovoLogo", "操作前校验");
            string operationRoot = Path.GetTempPath();
            string operation = Path.Combine(operationRoot, "LiteLX-Logo-" + Guid.NewGuid().ToString("N"));
            // 创建独立受限目录，避免不同操作互相覆盖文件。
            var security = new System.Security.AccessControl.DirectorySecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                WindowsIdentity.GetCurrent().User, System.Security.AccessControl.FileSystemRights.FullControl,
                System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
            Directory.CreateDirectory(operation, security);
            var moduleLocks = new List<FileStream>();
            try
            {
                moduleLocks = Extract(operation);
                string executable = Path.Combine(operation, "LogoOperationHost.exe");
                if (image != null)
                {
                    if (new FileInfo(image).Length > 52L * 1024 * 1024) throw new IOException("图片超过 52 MiB。");
                    string copy = Path.Combine(operation, "input.bmp");
                    File.Copy(image, copy, false);
                    LogoInstallation.VerifyFile(image, copy);
                }
                LogoInstallation.WriteJson(Path.Combine(operation, "request.json"), new { Action = image == null ? "restore" : "save" });
                var start = new ProcessStartInfo(executable, "--apply \"" + operation + "\"") {
                    WorkingDirectory = operation, UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden
                };
                if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) start.Verb = "runas";
                try
                {
                    {
                        int exitCode = execute(start);
                        string resultPath = Path.Combine(operation, "result.json");
                        if (!File.Exists(resultPath)) throw new IOException("后台执行器未返回结果。");
                        var result = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(resultPath));
                        string message = Convert.ToString(result["Message"]);
                        if (exitCode != 0 || !Convert.ToBoolean(result["Success"])) throw new IOException(message);
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
                foreach (var held in moduleLocks) held.Dispose();
                // 清理本次生成的临时工作目录，删除前核对其绝对路径和父目录。
                string fullPath = Path.GetFullPath(operation);
                if (!String.Equals(Path.GetDirectoryName(fullPath), operationRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                    throw new IOException("临时操作目录越界。");
                try { Directory.Delete(fullPath, true); }
                catch (IOException error) { throw new IOException("本次操作目录清理失败 " + fullPath, error); }
                catch (UnauthorizedAccessException error) { throw new IOException("本次操作目录清理失败 " + fullPath, error); }
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
