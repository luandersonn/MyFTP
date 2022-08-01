using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyFTP.Utils
{
    public static class FileHelper
    {
        public static async Task<StorageFile> CreateTempFileAsync(string extension = "")
        {
            var folder = ApplicationData.Current.TemporaryFolder;
            return await folder.CreateFileAsync(Guid.NewGuid().ToString() + extension, CreationCollisionOption.GenerateUniqueName);
        }

        public static ulong CreateHash64(this string source)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(source);
            ulong value = (ulong)utf8.Length;
            for (int n = 0; n < utf8.Length; n++)
            {
                value += (ulong)utf8[n] << (n * 5 % 56);
            }
            return value;
        }
    }
}
