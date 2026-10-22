using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace NetworkService.Services
{
   
    public static class SimulatorService
    {
        
        private static string ResolveSimulatorPath()
        {
            string configured = ConfigurationManager.AppSettings["SimulatorPath"];
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            {
                return configured;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "MeteringSimulator.exe"),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\MeteringSimulator\MeteringSimulator\bin\Debug\MeteringSimulator.exe")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\..\MeteringSimulator\MeteringSimulator\bin\Debug\MeteringSimulator.exe"))
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        public static void Restart()
        {
            try
            {
                
                foreach (var p in Process.GetProcessesByName("MeteringSimulator"))
                {
                    try { p.Kill(); } catch { /* ignore */ }
                }

                string path = ResolveSimulatorPath();
                if (path != null)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        WorkingDirectory = Path.GetDirectoryName(path)
                    });
                }
            }
            catch
            {
                
            }
        }
    }
}
