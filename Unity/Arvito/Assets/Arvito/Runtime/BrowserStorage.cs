#if UNITY_WEBGL && !UNITY_EDITOR
using System.IO;
using System.Runtime.InteropServices;

namespace Arvito.Runtime
{
    static class BrowserStorage
    {
        [DllImport("__Internal")] static extern string ArvitoStorageRead(string key);
        [DllImport("__Internal")] static extern int ArvitoStorageWrite(string key, string value);
        [DllImport("__Internal")] static extern int ArvitoStorageReadFailed();
        public static string Read(string key)
        {
            string value = ArvitoStorageRead(key);
            if (ArvitoStorageReadFailed() != 0) throw new IOException("Browser storage is unavailable.");
            return value;
        }
        public static void Write(string key, string value)
        {
            if (ArvitoStorageWrite(key, value) != 0) throw new IOException("Browser storage is full or unavailable.");
        }
    }
}
#endif
