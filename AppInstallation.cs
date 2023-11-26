using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibGen
{ 
    internal class AppInstallation
    {
        public string DisplayName { get; set; }
        public string DisplayVersion { get; set; }
        public string Publisher { get; set; }
        public int? EstimatedSize { get; set; }
        public DateTime? InstallDate { get; set; }
        public string InstallLocation { get; set; }
        public bool? WindowsInstaller { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"DisplayName: {DisplayName}");
            if (DisplayVersion != null)
                stringBuilder.AppendLine($"DisplayVersion: {DisplayVersion}");
            if (Publisher != null)
                stringBuilder.AppendLine($"Publisher: {Publisher}");
            if (EstimatedSize != null)
                stringBuilder.AppendLine($"EstimatedSize: {EstimatedSize}");
            if (InstallDate != null)
                stringBuilder.AppendLine($"InstallDate: {InstallDate}");
            if (InstallLocation != null)
                stringBuilder.AppendLine($"InstallLocation: {InstallLocation}");
            if (WindowsInstaller != null)
                stringBuilder.AppendLine($"WindowsInstaller: {WindowsInstaller}");
            return stringBuilder.ToString();
        }
    }
}
