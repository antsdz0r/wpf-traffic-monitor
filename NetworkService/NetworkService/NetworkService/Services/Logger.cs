using System;
using System.IO;

namespace NetworkService.Services
{
    /// <summary>
    /// Upisuje svaku pristiglu vrednost merenja u Log .txt datoteku na disku.
    /// Po specifikaciji belezimo: vremenski trenutak, referencu na entitet i vrednost.
    /// </summary>
    public static class Logger
    {
        // Log se pravi pored .exe fajla aplikacije.
        private static readonly string LogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log.txt");

        private static readonly object _lock = new object();

        public static string FilePath => LogPath;

        public static void Log(int entityId, string entityName, double value)
        {
            try
            {
                // lock zbog moguceg istovremenog upisa iz vise niti (TCP ThreadPool).
                lock (_lock)
                {
                    string line = string.Format("{0}\tEntitet ID={1} ({2})\tVrednost={3}",
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        entityId,
                        entityName,
                        value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Greska pri upisu u log ne sme da srusi aplikaciju.
            }
        }
    }
}
