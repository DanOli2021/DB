using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DB
{
    public static class RemoteDesktopHelper
    {
        public static string StartRdpSessionWithResources(string serverAddress,
                                                        string username,
                                                        string password,
                                                        int? port = null,
                                                        bool fullScreen = false,
                                                        int? width = null,
                                                        int? height = null,
                                                        bool isAdmin = false,
                                                        bool redirectDisks = false,
                                                        bool redirectPrinters = false,
                                                        bool redirectClipboard = false)
        {
            try
            {
                // Validar el nombre o dirección del servidor
                if (string.IsNullOrWhiteSpace(serverAddress))
                {
                    return "Error: La dirección del servidor es requerida.";
                }

                // Limpiar el nombre del servidor de posibles espacios
                serverAddress = serverAddress.Trim();

                // Verificar si es una dirección IP válida
                bool isIpAddress = Regex.IsMatch(serverAddress, @"^\d{1,3}(\.\d{1,3}){3}$");

                // Si no es una IP, intenta resolver el nombre de dominio
                if (!isIpAddress)
                {
                    try
                    {
                        var hostEntry = System.Net.Dns.GetHostEntry(serverAddress);
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        return "Error: No se pudo resolver el nombre del servidor. Verifique la dirección.";
                    }
                }

                // Crear el contenido del archivo .rdp
                string rdpFileContent = $"full address:s:{serverAddress}\n";

                // Si hay un puerto, agregarlo
                if (port.HasValue)
                {
                    rdpFileContent += $"server port:i:{port.Value}\n";
                }

                // Configuración para redirección de recursos
                rdpFileContent += $"redirectdrives:i:{(redirectDisks ? 1 : 0)}\n";
                rdpFileContent += $"redirectprinters:i:{(redirectPrinters ? 1 : 0)}\n";
                rdpFileContent += $"redirectclipboard:i:{(redirectClipboard ? 1 : 0)}\n";

                // Configuración de pantalla
                if (fullScreen)
                {
                    rdpFileContent += "screen mode id:i:2\n";  // Pantalla completa
                }
                else if (width.HasValue && height.HasValue)
                {
                    rdpFileContent += $"desktopwidth:i:{width.Value}\n";
                    rdpFileContent += $"desktopheight:i:{height.Value}\n";
                }

                // Configuración para admin
                if (isAdmin)
                {
                    rdpFileContent += "administrative session:i:1\n";
                }

                // Guardar el archivo RDP temporalmente
                string rdpFilePath = Path.Combine(Path.GetTempPath(), "tempRDP.rdp");
                File.WriteAllText(rdpFilePath, rdpFileContent);

                // Agregar las credenciales usando cmdkey
                Process cmdkeyProcess = new Process();
                cmdkeyProcess.StartInfo.FileName = "cmd.exe";
                cmdkeyProcess.StartInfo.Arguments = $"/C cmdkey /generic:TERMSRV/{serverAddress} /user:{username} /pass:{password}";
                cmdkeyProcess.StartInfo.CreateNoWindow = true;
                cmdkeyProcess.StartInfo.UseShellExecute = false;
                cmdkeyProcess.Start();
                cmdkeyProcess.WaitForExit();

                // Ejecutar mstsc.exe con el archivo RDP generado
                Process rdpProcess = new Process();
                rdpProcess.StartInfo.FileName = "mstsc.exe";
                rdpProcess.StartInfo.Arguments = rdpFilePath;

                // Iniciar el proceso
                rdpProcess.Start();
                return "Ok.";
            }
            catch (System.Exception e)
            {
                return $"Error: {e.Message}";
            }
        }
    }
}

