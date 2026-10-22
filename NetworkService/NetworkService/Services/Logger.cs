using System;
using System.IO;

namespace NetworkService.Services
{
    
    public static class Logger
    {
        
        private static readonly string LogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log.txt");

        private static readonly object _lock = new object();

        public static string FilePath => LogPath;

        public static void Log(int entityId, string entityName, double value)
        {
            try
            {
                
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
                
            }
        }
    }
}
