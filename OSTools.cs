using System;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace AngelDB
{
    public static class OSTools
    {
        public static bool IsLinux()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
               return true;
            } 

            return false;

        }

        public static bool IsOsx()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
               return true;
            } 

            return false;

        }

        public static bool IsWindows()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
               return true;
            } 

            return false;

        }


        public static string OSName() 
        {

            string architecture = "";
            string os_name = "";  

            if( System.Environment.Is64BitOperatingSystem ) 
            {
                architecture = "-x64";
            } 
            else 
            {
                architecture = "-x86";
            }

            if (IsLinux())
            {
               os_name = "linux";
            } 

            if (IsWindows())
            {
               os_name = "win";
            } 

            if ( IsOsx())
            {
               os_name = "osx";
            } 

            return os_name + architecture;
        }

        public static bool IsAbsolutePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Una ruta que comienza con "\\" o "driveLetter:\", como "C:\", se considera absoluta.
            if (path.StartsWith("\\\\") ||
                (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '\\'))
            {
                return true;
            }

            return false;
        }

        public static string MapNetworkDrive(string driveLetter, string sharedPath, string username, string password)
        {
            string arguments = $"{driveLetter} {sharedPath} /user:{username} {password}";
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = "use " + arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(error))
            {
                return "Error: mapping network drive: " + error;
            }

            return output;
        }
    }

}
