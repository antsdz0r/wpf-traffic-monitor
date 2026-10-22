using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace NetworkService.Services
{
    /// <summary>
    /// Pokretanje i restartovanje MeteringSimulator aplikacije.
    /// Po specifikaciji, nakon dodavanja ili brisanja entiteta simulator mora
    /// automatski da se restartuje kako bi ponovo procitao ukupan broj entiteta.
    /// </summary>
    public static class SimulatorService
    {
        // Pokusava da pronadje putanju do MeteringSimulator.exe.
        // Prvo gleda u App.config (kljuc "SimulatorPath"), a zatim relativne putanje
        // u odnosu na lokaciju NetworkService.exe.
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
                // Ugasi sve vec pokrenute instance simulatora.
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
                // Ako simulator nije dostupan, aplikacija nastavlja da radi normalno.
            }
        }
    }
}
