using System;
using System.IO;
using System.Text;

namespace Arvito.Core
{
    public interface IGameSaveStore
    {
        GameState Load(out string notice);
        void Save(GameState state);
    }
    /// <summary>JSON is supplied by Unity JsonUtility or the .NET test harness. Atomic replace with a validated backup.</summary>
    public sealed class SaveStore : IGameSaveStore
    {
        readonly string path;
        readonly Func<GameState, string> encode;
        readonly Func<string, GameState> decode;
        readonly Func<GameState, bool> valid;
        public string Path => path;
        public SaveStore(string file, Func<GameState, string> serialize, Func<string, GameState> deserialize, Func<GameState, bool> validator)
        { path = file; encode = serialize; decode = deserialize; valid = validator; }
        GameState Read(string file)
        {
            try { if (!File.Exists(file)) return null; var state = decode(File.ReadAllText(file, Encoding.UTF8)); return valid(state) ? state : null; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is FormatException) { return null; }
        }
        public GameState Load(out string notice)
        {
            notice = ""; var current = Read(path); if (current != null) return current;
            var backup = Read(path + ".backup");
            if (backup != null) { notice = "최근 저장을 읽지 못해 이전 기록을 복구했습니다."; return backup; }
            if (File.Exists(path)) notice = "저장 기록을 읽을 수 없습니다. 원본 파일은 보존합니다.";
            return null;
        }
        public void Save(GameState state)
        {
            if (!valid(state)) throw new InvalidDataException("저장할 게임 상태가 올바르지 않습니다.");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            File.WriteAllText(temp, encode(state), new UTF8Encoding(false));
            if (Read(path) != null) File.Copy(path, path + ".backup", true);
            // Same-directory replacement is atomic on Windows and Linux; no delete-then-write gap.
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }
}
