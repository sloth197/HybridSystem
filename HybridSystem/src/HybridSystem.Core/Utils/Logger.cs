using System;
using System.IO;

namespace HybridSystem.Core.Utils
{
    public static class Logger
    {
        private static readonly string LogPath = "./logs/system.log";
        public static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath) ?? "./logs");
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            }
            catch
            {
                /*swallow*/
            }
        }
    }
}