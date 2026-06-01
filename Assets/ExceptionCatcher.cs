using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class ExceptionCatcher
{
    static ExceptionCatcher()
    {
        Application.logMessageReceived += (condition, stackTrace, type) => {
            if (type == LogType.Exception || type == LogType.Error)
            {
                if (condition.Contains("InvalidOperationException"))
                {
                    File.AppendAllText("exception_trace.txt", "Condition: " + condition + "\nStack: " + stackTrace + "\nEnvironment Stack: " + System.Environment.StackTrace + "\n\n");
                }
            }
        };
    }
}
