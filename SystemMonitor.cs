// SystemMetrics.cs
using System.Collections.Generic;


// SystemMonitor.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;

namespace AngelDB 
{
    public class SystemMonitor
    {
        // Método principal para obtener todas las métricas y devolverlas como un string JSON.
        public async Task<string> GetSystemMetricsAsJsonAsync()
        {
            var (totalMemory, availableMemory) = GetMemoryInfo();

            var metrics = new SystemMetrics
            {
                CpuUsedByAngelSQLServerPercentage = await GetCpuUsageAsync(),
                TotalMemoryGB = ToGigabytes(totalMemory),
                AvailableMemoryGB = ToGigabytes(availableMemory),
                //Disks = GetDiskInfo(),
                NetworkUsage = GetNetworkUsage(),
                TimestampUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff") // Formato ISO 8601
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(metrics, options);
        }

        // Obtiene el uso de CPU promediado en 1 segundo.
        private async Task<double> GetCpuUsageAsync()
        {
            var startTime = Process.GetCurrentProcess().TotalProcessorTime;
            var startCpuTime = DateTime.UtcNow;
            await Task.Delay(500);
            var endTime = Process.GetCurrentProcess().TotalProcessorTime;
            var endCpuTime = DateTime.UtcNow;
            var cpuUsedMs = (endTime - startTime).TotalMilliseconds;
            var totalMsPassed = (endCpuTime - startCpuTime).TotalMilliseconds;
            return (float)(cpuUsedMs / (Environment.ProcessorCount * totalMsPassed) * 100);
        }

        // Obtiene la información de los discos. Es multiplataforma.
        private List<DiskInfo> GetDiskInfo()
        {
            var diskInfoList = new List<DiskInfo>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    diskInfoList.Add(new DiskInfo
                    {
                        Name = drive.Name,
                        DriveFormat = drive.DriveFormat,
                        TotalSpaceGB = ToGigabytes(drive.TotalSize),
                        AvailableFreeSpaceGB = ToGigabytes(drive.AvailableFreeSpace)
                    });
                }
            }
            return diskInfoList;
        }

        // Obtiene el uso de red (total de bytes enviados/recibidos). Es multiplataforma.
        private NetworkUsageInfo GetNetworkUsage()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return new NetworkUsageInfo();
            }

            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            long bytesSent = 0;
            long bytesReceived = 0;

            foreach (var iface in interfaces.Where(i => i.OperationalStatus == OperationalStatus.Up))
            {
                var stats = iface.GetIPStatistics();
                bytesSent += stats.BytesSent;
                bytesReceived += stats.BytesReceived;
            }

            return new NetworkUsageInfo { BytesSent = bytesSent, BytesReceived = bytesReceived };
        }

        // Lógica específica de la plataforma para obtener memoria total y disponible.
        private (long total, long available) GetMemoryInfo()
        {
            if (OperatingSystem.IsWindows())
            {
                return GetWindowsMemoryInfo();
            }
            if (OperatingSystem.IsLinux())
            {
                return GetLinuxMemoryInfo();
            }
            if (OperatingSystem.IsMacOS())
            {
                return GetMacOsMemoryInfo();
            }
            return (0, 0);
        }

        private (long total, long available) GetWindowsMemoryInfo()
        {
            // Usamos WMI para obtener información detallada en Windows
            try
            {
                long totalMemory = 0;
                long freeMemory = 0;

                // WMI para memoria total
                var mosComputer = new System.Management.ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in mosComputer.Get())
                {
                    totalMemory = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                }

                // WMI para memoria libre
                var mosOS = new System.Management.ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (var obj in mosOS.Get())
                {
                    freeMemory = Convert.ToInt64(obj["FreePhysicalMemory"]) * 1024; // Viene en KB
                }

                return (totalMemory, freeMemory);
            }
            catch
            {
                return (0, 0); // Falla si WMI no está disponible
            }
        }

        private (long total, long available) GetLinuxMemoryInfo()
        {
            var memInfo = File.ReadAllLines("/proc/meminfo");
            long totalMemory = 0;
            long availableMemory = 0;

            foreach (var line in memInfo)
            {
                if (line.StartsWith("MemTotal:"))
                {
                    totalMemory = long.Parse(line.Split(':')[1].Trim().Split(' ')[0]) * 1024; // Viene en kB
                }
                else if (line.StartsWith("MemAvailable:"))
                {
                    availableMemory = long.Parse(line.Split(':')[1].Trim().Split(' ')[0]) * 1024; // Viene en kB
                }
            }
            return (totalMemory, availableMemory);
        }

        private (long total, long available) GetMacOsMemoryInfo()
        {
            // En macOS, usamos la utilidad de línea de comandos `sysctl`
            long totalMemory = ExecuteCommandAndParseLong("/usr/sbin/sysctl", "-n hw.memsize");

            // La "memoria disponible" en macOS es más compleja; una aproximación es la memoria libre + inactiva.
            // Aquí simplificamos obteniendo solo el total, ya que obtener "disponible" es muy intrincado.
            // Podríamos parsear `vm_stat` para más detalles.
            long freeMemory = 0; // Dejamos en 0 como simplificación. La memoria total es el dato más fiable.

            return (totalMemory, freeMemory);
        }

        // Función de utilidad para convertir bytes a gigabytes
        private double ToGigabytes(long bytes) => Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), 2);

        private long ExecuteCommandAndParseLong(string fileName, string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return long.TryParse(output, out var result) ? result : 0;
            }
            catch
            {
                return 0;
            }
        }
    }






    // El contenedor principal para todas las métricas del sistema
    public record SystemMetrics
    {
        public double CpuUsedByAngelSQLServerPercentage { get; init; }
        public double TotalMemoryGB { get; init; }
        public double AvailableMemoryGB { get; init; }
        public double UsedMemoryGB => TotalMemoryGB - AvailableMemoryGB;
        public List<DiskInfo> Disks { get; init; } = new();
        public NetworkUsageInfo NetworkUsage { get; init; } = new();
        public string TimestampUtc { get; init; } = string.Empty;
    }

    // Información sobre un disco individual
    public record DiskInfo
    {
        public string Name { get; init; } = string.Empty;
        public string DriveFormat { get; init; } = string.Empty;
        public double TotalSpaceGB { get; init; }
        public double AvailableFreeSpaceGB { get; init; }
        public double UsedSpaceGB => TotalSpaceGB - AvailableFreeSpaceGB;
    }

    // Información sobre el uso de la red
    public record NetworkUsageInfo
    {
        public long BytesSent { get; init; }
        public long BytesReceived { get; init; }
    }
}
