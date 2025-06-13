using System;
using System.Collections.Generic;
using System.IO;

namespace AngelDB
{

    public class Base64Helper
    {
        public static string SaveBase64ToAutoNamedFile(string base64String, string outputDirectory, string baseFileName)
        {
            try
            {
                // Diccionario de tipos MIME comunes a extensiones
                var mimeToExt = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"image/jpeg", ".jpg"},
                {"image/png", ".png"},
                {"image/gif", ".gif"},
                {"image/bmp", ".bmp"},
                {"application/pdf", ".pdf"},
                {"text/plain", ".txt"},
                {"application/zip", ".zip"},
                {"application/msword", ".doc"},
                {"application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx"},
                {"application/vnd.ms-excel", ".xls"},
                {"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx"}
            };

                string mimeType = "application/octet-stream";
                string base64Data = base64String;

                // Extraer MIME y limpiar base64
                if (base64String.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = base64String.Split(',');
                    if (parts.Length == 2)
                    {
                        var header = parts[0]; // data:image/png;base64
                        base64Data = parts[1];

                        int start = header.IndexOf(':') + 1;
                        int end = header.IndexOf(';');
                        mimeType = header.Substring(start, end - start);
                    }
                }

                byte[] fileBytes = Convert.FromBase64String(base64Data);

                string extension = mimeToExt.ContainsKey(mimeType) ? mimeToExt[mimeType] : ".bin";
                string outputPath = Path.Combine(outputDirectory, baseFileName + extension);

                if( File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                File.WriteAllBytes(outputPath, fileBytes);
                return outputPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }

}
