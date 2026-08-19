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
            // 1. Low-level platform bridge.
            //    Sync APIs use WebView2's synchronous COM host-object proxy.
            //    Async APIs use postMessage and are completed by _eraResolve/_eraReject.
            // ----------------------------------------------------------------
            sb.Append("var _pending=Object.create(null),_nextCallId=1;\n");
            sb.Append("window._eraResolve=function(id,r){var p=_pending[id];if(!p)return;delete _pending[id];p.resolve(r);};\n");
            sb.Append("window._eraReject=function(id,e){var p=_pending[id];if(!p)return;delete _pending[id];p.reject(new Error(e||'ERA call failed'));};\n");
            sb.Append("var _wv=(window.chrome&&window.chrome.webview)?window.chrome.webview:null;\n");
            sb.Append("var _native=_wv&&_wv.hostObjects&&_wv.hostObjects.sync?window.chrome.webview.hostObjects.sync.eraNative:null;\n");
            sb.Append("if(!window._eraBridge){\n");
            sb.Append("  window._eraBridge={\n");
            sb.Append("    sync:function(m,a){if(!_native)throw new Error('[uEmuera] synchronous native bridge unavailable: '+m);return _native.DispatchSync(m,a);},\n");
            sb.Append("    beginAsync:function(m,a){if(!_wv)throw new Error('[uEmuera] asynchronous native bridge unavailable: '+m);var id=_nextCallId++;var resolve,reject;var promise=new Promise(function(ok,fail){resolve=ok;reject=fail;});_pending[id]={promise:promise,resolve:resolve,reject:reject};_wv.postMessage(JSON.stringify({id:id,method:m,args:a,isAsync:true}));return id;},\n");
            sb.Append("    awaitAsync:function(id){var p=_pending[id];return p?p.promise:Promise.reject(new Error('[uEmuera] unknown async call '+id));}\n");
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

            sb.Append("var _layout={align:'left',color:'',offset:0,width:24,horizontal:'start',vertical:'top'};\n");
            sb.Append("var _anchors={topleft:'left top',topcenter:'center top',top:'center top',topright:'right top',centerleft:'left center',left:'left center',center:'center center',centerright:'right center',right:'right center',bottomleft:'left bottom',bottomcenter:'center bottom',bottom:'center bottom',bottomright:'right bottom','1':'left bottom','2':'center bottom','3':'right bottom','4':'left center','5':'center center','6':'right center','7':'left top','8':'center top','9':'right top'};\n");
            sb.Append("function _clamp(v,min,max,fallback){v=Number(v);return Number.isFinite(v)?Math.max(min,Math.min(max,v)):fallback;}\n");
            sb.Append("function _root(){var r=document.getElementById('uemuera-output');if(r)return r;r=document.createElement('main');r.id='uemuera-output';r.style.cssText='box-sizing:border-box;position:relative;z-index:1;width:100%;min-height:100vh;padding:12px;overflow:auto;background:#111;color:#eee;font-family:sans-serif';document.body.appendChild(r);return r;}\n");
            sb.Append("function _content(parent,value){if(value===null||value===undefined)return;if(Array.isArray(value)){value.forEach(function(v){_content(parent,v);});return;}if(typeof value==='object'){if(value.isBr){parent.appendChild(document.createElement('br'));return;}if(value.isBlank){var blank=document.createElement('span');blank.style.display='inline-block';blank.style.width=(value.isBlank===true?1:value.isBlank)+'em';parent.appendChild(blank);return;}if(value.isDivider){parent.appendChild(document.createElement('hr'));return;}var span=value.url?document.createElement('a'):document.createElement('span');if(value.url){span.href=value.url;span.rel='noopener noreferrer';}['color','display','fontSize','fontStyle','fontWeight'].forEach(function(k){if(value[k]!==undefined)span.style[k]=value[k];});if(value.title!==undefined)span.title=value.title;_content(span,value.content);parent.appendChild(span);return;}parent.appendChild(document.createTextNode(String(value)));}\n");
            sb.Append("function _imageUrl(name){if(window._eraImageResolver)return window._eraImageResolver(name);name=String(name||'');if(/^(data:|https?:|\\/)/i.test(name))return name;return 'game/res/'+name.split('/').map(encodeURIComponent).join('/');}\n");
            sb.Append("function _position(value){return _anchors[String(value===undefined?'center':value).toLowerCase()]||'center center';}\n");
            sb.Append("function _gridElement(item){item=item||{};var cfg=item.config||{};var el;if(item.type==='divider'){el=document.createElement('div');var hr=document.createElement('hr');el.appendChild(hr);if(cfg.content)_content(el,cfg.content);}else if(item.type==='button'){el=document.createElement('button');el.type='button';_content(el,item.content);el.dataset.accelerator=String(item.accelerator);el.disabled=!!cfg.disabled;el.onclick=function(){_supplyInput(item.accelerator);};}else if(item.type==='image'||item.type==='image.whole'){el=document.createElement('div');var names=Array.isArray(item.names)?item.names:[item.names];names.forEach(function(name){if(name===undefined)return;var img=document.createElement('img');img.src=_imageUrl(name);img.alt=String(name);img.style.maxWidth='100%';img.style.height='auto';img.style.objectFit=cfg.fit||'contain';img.style.objectPosition=_position(cfg.position);el.appendChild(img);});}else if(item.type==='progress'){el=document.createElement('div');var progress=document.createElement('progress');progress.max=100;progress.value=_clamp(item.percentage,0,100,0);progress.style.width='100%';el.appendChild(progress);_content(el,item.inContent);_content(el,item.outContent);}else{el=document.createElement('div');_content(el,item.content!==undefined?item.content:item);}var width=_clamp(cfg.width,1,24,_layout.width);var offset=_clamp(cfg.offset,0,23,_layout.offset);if(offset+width>24)width=24-offset;el.style.gridColumn=(offset+1)+' / span '+Math.max(1,width);el.style.boxSizing='border-box';el.style.textAlign=cfg.align||_layout.align;if(cfg.color||_layout.color)el.style.color=cfg.color||_layout.color;if(cfg.fontSize)el.style.fontSize=cfg.fontSize;if(cfg.fontWeight)el.style.fontWeight=cfg.fontWeight;return el;}\n");
            sb.Append("function _row(items,cfg,replace){var row=document.createElement('section');row.className='uemuera-row';row.style.display='grid';row.style.gridTemplateColumns='repeat(24,minmax(0,1fr))';row.style.justifyItems=(cfg&&cfg.horizontalAlign)||_layout.horizontal;var vertical=(cfg&&cfg.verticalAlign)||_layout.vertical;row.style.alignItems=vertical==='middle'?'center':vertical==='bottom'?'end':'start';(Array.isArray(items)?items:[items]).forEach(function(item){row.appendChild(_gridElement(item));});var root=_root();if(replace&&root.lastElementChild)root.replaceChild(row,root.lastElementChild);else root.appendChild(row);return row;}\n");
            sb.Append("function _layer(id,name,cfg,z){var old=document.getElementById(id);if(old)old.remove();if(!name)return;cfg=cfg||{};var layer=document.createElement('div');layer.id=id;layer.style.cssText='position:fixed;inset:0;pointer-events:none;background-repeat:no-repeat';layer.style.zIndex=String(z);layer.style.opacity=String(_clamp(cfg.opacity,0,1,1));layer.style.backgroundImage='url(\"'+_imageUrl(name).replace(/\"/g,'%22')+'\")';layer.style.backgroundSize=cfg.fit||'cover';layer.style.backgroundPosition=_position(cfg.position);document.body.insertBefore(layer,document.body.firstChild);}\n");
            sb.Append("var _inputQueue=[],_inputWaiters=[];function _supplyInput(value){if(_inputWaiters.length)_inputWaiters.shift()(value);else _inputQueue.push(value);}function _waitInput(){if(_inputQueue.length)return Promise.resolve(_inputQueue.shift());return new Promise(function(resolve){_inputWaiters.push(resolve);});}\n");
            sb.Append("document.addEventListener('keydown',function(e){if(e.target&&e.target.id==='uemuera-input')return;var value=/^[0-9]$/.test(e.key)?Number(e.key):e.key;_supplyInput(value);});\n");
            sb.Append("function _callAndRender(name,args,render){var result=_generatedNative[name].apply(null,args);render(args);return result;}\n");
            sb.Append("var _generatedNative={};Object.keys(window.era).forEach(function(k){if(typeof window.era[k]==='function')_generatedNative[k]=window.era[k];});\n");
            sb.Append("window.era.print=function(content,config){return _callAndRender('print',arguments,function(){_row({type:'text',content:content,config:config||{}},config||{});});};\n");
            sb.Append("window.era.println=function(){return _callAndRender('println',arguments,function(){_row({type:'text',content:''},{});});};\n");
            sb.Append("window.era.drawLine=function(config){return _callAndRender('drawLine',arguments,function(){_row({type:'divider',config:config||{}},config||{});});};\n");
            sb.Append("window.era.printButton=function(content,accelerator,config){return _callAndRender('printButton',arguments,function(){_row({type:'button',content:content,accelerator:accelerator,config:config||{}},config||{});});};\n");
            sb.Append("window.era.printImage=function(){var names=[].slice.call(arguments);return _callAndRender('printImage',arguments,function(){_row({type:'image',names:names,config:{}},{});});};\n");
            sb.Append("window.era.printWholeImage=function(names,config){return _callAndRender('printWholeImage',arguments,function(){_row({type:'image.whole',names:names,config:config||{}},config||{});});};\n");
            sb.Append("window.era.printProgress=function(percentage,inContent,outContent,config){return _callAndRender('printProgress',arguments,function(){_row({type:'progress',percentage:percentage,inContent:inContent,outContent:outContent,config:config||{}},config||{});});};\n");
            sb.Append("window.era.printMultiColumns=function(items,config){return _callAndRender('printMultiColumns',arguments,function(){_row(items,config||{});});};\n");
            sb.Append("window.era.printInColRows=function(){var args=[].slice.call(arguments);return _callAndRender('printInColRows',arguments,function(){args.forEach(function(group){_row(group.columns||group,(group&&group.config)||{});});});};\n");
            sb.Append("window.era.replaceText=function(content,config){return _callAndRender('replaceText',arguments,function(){_row({type:'text',content:content,config:config||{}},config||{},true);});};\n");
            sb.Append("window.era.replaceInColRows=function(){var args=[].slice.call(arguments);return _callAndRender('replaceInColRows',arguments,function(){_row(args,{},true);});};\n");
            sb.Append("window.era.setAlign=function(v){var r=_generatedNative.setAlign(v);_layout.align=v||'left';return r;};window.era.setColor=function(v){var r=_generatedNative.setColor(v);_layout.color=v||'';return r;};window.era.setOffset=function(v){var r=_generatedNative.setOffset(v);_layout.offset=_clamp(v,0,23,0);return r;};window.era.setWidth=function(v){var r=_generatedNative.setWidth(v);_layout.width=_clamp(v,1,24,24);return r;};\n");
            sb.Append("window.era.setHorizontalAlign=function(v){var r=_generatedNative.setHorizontalAlign(v);_layout.horizontal=v||'start';return r;};window.era.setVerticalAlign=function(v){var r=_generatedNative.setVerticalAlign(v);_layout.vertical=v||'top';return r;};\n");
            sb.Append("window.era.setBack=function(name,cfg){var r=_generatedNative.setBack(name,cfg);_layer('uemuera-background',name,cfg,0);return r;};window.era.setOverlay=function(name,cfg){var r=_generatedNative.setOverlay(name,cfg);_layer('uemuera-overlay',name,cfg,2);return r;};window.era.setTitle=function(title){var r=_generatedNative.setTitle(title);document.title=String(title||'');return r;};\n");
            sb.Append("window.era.input=function(){var input=document.createElement('input');input.id='uemuera-input';input.autocomplete='off';var row=_row({type:'text',content:''},{});row.appendChild(input);input.focus();return new Promise(function(resolve){var done=false;function finish(value){if(done)return;done=true;var index=_inputWaiters.indexOf(finish);if(index>=0)_inputWaiters.splice(index,1);row.remove();resolve(value);}input.addEventListener('keydown',function(e){if(e.key!=='Enter')return;var value=input.value;/^-?[0-9]+$/.test(value)&&(value=Number(value));finish(value);});_inputWaiters.push(finish);});};\n");
            sb.Append("window.era.waitAnyKey=function(){return _waitInput().then(function(){return null;});};window.era.printAndWait=function(content,config){window.era.print(content,config);return _waitInput().then(function(){return _root().children.length;});};window.era.clear=function(){return _generatedNative.clear.apply(null,arguments).then(function(result){var r=_root();while(r.firstChild)r.removeChild(r.firstChild);return result;});};\n");

            // ----------------------------------------------------------------
            // 3. Preserve helpers from the game's era.bundle.js while replacing
            //    native API methods with the uEmuera bridge implementations.
            // ----------------------------------------------------------------
            sb.Append("var _generated=window.era;\n");
            sb.Append("if(window._era&&typeof window._era==='object'){\n");
            sb.Append("  var _sdk=window._era;Object.keys(_generated).forEach(function(k){if(k!=='version')_sdk[k]=_generated[k];});\n");
            sb.Append("  _sdk.version=_sdk.version||{};_sdk.version.engine=_ver.engine;_sdk.version.sdk=_sdk.version.sdk||_ver.sdk;\n");
            sb.Append("  window.era=_sdk;\n");
            sb.Append("}else{window._era=_generated;}\n");

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
            "replaceInColRows", "setToBottom", "notify", "printImage", "printProgress",
        };

        static readonly string[] SyncLayoutApis =
        {
            "setAlign", "setColor", "setOffset", "setWidth",
            "setHorizontalAlign", "setVerticalAlign", "setBack", "setOverlay",
            "setTitle", "getLineCount",
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
