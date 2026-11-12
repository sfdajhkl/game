using System;
using System.IO;

namespace Arvito.Core
{
    /// <summary>Synchronous browser key/value persistence; current and backup share the desktop JSON schema.</summary>
    public sealed class BrowserSaveStore : IGameSaveStore
    {
        public const string CurrentKey = "arvito.save.v1";
        public const string BackupKey = "arvito.save.backup.v1";
        public const string PreviousLifeKey = "arvito.life.previous.v1";
        readonly Func<string, string> read;
        readonly Action<string, string> write;
        readonly Func<GameState, string> encode;
        readonly Func<string, GameState> decode;
        readonly Func<GameState, bool> valid;
        public BrowserSaveStore(Func<string, string> get, Action<string, string> set,
            Func<GameState, string> serialize, Func<string, GameState> deserialize, Func<GameState, bool> validator)
        { read = get; write = set; encode = serialize; decode = deserialize; valid = validator; }
        GameState Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { var state = decode(json); return valid(state) ? state : null; }
            catch (Exception e) when (e is ArgumentException || e is FormatException) { return null; }
        }
        public GameState Load(out string notice)
        {
            notice = "";
            try
            {
                string current = read(CurrentKey);
                var state = Parse(current); if (state != null) return state;
                state = Parse(read(BackupKey));
                if (state != null) { notice = "최근 저장을 읽지 못해 이전 기록을 복구했습니다."; return state; }
                if (current != null) notice = "저장 기록을 읽을 수 없습니다. 원본은 보존합니다.";
            }
            catch (IOException) { notice = "브라우저 저장소에 접근할 수 없습니다. 저장을 허용한 뒤 다시 열어 주세요."; }
            return null;
        }
        public void Save(GameState state)
        {
            if (!valid(state)) throw new InvalidDataException("저장할 게임 상태가 올바르지 않습니다.");
            string next = encode(state);
            string current = read(CurrentKey);
            if (Parse(current) != null) write(BackupKey, current);
            // localStorage.setItem is atomic: quota/permission failure leaves the previous value intact.
            write(CurrentKey, next);
        }
    }
}
