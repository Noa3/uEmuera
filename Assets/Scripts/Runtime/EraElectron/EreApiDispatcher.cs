using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace uEmuera.Runtime.EraElectron
{
    /// <summary>
    /// Routes era.* API calls from the JS bridge to C# implementations.
    ///
    /// Each era.* method maps to one of five sub-dispatchers:
    ///   EreOutputApi   — print, println, drawLine, printButton, ...
    ///   EreInputApi    — input, waitAnyKey, printAndWait
    ///   EreDataApi     — get, set, add, character ops
    ///   EreSaveApi     — saveData, loadData, resetData, ...
    ///   EreMediaApi    — playMusic, stopMusic, ...
    ///
    /// Implements <see cref="IEraNativeBridge"/> so the host can invoke it.
    ///
    /// CURRENT STATUS: STUB.
    /// All methods return placeholder values or throw NotImplementedException.
    /// Real implementations will be added per API milestone.
    /// </summary>
    public sealed class EreApiDispatcher : IEraNativeBridge
    {
        readonly EreDataModel  _data;
        readonly RuntimeContext _context;

        // Pending async calls: callId → TaskCompletionSource
        readonly Dictionary<int, TaskCompletionSource<string>> _pending =
            new Dictionary<int, TaskCompletionSource<string>>();
        int _nextCallId = 1;

        // Current line count (incremented by output APIs)
        int _lineCount;

        // era.version object — injected by the JS bridge wrapper
        // Engine version read from the game's .ere-min-version at runtime start.
        string _engineVersion = string.Empty;

        public EreApiDispatcher(EreDataModel data, RuntimeContext context)
        {
            _data    = data    ?? throw new ArgumentNullException(nameof(data));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Called by <see cref="EraElectronRuntime"/> after reading the game's
        /// .ere-min-version so that era.version.engine returns the correct value.
        /// </summary>
        public void SetEngineVersion(string version)
        {
            _engineVersion = version ?? string.Empty;
        }

        // ------------------------------------------------------------------ //
        //  IEraNativeBridge                                                    //
        // ------------------------------------------------------------------ //

        public string DispatchSync(string method, string argsJson)
        {
            try
            {
                return DispatchSyncInternal(method, argsJson);
            }
            catch (NotImplementedException)
            {
                _context.Logger?.Warn(
                    $"[EreApiDispatcher] era.{method} not yet implemented.");
                return "null";
            }
            catch (Exception ex)
            {
                _context.Logger?.Error(
                    $"[EreApiDispatcher] era.{method} error: {ex.Message}", ex);
                throw;
            }
        }

        public int BeginAsync(string method, string argsJson)
        {
            int id = _nextCallId++;
            var tcs = new TaskCompletionSource<string>();
            lock (_pending) _pending[id] = tcs;

            // Start async resolution on Unity main thread.
            _context.MainThread?.Post(() =>
            {
                _ = ResolveAsyncAsync(id, method, argsJson);
            });

            return id;
        }

        public async Task<string> AwaitAsync(
            int callId, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<string> tcs;
            lock (_pending)
            {
                if (!_pending.TryGetValue(callId, out tcs))
                    return "null"; // already resolved or unknown
            }

            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                string result = await tcs.Task;
                lock (_pending) _pending.Remove(callId);
                return result;
            }
        }

        // ------------------------------------------------------------------ //
        //  Sync dispatch                                                       //
        // ------------------------------------------------------------------ //

        string DispatchSyncInternal(string method, string argsJson)
        {
            // Args parsing is deferred to full implementation.
            // Stubs return 0 (line count), null, or trivial values.
            switch (method)
            {
                // --- Output (sync) ---
                case "print":          return StubLineNumber();
                case "println":        return StubLineNumber();
                case "drawLine":       return StubLineNumber();
                case "printButton":    return StubLineNumber();
                case "printImage":     return StubLineNumber();
                case "printMultiColumns": return StubLineNumber();
                case "printInColRows": return StubLineNumber();
                case "printProgress":  return StubLineNumber();
                case "printWholeImage": return StubLineNumber();
                case "printLineChart": return StubLineNumber();
                case "replaceText":    return StubLineNumber();
                case "replaceInColRows": return StubLineNumber();
                case "setToBottom":    return StubLineNumber();

                // --- Layout state (stub: values not yet forwarded to renderer) ---
                case "setAlign":           return "null";
                case "setColor":           return "null";
                case "setOffset":          return "null";
                case "setWidth":           return "null";
                case "setHorizontalAlign": return "null";
                case "setVerticalAlign":   return "null";
                case "setBack":            return "null";
                case "setOverlay":         return "null";
                case "setMask":            return "null"; // deprecated
                case "setTitle":           return "null";
                case "notify":             return "null";

                // --- Data (sync) ---
                case "get":                 return SerializeValue(_data.Get(ParseVarName(argsJson)));
                case "set":                 return SerializeValue(_data.Set(ParseVarName(argsJson), ParseValue(argsJson)));
                case "add":                 return SerializeValue(_data.Add(ParseVarName(argsJson), ParseValue(argsJson)));
                case "addCharacter":        return SerializeBool(_data.AddCharacter(ParseIntArg(argsJson)));
                case "addCharacterForTrain": return "null";
                case "beginTrain":          return "null";
                case "endTrain":            return "null";
                case "nextTurnInTrain":     return "null";
                case "removeCharacter":     _data.RemoveCharacter(ParseIntArg(argsJson)); return "null";
                case "resetCharacter":      return "null";
                case "resetData":           _data.ResetAll(); return "null";
                case "getAddedCharacters":  return SerializeIntArray(_data.AddedCharacters);
                case "getAllCharacters":     return SerializeIntArray(_data.AllCharacters);
                case "getCharactersInTrain":return SerializeIntArray(_data.CharactersInTrain);
                case "getLineCount":        return _lineCount.ToString();

                // --- Audio ---
                case "playMusic":    return "true";
                case "stopMusic":    return "null";
                case "resumeMusic":  return "null";

                // --- Misc ---
                case "isDebug":      return "false";
                case "checkImage":   return "false";
                case "toggleDebug":  return "false";
                case "quit":         Application_Quit(); return "null";

                // --- Version object (era.version.engine / era.version.sdk) ---
                // The JS bridge injects era.version as a plain object; this path
                // handles the rare case where game code calls era.version() directly.
                case "version":
                    return "{\"engine\":" +
                           "\"" + _engineVersion.Replace("\"","\\\"") + "\"" +
                           ",\"sdk\":\"0.0.0\"}";

                // --- Logger sub-object (era.logger.debug / .warn / .error etc.) ---
                // Routed here when the JS bridge forwards era.logger.* calls.
                case "logger.debug":
                    _context.Logger?.Info($"[JS debug] {ParseLogMessage(argsJson)}"); return "null";
                case "logger.info":
                    _context.Logger?.Info($"[JS info] {ParseLogMessage(argsJson)}"); return "null";
                case "logger.warn":
                    _context.Logger?.Warn(ParseLogMessage(argsJson)); return "null";
                case "logger.error":
                    _context.Logger?.Error(ParseLogMessage(argsJson)); return "null";
                case "logger.assert":
                    // era.logger.assert(condition, message)
                    // Skip logging when condition is truthy; log as error if falsy.
                    _context.Logger?.Warn($"[assert] {ParseLogMessage(argsJson)}");
                    return "null";

                // --- Async that should not come through sync path ---
                case "input":
                case "waitAnyKey":
                case "clear":
                case "printAndWait":
                case "loadData":
                case "saveData":
                case "saveGlobal":
                case "loadGlobal":
                case "resetGlobal":
                case "rmData":
                case "isLandscape":
                case "delay":
                    throw new InvalidOperationException(
                        $"era.{method} is async; use BeginAsync instead.");

                default:
                    _context.Logger?.Warn($"[EreApiDispatcher] Unknown sync method: {method}");
                    return "null";
            }
        }

        // ------------------------------------------------------------------ //
        //  Async resolution                                                    //
        // ------------------------------------------------------------------ //

        async Task ResolveAsyncAsync(int callId, string method, string argsJson)
        {
            string result = "null";
            try
            {
                result = await DispatchAsyncInternal(method, argsJson);
            }
            catch (Exception ex)
            {
                _context.Logger?.Error($"[EreApiDispatcher] era.{method} async error: {ex.Message}", ex);
            }

            lock (_pending)
            {
                if (_pending.TryGetValue(callId, out var tcs))
                    tcs.TrySetResult(result);
            }
        }

        async Task<string> DispatchAsyncInternal(string method, string argsJson)
        {
            switch (method)
            {
                case "input":
                case "waitAnyKey":
                case "printAndWait":
                    // TODO: present to player, await input, resolve with player value
                    _context.Logger?.Warn($"[EreApiDispatcher] era.{method} — STUB awaiting 1 s");
                    await Task.Delay(1000);
                    return "0"; // stub: always return 0

                case "clear":
                    _lineCount = 0;
                    await Task.Yield();
                    return _lineCount.ToString();

                case "delay":
                    int ms = ParseIntArg(argsJson);
                    await Task.Delay(ms > 0 ? ms : 0);
                    return "null";

                case "saveData":
                    return SerializeBool(await SaveDataAsync(ParseIntArg(argsJson), argsJson));

                case "loadData":
                    return SerializeBool(await LoadDataAsync(ParseIntArg(argsJson)));

                case "rmData":
                    return SerializeBool(await RmDataAsync(ParseIntArg(argsJson)));

                case "saveGlobal":
                case "loadGlobal":
                case "resetGlobal":
                    return "true";

                case "isLandscape":
                    return "true"; // always landscape on desktop

                default:
                    _context.Logger?.Warn($"[EreApiDispatcher] Unknown async method: {method}");
                    return "null";
            }
        }

        // ------------------------------------------------------------------ //
        //  Save helpers                                                        //
        // ------------------------------------------------------------------ //

        Task<bool> SaveDataAsync(int slotIndex, string argsJson)
        {
            try
            {
                byte[] data = _data.Serialize();
                _context.Storage?.SaveSlot($"save_{slotIndex}", data);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _context.Logger?.Error($"[EreApiDispatcher] saveData({slotIndex}): {ex.Message}");
                return Task.FromResult(false);
            }
        }

        Task<bool> LoadDataAsync(int slotIndex)
        {
            try
            {
                byte[] data = _context.Storage?.LoadSlot($"save_{slotIndex}");
                if (data == null) return Task.FromResult(false);
                return Task.FromResult(_data.Deserialize(data));
            }
            catch (Exception ex)
            {
                _context.Logger?.Error($"[EreApiDispatcher] loadData({slotIndex}): {ex.Message}");
                return Task.FromResult(false);
            }
        }

        Task<bool> RmDataAsync(int slotIndex)
        {
            try
            {
                _context.Storage?.DeleteSlot($"save_{slotIndex}");
                return Task.FromResult(true);
            }
            catch { return Task.FromResult(false); }
        }

        // ------------------------------------------------------------------ //
        //  Argument parsing helpers (TODO: replace with proper JSON parser)   //
        // ------------------------------------------------------------------ //

        static string ParseVarName(string argsJson)
        {
            // Extremely naive; replace with real JSON parsing
            var s = argsJson?.Trim('[', ']', '"', '\'');
            return string.IsNullOrEmpty(s) ? "unknown" : s.Split(',')[0].Trim('"', ' ');
        }

        static string ParseLogMessage(string argsJson)
        {
            // era.logger.*(message) — extract first string argument
            if (string.IsNullOrEmpty(argsJson)) return string.Empty;
            string s = argsJson.Trim();
            // Strip outer array brackets if present
            if (s.StartsWith("[") && s.EndsWith("]"))
                s = s.Substring(1, s.Length - 2).Trim();
            // Strip surrounding quotes
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                s = s.Substring(1, s.Length - 2);
            return s;
        }

        static object ParseValue(string argsJson)
        {
            // TODO: real JSON parse
            return 0L;
        }

        static int ParseIntArg(string argsJson)
        {
            // TODO: real JSON parse
            if (string.IsNullOrEmpty(argsJson)) return 0;
            var s = argsJson.Trim('[', ']', ' ');
            int.TryParse(s.Split(',')[0].Trim(), out int v);
            return v;
        }

        // ------------------------------------------------------------------ //
        //  Serialization helpers                                               //
        // ------------------------------------------------------------------ //

        string StubLineNumber() => (++_lineCount).ToString();

        static string SerializeValue(object v)
        {
            if (v == null) return "null";
            if (v is string s) return "\"" + s.Replace("\"", "\\\"") + "\"";
            return v.ToString();
        }

        static string SerializeBool(bool v) => v ? "true" : "false";

        static string SerializeIntArray(IReadOnlyList<int> list)
        {
            if (list == null || list.Count == 0) return "[]";
            var sb = new System.Text.StringBuilder("[");
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(list[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }

        static void Application_Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
