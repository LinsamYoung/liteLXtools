using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace LiteLX
{
    internal sealed class LogoFirmwareState
    {
        public byte Enabled { get; set; }
        public uint Formats { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    internal interface ILogoFirmware
    {
        LogoFirmwareState Read();
        void SetEnabled(byte enabled);
        void SetCheck(string image);
    }

    internal static class LogoInstallation
    {
        public static string Apply(ILogoFirmware firmware, string logoDirectory, string image, string operationDirectory)
        {
            bool restore = image == null;
            var parameters = firmware.Read();
            if (parameters.Enabled > 1 || parameters.Width < 1 || parameters.Height < 1 || parameters.Width > 8192 || parameters.Height > 8192)
                throw new NotSupportedException("固件返回的开机画面参数无效。");
            string prepared = null;
            if (!restore)
            {
                if ((parameters.Formats & 16) == 0) throw new NotSupportedException("当前固件未声明支持 BMP 图片。");
                if (new FileInfo(image).Length > 52L * 1024 * 1024) throw new IOException("图片超过 52 MiB。");
                using (var input = Image.FromFile(image))
                    if (input.RawFormat.Guid != ImageFormat.Bmp.Guid) throw new InvalidDataException("请选择 BMP 图片。");
                prepared = LogoTools.PrepareImage(image, Path.Combine(operationDirectory, "prepared"), parameters.Width, parameters.Height);
                if (new FileInfo(prepared).Length > 52L * 1024 * 1024) throw new IOException("准备后的图片超过 52 MiB。");
            }

            LogoFiles(logoDirectory);
            try
            {
                // 图片和校验准备完成后才启用自定义画面。
                firmware.SetEnabled(0);
                Directory.CreateDirectory(logoDirectory);
                ClearFiles(logoDirectory);
                if (!restore)
                {
                    string target = Path.Combine(logoDirectory, Path.GetFileName(prepared));
                    File.Copy(prepared, target, false);
                    VerifyFile(prepared, target);
                    firmware.SetCheck(target);
                    firmware.SetEnabled(1);
                }
                var current = firmware.Read();
                if (current.Enabled != (restore ? 0 : 1) || current.Width != parameters.Width || current.Height != parameters.Height || current.Formats != parameters.Formats)
                    throw new IOException("固件回读与目标状态不符。");
                return prepared;
            }
            catch (Exception failure)
            {
                // 写入阶段出错时停用自定义画面，并将原始错误返回主窗口。
                try { firmware.SetEnabled(0); }
                catch (Exception disableError)
                {
                    throw new AggregateException("开机画面操作未完成，关闭自定义画面时也发生错误。", failure, disableError);
                }
                throw new IOException("开机画面操作未完成，自定义画面已关闭。", failure);
            }
        }

        private static string[] LogoFiles(string directory)
        {
            // 检查所有已有路径段，防止目录联接将文件操作引向其他位置。
            for (var current = new DirectoryInfo(directory); current != null; current = current.Parent)
                if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("开机画面路径包含重解析点，已停止操作。");
            if (!Directory.Exists(directory)) return new string[0];
            if (Directory.GetDirectories(directory).Length > 0) throw new IOException("开机画面目录含有子目录，请先核对内容。");
            string[] files = Directory.GetFiles(directory);
            if (files.Length > 128 || files.Sum(path => new FileInfo(path).Length) > 64L * 1024 * 1024)
                throw new IOException("开机画面目录内容超出预期范围。");
            if (files.Any(path => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0))
                throw new IOException("开机画面目录含有链接文件，已停止操作。");
            return files;
        }

        private static void ClearFiles(string directory)
        {
            foreach (string file in LogoFiles(directory)) File.Delete(file);
        }

        internal static void VerifyFile(string expected, string actual)
        {
            using (var sha = SHA256.Create())
            using (var source = File.OpenRead(expected))
            using (var destination = File.OpenRead(actual))
                if (!sha.ComputeHash(source).SequenceEqual(sha.ComputeHash(destination))) throw new IOException("图片文件回读校验失败。");
        }

        internal static void WriteJson(string path, object value)
        {
            string temporary = path + ".tmp";
            byte[] data = Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(value));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(data, 0, data.Length);
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}
