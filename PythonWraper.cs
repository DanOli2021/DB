using System;
using Python.Runtime;

public class PythonWraper
{
    public dynamic Scope { get; private set; }

    public PythonWraper()
    {
        using (Py.GIL())
        {
            Scope = Py.CreateScope();
        }
    }

    public string Exec(string pythonFilePath, string functionName, string message)
    {
        try
        {
            using (Py.GIL())
            {
                dynamic py = Py.Import("__main__");
                dynamic script = py.open(pythonFilePath).read();
                Scope.Exec(script);

                dynamic pythonFunction = Scope.Get(functionName);
                return pythonFunction(message);
            }
        }
        catch (PythonException ex)
        {
            return $"Error: Python: {ex.Message}";
        }
    }

    public object ExecString(string pythonCode)
    {
        try
        {
            using (Py.GIL())
            {
                Scope.Exec(pythonCode);
                return "Ok.";
            }
        }
        catch (PythonException ex)
        {
            return $"Error: Python: {ex.Message}";
        }
    }

}
