using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace LibGen
{
    internal class Program
    { 
        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_EXPORT_DIRECTORY
        {
            public uint Characteristics;
            public uint TimeDateStamp;
            public ushort MajorVersion;
            public ushort MinorVersion;
            public uint Name;
            public uint Base;
            public uint NumberOfFunctions;
            public uint NumberOfNames;
            public uint AddressOfFunctions;
            public uint AddressOfNames;
            public uint AddressOfOrdinals;
        }
        private static PEReader? s_peReader;
        private static string? s_vsInstallationDirectory;
        private static string? s_microsoftLibExePath = null;
        private static Regex s_vsDisplayNameRegex = new Regex("^Visual Studio (Community|Professional|Enterprise) [0-9]{4}$");
        private static int ConvertRvaToFileOffset(PEHeaders? peHeaders, int rva)
        {
            if (peHeaders == null)
                return 0;
            SectionHeader sectionHeader = peHeaders.SectionHeaders.First(
                (sh) => sh.VirtualAddress <= rva && rva <= sh.VirtualAddress + sh.VirtualSize);
            
            return sectionHeader.PointerToRawData + rva - sectionHeader.VirtualAddress;
        }
        public static string VsInstallationDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(s_vsInstallationDirectory))
                {
                    AppInstallation? visualStudioInstallation = InstalledApps
                        .GetInstalledAppInfos()?
                        .Find(iai => s_vsDisplayNameRegex.IsMatch(iai.DisplayName));
                    s_vsInstallationDirectory = visualStudioInstallation?.InstallLocation;
                }
                return s_vsInstallationDirectory!;
            }
        }

        private static string? ChooseMicrosoftLibExe(PEHeaders? peHeaders, string preferredExeBitness)
        {
            string osBitness = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            string hostDirectoryName = string.Join(string.Empty, "Host", osBitness);
            string vsInstallationDirectory = VsInstallationDirectory;

            string msvcDirectory = Path.Combine(vsInstallationDirectory, "VC\\Tools\\MSVC");
            DirectoryInfo directoryInfo = new DirectoryInfo(msvcDirectory);

            DirectoryInfo[] versions = directoryInfo.GetDirectories();
            if (versions.Length == 0)
                return null;
            Version? preVersion = new Version(versions[0].Name);
            string? latestVersionName = null;
            foreach (var versionName in versions)
            {
                Version version = new Version(versionName.Name);
                if(version >= preVersion)
                    latestVersionName = versionName.Name;
                preVersion = version;
            }
            string binDirectory = Path.Combine(msvcDirectory, latestVersionName!, "bin");

            s_microsoftLibExePath = Path.Combine(binDirectory, hostDirectoryName, preferredExeBitness, "lib.exe");

            return s_microsoftLibExePath;
        } 
        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("You didn't specified any file.");
                return;
            }
            StringBuilder moduleDefinition = new StringBuilder();
            string dllFilePath = args[0];
            string defFileName = $"{Path.GetFileNameWithoutExtension(dllFilePath)}.def";
            using (var fs = new FileStream(dllFilePath, FileMode.Open, FileAccess.Read))
            {
                s_peReader = new PEReader(fs);
                PEHeaders? peHeaders = s_peReader.PEHeaders;
                if (peHeaders.PEHeader != null)
                {
                    moduleDefinition.AppendLine($"LIBRARY {Path.GetFileNameWithoutExtension(args[0])}");
                    moduleDefinition.AppendLine("EXPORTS");
                    PEHeader peHeader = peHeaders.PEHeader;
                    DirectoryEntry exportEntry = peHeader.ExportTableDirectory;
                    PEMemoryBlock peMemoryBlock = s_peReader.GetSectionData(exportEntry.RelativeVirtualAddress);
                    unsafe
                    {
                        byte* sectionBase = peMemoryBlock.Pointer;
                        uint sectionBaseRVA = (uint)exportEntry.RelativeVirtualAddress;
                        IMAGE_EXPORT_DIRECTORY* exportTableDesc = (IMAGE_EXPORT_DIRECTORY*)sectionBase;
                        uint numberOfNames = exportTableDesc->NumberOfNames;
                        uint addressOfNames = exportTableDesc->AddressOfNames - sectionBaseRVA;
                        uint addressOfOrdinals = exportTableDesc->AddressOfOrdinals - sectionBaseRVA;
                        for (uint i = 0; i < numberOfNames; i++)
                        {
                            uint functionNameRVA = *(uint*)(sectionBase + addressOfNames + i * sizeof(uint)) - sectionBaseRVA;
                            ushort ordinal = *(ushort*)(sectionBase + addressOfOrdinals + i * sizeof(ushort));
                            ordinal += (ushort)exportTableDesc->Base;
                            string? functionName = Marshal.PtrToStringAnsi((nint)(sectionBase + functionNameRVA));
                            moduleDefinition.AppendLine($"{functionName} @{ordinal}");
                        }
                    }
                    string dllParentDirectory = Directory.GetParent(dllFilePath)!.FullName;
                    File.WriteAllText(
                        Path.Combine(   dllParentDirectory,
                                        defFileName),
                                    moduleDefinition.ToString());
                    Console.WriteLine("Module-definition file created...");
                    Console.WriteLine("Lib.exe running...");

                    string preferredExeBitness = peHeaders!.PEHeader!.Magic == PEMagic.PE32 ? "x86" : "x64";
                    string osBitness = Environment.Is64BitOperatingSystem ? "x64" : "x86";


                    string outputDirectory = Path.Combine(dllParentDirectory, "lib", preferredExeBitness);
                    if (!Directory.Exists(outputDirectory)) 
                        Directory.CreateDirectory(outputDirectory); 

                    Process libProcess = new Process();
                    ProcessStartInfo processStartInfo = new ProcessStartInfo();
                    processStartInfo.UseShellExecute = false;
                    processStartInfo.CreateNoWindow = true;
                    processStartInfo.WorkingDirectory = outputDirectory;
                    processStartInfo.FileName = ChooseMicrosoftLibExe(s_peReader.PEHeaders, preferredExeBitness);
                    processStartInfo.Arguments = $"/MACHINE:{preferredExeBitness} /DEF:..\\..\\{defFileName}";
                    processStartInfo.RedirectStandardOutput = true;
                    libProcess.StartInfo = processStartInfo;

                    libProcess.Start(); 
                    libProcess.WaitForExit();
                    Console.WriteLine(libProcess.StandardOutput.ReadToEnd());
                    Console.WriteLine("Exiting...");
                }
            }
        }
    }
}
