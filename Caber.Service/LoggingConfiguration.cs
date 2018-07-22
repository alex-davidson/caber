using System;
using System.IO;

namespace Caber.Service
{
    public class Log4NetConfiguration
    {
        public string LogRootDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        public bool EnableDiagnostics { get; set; } = true;
        public bool EnableDebugDiagnostics { get; set; } = false;
    }
}
