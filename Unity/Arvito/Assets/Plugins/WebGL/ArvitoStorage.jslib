mergeInto(LibraryManager.library, {
  $ArvitoStorageState: { readFailed: 0 },
  ArvitoStorageRead__deps: ['$ArvitoStorageState'],
  ArvitoStorageRead: function(key) {
    ArvitoStorageState.readFailed = 0;
    try {
      var value = window.localStorage.getItem(UTF8ToString(key));
      if (value === null) return 0;
      var size = lengthBytesUTF8(value) + 1;
      var buffer = _malloc(size);
      stringToUTF8(value, buffer, size);
      return buffer;
    } catch (error) { ArvitoStorageState.readFailed = 1; return 0; }
  },
  ArvitoStorageReadFailed__deps: ['$ArvitoStorageState'],
  ArvitoStorageReadFailed: function() { return ArvitoStorageState.readFailed; },
  ArvitoStorageWrite: function(key, value) {
    try { window.localStorage.setItem(UTF8ToString(key), UTF8ToString(value)); return 0; }
    catch (error) { return 1; }
  }
});
