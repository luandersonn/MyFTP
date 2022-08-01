using MyFTP.Services;
using System;
using System.IO;
using Windows.Storage;

namespace MyFTP.Utils
{
    public static class LoggerFactory
    {
        public static string LogDefaultFolderPath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "Logs");
        public static ILogger CreateLogger(string id)
        {
            try
            {
                if (!Directory.Exists(LogDefaultFolderPath))
                    Directory.CreateDirectory(LogDefaultFolderPath);

                var fileName = id.CreateHash64().ToString() + ".log";
                var filePath = System.IO.Path.Combine(LogDefaultFolderPath, fileName);
                return FileLogger.Create(filePath);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
