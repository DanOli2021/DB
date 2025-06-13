using System;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.IO;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Text;
using System.Data;
using System.Collections.Concurrent;
using System.Globalization;
using AngelDBTools;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace AngelDB
{
    public class MyScript : IDisposable
    {

        public ConcurrentDictionary<string, Script> scripts = new ConcurrentDictionary<string, Script>();
        public ConcurrentDictionary<string, CompiledScripts> Compiled_scripts = new ConcurrentDictionary<string, CompiledScripts>();
        private bool disposedValue;

        public string Eval(string code, string data, AngelDB.DB db)
        {
            try
            {
                Globals g = new Globals();
                g.db = db;
                g.data = data;

                ScriptOptions options = ScriptOptions.Default.WithImports(new[] { "System", "System.Net", "System.Collections.Generic" });
                object o = CSharpScript.EvaluateAsync(code, options, g).GetAwaiter().GetResult();

                if (o != null)
                {
                    return o.ToString();
                }

                return "";

            }
            catch (Exception e)
            {
                return $"Error: {e.ToString()}";
            }

        }

        public string EvalExpresion(string key, string code, string data, AngelDB.DB db, DataRow r)
        {
            try
            {

                Globals g = new Globals();
                g.db = db;
                g.data = data;
                g.data_row = r;

                if (scripts.ContainsKey(key))
                {
                    return scripts[key].RunAsync(g).Result.ReturnValue.ToString();
                }

                ScriptOptions options = ScriptOptions.Default.WithImports(new[] { "System", "System.Net", "System.Collections.Generic" });

                var script = CSharpScript.Create(code, options, typeof(Globals));
                script.Compile();

                string result = script.RunAsync(g).Result.ReturnValue.ToString();

                scripts.TryAdd(key, script);
                return result;

            }
            catch (Exception e)
            {
                return $"Error: {e.ToString()}";
            }

        }


        public string EvalCode(string script_name, string code, DateTime timestamp, AngelDB.DB db, DB operational_db = null, object main_object = null, object environment = null) 
        {
            try
            {
                Globals g = new Globals();
                g.server_db = db;

                if (operational_db is not null)
                {
                    g.db = operational_db;
                }
                else
                {
                    g.db = db;
                }

                g.data = "";
                g.message = "";

                if (main_object is not null)
                {
                    g.MainObject = main_object;
                }

                if (environment is not null)
                {
                    g.environment = environment;
                }

                db.script_file_datetime = timestamp;
                db.script_file = script_name;

                if (Compiled_scripts.ContainsKey(script_name))
                {
                    if (db.script_file_datetime == Compiled_scripts[script_name].script_date)
                    {
                        //var o1 = Compiled_scripts[script_name].script.RunAsync(g).Result.ReturnValue;
                        var o1 = Compiled_scripts[script_name].script.Invoke(g);

                        if (o1.Result is null)
                        {
                            return "";
                        }
                        else
                        {
                            return o1.Result.ToString();
                        }

                    }
                }

                List<string> l = new List<string>();
                l.Add(typeof(AngelDB.DB).Assembly.FullName);
                ScriptOptions options;

                options = ScriptOptions.Default.
                            WithFilePath(AppDomain.CurrentDomain.BaseDirectory + script_name).
                            AddImports("System").WithEmitDebugInformation(true).AddReferences(l).WithFileEncoding(Encoding.UTF8);

                var script = CSharpScript.Create(code, options, typeof(Globals));
                script.Compile();

                ScriptRunner<object> runner = script.CreateDelegate();

                if (Compiled_scripts.ContainsKey(script_name))
                {
                    Compiled_scripts.TryRemove(script_name, out _);
                }

                Compiled_scripts.TryAdd(script_name, new CompiledScripts { script = runner, script_date = db.script_file_datetime });
                //var o = Compiled_scripts[script_name].script.RunAsync(g).Result.ReturnValue;
                var o = runner.Invoke(g);

                if (o.Result is null)
                {
                    return "";
                }
                else
                {
                    return o.Result.ToString();
                }

            }
            catch (Exception e2)
            {
                string error = e2.ToString();
                int pos = error.IndexOf("End of stack trace from previous location");
                if (pos > 0)
                {
                    error = error.Substring(0, pos);
                }

                return $"Error: {script_name} {error}";
            }
        }


       public class SourceCode
        {
            public string id { get; set; }
            public string code { get; set; }
        }


        public string ImportDirectory(string directory_name, string type, AngelDB.DB db) 
        {
            if( !Directory.Exists(directory_name))
            {
                return "Error: The directory does not exist: " + directory_name;
            }

            //Get all files in the directory
            string[] files = Directory.GetFiles(directory_name, type);

            foreach (string file in files)
            {
                string result = ImportCodeFromFile(file, db);

                if (result.StartsWith("Error:"))
                {
                    return result;
                }
            }

            return "Ok.";

        }


        public string ImportCodeFromFile(string file, AngelDB.DB db) 
        {
            try
            {
                if (!File.Exists(file))
                {
                    return "Error: The file does not exist: " + file;
                }

                string result = db.Prompt("GET TABLES WHERE tablename = 'sourcecode'");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                SourceCode sourceCode = new SourceCode();

                if (result == "[]")
                {
                    db.CreateTable(sourceCode);
                }

                string code = File.ReadAllText(file);
                code = CryptoString.Encrypt(code, "hbjklios", "iuybncsa");

                sourceCode.id = Path.GetFileName(file);
                sourceCode.code = code;

                result = db.UpsertInto("sourcecode", sourceCode);

                if (result.StartsWith("Error:"))
                {
                    return result + "-" + file;
                }

                Console.WriteLine("Imported: " + file);

                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: ImportCodeFromFile {e.ToString()}";
            }

        }



        public string EvalDB(Dictionary<string, string> d, AngelDB.DB db, DB operational_db = null, object main_object = null, object environment = null )
        {

            string script_name = d["db_script"];

            try
            {
                string result = "";

                result = db.Prompt("SELECT id, timestamp FROM sourcecode WHERE id = '" + script_name + "'");

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                if( result == "[]" )
                {
                    return "Error: The script does not exists.";
                }

                DataTable dt = db.GetDataTable(result);
                DataRow r = dt.Rows[0];

                Globals g = new Globals();
                g.server_db = db;

                if (operational_db is not null)
                {
                    g.db = operational_db;
                }
                else
                {
                    g.db = db;
                }

                g.data = d["data"];
                g.message = d["message"];

                if (main_object is not null)
                {
                    g.MainObject = main_object;
                }

                if (environment is not null)
                {
                    g.environment = environment;
                }

                if (d["recompile"] == "true")
                {
                    if (Compiled_scripts.ContainsKey(script_name))
                    {
                        Compiled_scripts.Remove(script_name, out _);
                    }
                }

                if (Compiled_scripts.ContainsKey(script_name))
                {
                    if (DateTime.ParseExact(r["timestamp"].ToString(), "yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture) == Compiled_scripts[script_name].script_date)
                    {
                        //var o1 = CompiledScripts[script_name].script.RunAsync(g).Result.ReturnValue;
                        var o1 = Compiled_scripts[script_name].script.Invoke(g);

                        if (o1.Result is null)
                        {
                            return "";
                        }
                        else
                        {
                            return o1.Result.ToString();
                        }

                    }
                }

                result = db.Prompt("SELECT code FROM sourcecode WHERE Id = '" + script_name + "'");
                DataTable dtCode = db.GetDataTable(result);

                string code = CryptoString.Decrypt(dtCode.Rows[0]["code"].ToString(), "hbjklios", "iuybncsa");

                int pos = code.IndexOf("END GLOBALS");

                if (pos > 0)
                {
                    string globals = code.Substring(0, pos + 11);
                    int lines = globals.Split('\n').Length;
                    code = new string('\n', lines - 1) + code.Substring(pos + 11);
                }


                if(d["main_directory"] == "null")
                {
                    d["main_directory"] = AppDomain.CurrentDomain.BaseDirectory;
                }

                List<string> l = new List<string>();
                l.Add(typeof(AngelDB.DB).Assembly.FullName);
                ScriptOptions options;

                ImmutableArray<string> array = ImmutableArray.Create(d["main_directory"]);

                g.db.Globals.Clear();

                options = ScriptOptions.Default.
                            WithFilePath(d["main_directory"] + script_name).
                            AddImports("System").
                            WithSourceResolver(new SourceFileResolver(array, d["main_directory"] + script_name)).
                            AddReferences(l).WithEmitDebugInformation(true).WithFileEncoding(Encoding.UTF8);

                var script = CSharpScript.Create(code, options, typeof(Globals));
                script.Compile();

                ScriptRunner<object> runner = script.CreateDelegate();

                if (Compiled_scripts.ContainsKey(script_name))
                {
                    Compiled_scripts.TryRemove(script_name, out _);
                }

                Compiled_scripts.TryAdd(script_name, new CompiledScripts { script = runner, script_date = DateTime.ParseExact(r["timestamp"].ToString(), "yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture) });
                //var o = Compiled_scripts[file].script.RunAsync(g).Result.ReturnValue;
                var o = runner.Invoke(g);

                if (o.Result is null)
                {
                    return "";
                }
                else
                {
                    if (o.Result is null)
                    {
                        return "";
                    }

                    return o.Result.ToString();
                }

            }
            catch (Exception e2)
            {
                string error = e2.ToString();
                int pos = error.IndexOf("End of stack trace from previous location");
                if (pos > 0)
                {
                    error = error.Substring(0, pos);
                }

                return $"Error: {script_name} {error}";
            }

        }

        public string CompileCsxToDll(string csxFilePath, string outputDllPath)
        {
            try
            {
                if (!File.Exists(csxFilePath))
                {
                    return $"Error: The script file does not exist at {csxFilePath}";
                }

                string code = File.ReadAllText(csxFilePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var references = AppDomain.CurrentDomain.GetAssemblies()
                                        .Where(a => !a.IsDynamic)
                                        .Select(a => MetadataReference.CreateFromFile(a.Location))
                                        .Cast<MetadataReference>()
                                        .ToList();

                var compilation = CSharpCompilation.Create(
                    Path.GetFileNameWithoutExtension(csxFilePath),
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                var result = compilation.Emit(outputDllPath);

                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    string errors = string.Join(Environment.NewLine, failures.Select(f => f.GetMessage()));
                    return $"Error: compiling script to DLL: {errors}";
                }

                return $"DLL successfully compiled at: {outputDllPath}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public string EvalFile(Dictionary<string, string> d, AngelDB.DB db, DB operational_db = null, object main_object = null, object environment = null, AngelDB.TaskState taskState = null)
        {

            FileInfo fileInfo = null;
            string file = "";

            try
            {

                file = d["script_file"];

                if (d["on_main_directory"] == "true")
                {
                    file = db.BaseDirectory + "/" + d["script_file"];
                }
                else if (d["on_database"] == "true")
                {
                    file = db.BaseDirectory + db.os_directory_separator + db.account + db.os_directory_separator + db.database + db.os_directory_separator + d["script_file"];
                }
                else if (d["on_table"] != "null")
                {
                    file = db.BaseDirectory + db.os_directory_separator + db.account + db.os_directory_separator + db.database + db.os_directory_separator + d["on_table"] + db.os_directory_separator + d["script_file"];
                }
                else if (d["on_application_directory"] == "true")
                {
                    file = Environment.CurrentDirectory + "/" + d["script_file"];
                }
                else if (db.apps_directory != "null")
                {
                    if (!string.IsNullOrEmpty(db.apps_directory))
                    {
                        file = db.apps_directory + db.os_directory_separator + d["script_file"];
                    }
                }

                if (!File.Exists(file))
                {
                    return $"Error: The script file does not exists {file}";
                }

                Globals g = new Globals();
                g.server_db = db;
                g.Taskstate = taskState;

                if (operational_db is not null)
                {
                    g.db = operational_db;
                }
                else 
                {
                    g.db = db;
                }

                g.data = d["data"];
                g.message = d["message"];

                if (main_object is not null)
                {
                    g.MainObject = main_object;
                }

                if (environment is not null)
                {
                    g.environment = environment;
                }

                db.script_file_datetime = File.GetLastWriteTime(file);
                db.script_file = file;

                if (d["recompile"] == "true") 
                {
                    if (Compiled_scripts.ContainsKey(file)) 
                    {
                        Compiled_scripts.Remove(file, out _);
                    }                     
                }

                if (Compiled_scripts.ContainsKey(file))
                {
                    if (db.script_file_datetime == Compiled_scripts[file].script_date)
                    {
                        //var o1 = Compiled_scripts[file].script.RunAsync(g).Result.ReturnValue;
                        var o1 = Compiled_scripts[file].script.Invoke(g);

                        if (o1.Result is null)
                        {
                            return "";
                        }
                        else
                        {
                            return o1.Result.ToString();
                        }

                    }
                }

                string code = File.ReadAllText(file);
                int pos = code.IndexOf("END GLOBALS");

                if (pos > 0)
                {
                    string globals = code.Substring(0, pos + 11);
                    int lines = globals.Split('\n').Length;
                    code = new string('\n', lines - 1) + code.Substring(pos + 11);
                }


                List<string> l = new List<string>();
                l.Add(typeof(AngelDB.DB).Assembly.FullName);
                ScriptOptions options;

                fileInfo = new FileInfo(file);

                ImmutableArray<string> array = ImmutableArray.Create(fileInfo.Directory.FullName);

                g.db.Globals.Clear();

                options = ScriptOptions.Default.
                    WithFilePath(fileInfo.FullName).
                    AddImports("System").
                    WithSourceResolver(new SourceFileResolver(array, fileInfo.Directory.FullName)).
                    AddReferences(l).WithEmitDebugInformation(true).WithFileEncoding(Encoding.UTF8);

                var script = CSharpScript.Create(code, options, typeof(Globals));
                script.Compile();

                ScriptRunner<object> runner = script.CreateDelegate();

                if (Compiled_scripts.ContainsKey(file))
                {
                    Compiled_scripts.TryRemove(file, out _);
                }

                Compiled_scripts.TryAdd(file, new CompiledScripts { script = runner, script_date = db.script_file_datetime });

                //var o = Compiled_scripts[file].script.RunAsync(g).Result.ReturnValue;
                var o = runner.Invoke(g);
                
                if (o.Result is null)
                {
                    return "";
                }
                else
                {
                    if (o.Result is null)
                    {
                        return "";
                    }

                    return o.Result.ToString();
                }

            }
            catch (Exception e2)
            {
                string error = e2.ToString();
                int pos = error.IndexOf("End of stack trace from previous location");
                if (pos > 0)
                {
                    error = error.Substring(0, pos);
                }

                return $"Error: {file} {error}";
            }

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: eliminar el estado administrado (objetos administrados)
                }

                // TODO: liberar los recursos no administrados (objetos no administrados) y reemplazar el finalizador
                // TODO: establecer los campos grandes como NULL
                disposedValue = true;
                scripts = new ConcurrentDictionary<string, Script>();
                Compiled_scripts = null;
            }
        }

        // // TODO: reemplazar el finalizador solo si "Dispose(bool disposing)" tiene código para liberar los recursos no administrados
        // ~MyScript()
        // {
        //     // No cambie este código. Coloque el código de limpieza en el método "Dispose(bool disposing)".
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose()
        {
            // No cambie este código. Coloque el código de limpieza en el método "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


    }


}


public class PreprocessingSourceResolver : SourceFileResolver
{
    public PreprocessingSourceResolver(ImmutableArray<string> searchPaths, string baseDirectory)
        : base(ImmutableArray<string>.Empty, baseDirectory)
    {
    }

    public override string ResolveReference(string path, string baseFilePath)
    {
        var fullPath = base.ResolveReference(path, baseFilePath);
        if (File.Exists(fullPath))
        {
            string code = File.ReadAllText(fullPath);
            return PreprocessScriptToTempFile(code, fullPath);
        }

        return fullPath;
    }

    private string PreprocessScriptToTempFile(string code, string originalPath)
    {
        const string startMarker = "// GLOBALS";
        const string endMarker = "// END GLOBALS";

        int start = code.IndexOf(startMarker);
        int end = code.IndexOf(endMarker);

        if (start >= 0 && end > start)
        {
            string globalsSection = code.Substring(start, end + endMarker.Length - start);
            int lineCount = globalsSection.Count(c => c == '\n') + 1; // cuenta líneas

            string padding = string.Join("", Enumerable.Repeat("\n", lineCount));
            code = code.Substring(0, start) + padding + code.Substring(end + endMarker.Length);
        }

        string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(originalPath) + "_" + Guid.NewGuid().ToString("N") + ".csx");
        File.WriteAllText(tempPath, code);
        return tempPath;
    }
}

public class Globals
{
    public AngelDB.DB db;
    public AngelDB.DB server_db;
    public object MainObject;
    public object environment;
    public string return_result = "";
    public string data = "";
    public string message = "";
    public AngelDB.TaskState Taskstate = null;
    public DataRow data_row;
}

public class CompiledScripts
{
    public System.DateTime script_date;
    public ScriptRunner<object> script;
}

