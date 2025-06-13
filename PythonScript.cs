using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Solvers;

namespace AngelDB
{
    public class PythonExecutor
    {
        public string ExecutePythonScript(string scriptPath, string argument, AngelDB.DB db, string pythonFile = "")
        {
            string pythonPath;
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
            File.WriteAllText(tempFile, argument, System.Text.Encoding.UTF8);

            db.LastPythonError = "";
            db.LastPythonWarning = "";
            db.LastPythonResult = "";

            try
            {
                // Determinar la ruta de Python
                if (string.IsNullOrEmpty(pythonFile) || pythonFile == "null")
                {
                    pythonPath = Environment.GetEnvironmentVariable("PYTHON_PATH");

                    if (string.IsNullOrEmpty(pythonPath))
                    {
                        return "Error: PYTHON_PATH not found. Ensure Python is installed and PYTHON_PATH environment variable is set to python.exe.";
                    }
                }
                else
                {
                    pythonPath = pythonFile;
                }

                // Validar que el archivo python.exe existe
                if (!File.Exists(pythonPath))
                {
                    return $"Error: Python executable not found at {pythonPath}. Please provide a valid path.";
                }

                // Crear el proceso para ejecutar el script de Python
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = pythonPath, // Ruta de Python
                    Arguments = $"\"{scriptPath}\" \"{tempFile}\"", // Script y archivo temporal como argumento
                    RedirectStandardOutput = true, // Redirigir la salida estándar
                    RedirectStandardError = true,  // Redirigir los errores
                    UseShellExecute = false, // No usar la shell
                    CreateNoWindow = true  // No crear una ventana de consola
                };

                StringBuilder output = new StringBuilder();
                StringBuilder errorOutput = new StringBuilder();
                StringBuilder warningOutput = new StringBuilder();

                // Ejecutar el proceso
                using (Process process = new Process())
                {
                    process.StartInfo = psi;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            output.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            if (e.Data.Contains("ValueWarning:"))
                            {
                                warningOutput.AppendLine(e.Data);
                            } else if (e.Data.Contains("DeprecationWarning:"))
                            {
                                warningOutput.AppendLine(e.Data);
                            }
                            else if (e.Data.Contains("FutureWarning:"))
                            {
                                warningOutput.AppendLine(e.Data);
                            }
                            else if (e.Data.Contains("Warning:"))
                            {
                                warningOutput.AppendLine(e.Data);
                            }
                            else
                            {
                                errorOutput.AppendLine(e.Data);
                            }
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine(); // Lectura asíncrona de la salida estándar
                    process.BeginErrorReadLine();  // Lectura asíncrona de la salida de error

                    process.WaitForExit(); // Esperar a que el proceso termine

                    // Verificar si hubo algún error
                    if (errorOutput.Length > 0)
                    {
                        db.LastPythonError = errorOutput.ToString();
                        //return $"Error: when running the Python script: {errorOutput.ToString()}";
                    }

                    if(warningOutput.Length > 0)
                    {
                        db.LastPythonWarning = warningOutput.ToString();
                    }

                    db.LastPythonResult = output.ToString();
                    return output.ToString(); // Devolver la salida
                }
            }
            catch (Exception ex)
            {
                return $"Error: ExecutePythonScript {ex.Message}";
            }
            finally
            {
                // Eliminar el archivo temporal
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}











