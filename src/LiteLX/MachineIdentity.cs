using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace LiteLX
{
    internal static class MachineIdentity
    {
        public static string ReadName()
        {
            string model = "", family = "", sku = "", version = "";
            try
            {
                // Windows 在此提供 SMBIOS 的本机副本，优先读取以避免启动时等待 WMI。
                using (var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var bios = root.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS", false))
                {
                    if (bios != null)
                    {
                        model = Clean(Convert.ToString(bios.GetValue("SystemProductName")));
                        family = Clean(Convert.ToString(bios.GetValue("SystemFamily")));
                        sku = Convert.ToString(bios.GetValue("SystemSKU"));
                    }
                }
            }
            catch (SecurityException) { }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            string name = ResolveName(model, family, sku, version);
            if (!String.IsNullOrEmpty(name) && !String.Equals(name, model, StringComparison.OrdinalIgnoreCase)) return name;
            try
            {
                using (var search = new ManagementObjectSearcher("SELECT Name, Version FROM Win32_ComputerSystemProduct"))
                {
                    search.Options.Timeout = TimeSpan.FromSeconds(2);
                    using (var items = search.Get())
                        foreach (ManagementObject item in items)
                            using (item)
                            {
                                if (String.IsNullOrEmpty(model)) model = Clean(Convert.ToString(item["Name"]));
                                version = Convert.ToString(item["Version"]);
                                name = ResolveName(model, family, sku, version);
                                break;
                            }
                }
            }
            catch (ManagementException) { }
            catch (COMException) { }
            catch (UnauthorizedAccessException) { }
            return String.IsNullOrEmpty(name) ? "本机" : name;
        }

        internal static string ResolveName(string model, string family, string sku, string version)
        {
            model = Clean(model);
            string skuFamily = "";
            int marker = (sku ?? "").IndexOf("_FM_", StringComparison.OrdinalIgnoreCase);
            // 仅解析联想明确带有系列标记的 SKU，避免把其他厂商的 SKU 当成型号。
            if ((sku ?? "").StartsWith("LENOVO_MT_", StringComparison.OrdinalIgnoreCase) && marker >= 0)
                skuFamily = sku.Substring(marker + 4);
            string name = new[] { family, version, skuFamily }.Select(Clean)
                .FirstOrDefault(value => value.Length > 0 && !String.Equals(value, model, StringComparison.OrdinalIgnoreCase));
            return name ?? model;
        }

        private static string Clean(string value)
        {
            value = Regex.Replace(value ?? "", @"\s+", " ").Trim();
            var placeholders = new[] { "To Be Filled By O.E.M.", "Default string", "System Product Name", "System Family", "System Version", "Unknown", "Not Applicable", "Not Specified", "None" };
            return placeholders.Contains(value, StringComparer.OrdinalIgnoreCase) ? "" : value;
        }
    }
}
