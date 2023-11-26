using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibGen
{
    internal static class InstalledApps
    {
        public static List<AppInstallation>? GetInstalledAppInfos()
        {
#if WINDOWS
            List<AppInstallation> installedApps = new List<AppInstallation>();
            var registryRootKeys = new RegistryKey[4];
            registryRootKeys[0] = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            registryRootKeys[1] = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            registryRootKeys[2] = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            registryRootKeys[3] = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);

            foreach (var registryRootKey in registryRootKeys)
            {
                using (registryRootKey)
                {
                    using (RegistryKey uninstallKey = registryRootKey.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall")!)
                    {
                        if (uninstallKey == null)
                            return null;
                        string[]? subKeys = uninstallKey?.GetSubKeyNames();
                        if (subKeys == null)
                            return null;
                        foreach (string subKey in subKeys)
                        {
                            using (RegistryKey uninstallEntry = uninstallKey?.OpenSubKey(subKey)!)
                            {
                                if (uninstallEntry == null)
                                    return null;
                                string? displayName = (string?)uninstallEntry.GetValue("DisplayName")!;

                                int? systemComponent = (int?)uninstallEntry.GetValue("SystemComponent");
                                string? parentKeyName = (string?)uninstallEntry.GetValue("ParentKeyName");
                                string? parentDisplayName = (string?)uninstallEntry.GetValue("ParentDisplayName");
                                string? releaseType = (string?)uninstallEntry.GetValue("ReleaseType");
                                string? bundleVersion = (string?)uninstallEntry.GetValue("BundleVersion");
                                string? bundleProviderKey = (string?)uninstallEntry.GetValue("BundleProviderKey");
                                if (displayName == null
                                    || bundleVersion != null
                                    || bundleProviderKey != null
                                    || parentKeyName != null
                                    || parentDisplayName != null
                                    || releaseType != null
                                    || systemComponent != null)
                                    continue;
                                string? displayVersion = (string?)uninstallEntry.GetValue("DisplayVersion");
                                string? publisher = (string?)uninstallEntry.GetValue("Publisher");
                                int? estimatedSize = (int?)uninstallEntry.GetValue("EstimatedSize");
                                string? installDate = (string?)uninstallEntry.GetValue("InstallDate");
                                string? installLocation = (string?)uninstallEntry.GetValue("InstallLocation");
                                int? windowsInstaller = (int?)uninstallEntry.GetValue("WindowsInstaller");
                                AppInstallation installedAppInfo = new AppInstallation();
                                installedAppInfo.DisplayName = displayName;
                                installedAppInfo.DisplayVersion = displayVersion!;
                                installedAppInfo.Publisher = publisher!;
                                installedAppInfo.EstimatedSize = estimatedSize;
                                DateTime? dateTime = null;
                                if (installDate != null)
                                {
                                    ReadOnlySpan<char> dateSpan = installDate;
                                    if (dateSpan.Length == 8)
                                    {
                                        int year = int.Parse(dateSpan.Slice(0, 4));
                                        int month = int.Parse(dateSpan.Slice(4, 2));
                                        int day = int.Parse(dateSpan.Slice(6, 2));
                                        dateTime = new DateTime(year, month, day);
                                    }
                                    else
                                    {
                                        // Çar Haz 21 23:16:44 2023
                                        bool result = DateTime.TryParse(installDate, DateTimeFormatInfo.CurrentInfo, out DateTime tempDateTime);
                                        if (result)
                                            dateTime = tempDateTime;
                                    }
                                }
                                if (dateTime == null || installDate == null)
                                {
                                    string? displayIcon = (string?)uninstallEntry.GetValue("DisplayIcon");
                                    if (File.Exists(displayIcon))
                                        dateTime = File.GetCreationTime(displayIcon);
                                }
                                installedAppInfo.InstallDate = dateTime;
                                installedAppInfo.InstallLocation = installLocation!;
                                installedAppInfo.WindowsInstaller = windowsInstaller == null ? null : windowsInstaller == 1;
                                installedApps.Add(installedAppInfo);
                                uninstallEntry.Close();
                            }
                        }
                        uninstallKey.Close();
                    }
                    registryRootKey.Close();
                }
            }
            return installedApps;
#else
            return null;
#endif
        }
    }
}
