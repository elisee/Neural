using System;
using System.IO;

namespace Neural
{
    public static class FileHelper
    {
        public static string FindAppFolder(string folderName)
        {
            var basePath = AppContext.BaseDirectory;

            while (true)
            {
                var folderPath = Path.Combine(basePath, folderName);
                if (Directory.Exists(folderPath)) return folderPath;

                basePath = Directory.GetParent(basePath).FullName;
            }
        }
    }
}
