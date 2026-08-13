using System;
using System.Text;

namespace uEmuera.Runtime.EraElectron
{
    /// <summary>
    /// Generates the JavaScript bootstrap that is injected into the WebView before
    /// any game bundles load.
    ///
    /// The bootstrap:
    ///   1. Creates <c>window._eraBridge</c> — the low-level C→JS channel.
    ///   2. Creates <c>window.era</c> — the full ERA SDK object as the EraElectron
    ///      SDK expects it (see era-electron.js in source games).
    ///   3. Sets <c>era.version</c> to the current uEmuera engine version.
    ///
    /// Call <see cref="Build"/> once before loading any game JS.
    ///
    /// Bridge protocol (C# ↔ JS):
    ///   Sync call:  window._eraBridge.sync("method", argsJsonString) → jsonResult
    ///   Async call: window._eraBridge.beginAsync("method", argsJsonString) → callId
    ///               window._eraBridge.awaitAsync(callId)               → Promise
    ///
    /// The host (IEraElectronHost implementation) must register a message handler
    /// under the name "eraBridge" that responds to these function signatures.
    /// On WebView2: use AddHostObjectToScript or ExecuteScriptAsync injection.
    /// On Android WebView: use addJavascriptInterface.
    /// </summary>
    public static class EraElectronBridgeScript
    {
        // Keep in sync with ReferenceParity/EraElectron/API.generated.json
        private const string UEmueraSdkVersion = "0.1.0-stub";
        private const string EraElectronSdkAlias = "#/era-electron";

        /// <summary>
        /// Builds the full bootstrap JS string.
        /// </summary>
        /// <param name="engineVersion">
        /// The minimum engine version from the game's <c>.ere-min-version</c> file
        /// (e.g. "2200"). Surfaced as <c>era.version.engine</c>.
        /// </param>
        public static string Build(string engineVersion)
        {
            var sb = new StringBuilder(4096);
            sb.Append("(function(){\n");
            sb.Append("'use strict';\n");

            // ----------------------------------------------------------------
            // 1. Low-level bridge stubs (replaced by real platform objects).
            //    PlatformWebViewBridge replaces these with native bindings
            //    before executing this script.
            // ----------------------------------------------------------------
            sb.Append("if(!window._eraBridge){\n");
            sb.Append("  window._eraBridge={\n");
            sb.Append("    sync:function(m,a){console.error('[uEmuera] _eraBridge.sync not bound: '+m);return'null';},\n");
            sb.Append("    beginAsync:function(m,a){console.error('[uEmuera] _eraBridge.beginAsync not bound: '+m);return -1;},\n");
            sb.Append("    awaitAsync:function(id){return Promise.reject(new Error('[uEmuera] _eraBridge.awaitAsync not bound'));}\n");
            sb.Append("  };\n");
            sb.Append("}\n");

            // ----------------------------------------------------------------
            // 2. era.* surface — matches the EraElectron SDK interface.
            //    Sync APIs call _eraBridge.sync directly.
            //    Async APIs call _eraBridge.beginAsync then poll awaitAsync.
            // ----------------------------------------------------------------
            sb.Append("var _b=window._eraBridge;\n");
            sb.Append("function _s(m,a){return JSON.parse(_b.sync(m,JSON.stringify(a)));}\n");
            sb.Append("function _a(m,a){\n");
            sb.Append("  var id=_b.beginAsync(m,JSON.stringify(a));\n");
            sb.Append("  return _b.awaitAsync(id).then(function(r){return JSON.parse(r);});\n");
            sb.Append("}\n");

            // era.version (property object, accessed as era.version.engine / era.version.sdk)
            sb.Append("var _ver={engine:").Append(JsonString(engineVersion ?? "")).Append(",");
            sb.Append("sdk:").Append(JsonString(UEmueraSdkVersion)).Append("};\n");

            sb.Append("window.era={\n");

            // --- version ---
            sb.Append("  version:_ver,\n");

            // --- Output APIs (sync) ---
            foreach (var m in SyncOutputApis)
                sb.Append($"  {m}:function(){{return _s('{m}',[].slice.call(arguments));}},\n");

            // --- Layout APIs (sync) ---
            foreach (var m in SyncLayoutApis)
                sb.Append($"  {m}:function(){{return _s('{m}',[].slice.call(arguments));}},\n");

            // --- Data APIs (sync) ---
            foreach (var m in SyncDataApis)
                sb.Append($"  {m}:function(){{return _s('{m}',[].slice.call(arguments));}},\n");

            // --- Media APIs (sync) ---
            foreach (var m in SyncMediaApis)
                sb.Append($"  {m}:function(){{return _s('{m}',[].slice.call(arguments));}},\n");

            // --- Async APIs ---
            foreach (var m in AsyncApis)
                sb.Append($"  {m}:function(){{return _a('{m}',[].slice.call(arguments));}},\n");

            // --- era.logger sub-object ---
            sb.Append("  logger:{\n");
            foreach (var m in LoggerApis)
                sb.Append($"    {m}:function(){{_s('logger.{m}',[].slice.call(arguments));}},\n");
            sb.Append("  },\n");

            // Remove trailing comma before closing (JS strict)
            sb.Append("  _uEmuera:true\n");
            sb.Append("};\n");

            // ----------------------------------------------------------------
            // 3. Alias window.era as the module value for #/era-electron
            //    Games do: const era = require('#/era-electron');
            //    The webpack bundle aliases this to window._era (set by sdk stub)
            //    AND we also expose it as window.era (our primary name).
            // ----------------------------------------------------------------
            sb.Append("if(typeof window._era==='undefined'){window._era=window.era;}\n");

            sb.Append("})();\n");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ //
        //  API classification — derived from ERAUMA_USAGE.generated.json     //
        // ------------------------------------------------------------------ //

        static readonly string[] SyncOutputApis =
        {
            "print", "println", "drawLine", "printButton", "printMultiColumns",
            "printInColRows", "printWholeImage", "printLineChart", "replaceText",
            "replaceInColRows", "setToBottom", "notify",
        };

        static readonly string[] SyncLayoutApis =
        {
            "setAlign", "setColor", "setOffset", "setWidth",
            "setHorizontalAlign", "setVerticalAlign", "setBack", "setOverlay",
            "getLineCount",
        };

        static readonly string[] SyncDataApis =
        {
            "get", "set", "add",
            "addCharacter", "addCharacterForTrain",
            "getAddedCharacters", "getAllCharacters", "getCharactersInTrain",
            "beginTrain", "endTrain",
            "resetData",
            "checkImage", "isDebug", "quit",
        };

        static readonly string[] SyncMediaApis =
        {
            "playMusic", "stopMusic",
        };

        static readonly string[] AsyncApis =
        {
            "printAndWait", "input", "waitAnyKey", "clear",
            "delay", "saveData", "loadData", "saveGlobal", "rmData",
        };

        static readonly string[] LoggerApis =
        {
            "debug", "info", "warn", "error", "assert",
        };

        // ------------------------------------------------------------------ //

        static string JsonString(string s)
        {
            if (s == null) return "null";
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                           .Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
        }
    }
}
