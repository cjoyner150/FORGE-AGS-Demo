using System.IO;
using UnityEngine;

namespace FORGE
{
    /// <summary>
    /// Shared static file logger. GameManager calls Init() on Start.
    /// Any script can call ForgeLog.Write() to append a line.
    /// Log file path is printed to the Unity console on Init.
    /// </summary>
    public static class ForgeLog
    {
        private static StreamWriter _writer;
        private static string       _path;

        public static void Init()
        {
            _writer?.Close();
            _path   = Path.Combine(Application.persistentDataPath, "forge_timing.txt");
            _writer = new StreamWriter(_path, append: false);
            _writer.AutoFlush = true;
            Debug.Log($"[ForgeLog] Logging to: {_path}");
        }

        public static void Write(string message)
        {
            _writer?.WriteLine(message);
        }

        public static void Close()
        {
            _writer?.Close();
            _writer = null;
        }
    }
}
