uEmuera
=======

<img src="Assets/splash/icon.png" width="256"/>

[English](#english) | [中文](#中文) | [Deutsch](#deutsch)

---

## English

**Special Note for Android 10:**
**If the app cannot find files placed in sdcard/uEmuera, try placing them in sdcard/Android/data/xerysherry.uEmuera/files/ instead.**

Emuera stands for "Emulator of Eramaker", a text-based game platform for Windows.

This project is a Unity3D port of Emuera. It leverages Unity3D's cross-platform capabilities to make it easier to port to non-Windows platforms.

The current project is based on the emuera1824v15 source code with additional EM/EE (Emuera MultipleEx/Emuera EnhanceEx) extensions implemented.

It can run almost all era script games!

### What's New in This Fork (vs [xerysherry/uEmuera](https://github.com/xerysherry/uEmuera))

This fork includes significant improvements and modernizations:

#### Engine & Platform Updates
- **Unity 6 (6000.2.14f1)** - Upgraded from Unity 2018/2019 for better performance and modern features
- **Updated deprecated Unity APIs** - Migrated to modern Unity 6 compatible APIs
- **Lightweight rendering** - Configured for optimized rendering

#### Extended Game Compatibility
- **Emuera EM/EE Extensions** - Added support for extended Emuera commands:
  - `BINPUT` / `BINPUTS` - Binary input commands
  - `TRYCALLF` / `TRYCALLFORMF` - Try-call function variants
- **Sound Commands** - Full audio implementation with `AudioManager`:
  - `PLAYSOUND` / `STOPSOUND` - Sound effect playback
  - `PLAYBGM` / `STOPBGM` - Background music playback
  - `EXISTSOUND` - Sound file existence check
  - Supports WAV format with volume control
- **GXX Graphics Instructions** - Implementation support for GXX drawing commands
- **Case-Insensitive Folder Names** - Automatically detects `resources/`, `Resources/`, `RESOURCES/`, etc.

#### Internationalization & Documentation
- **Multi-language README** - English, Chinese, and German documentation
- **Unity Localization Support** - Built-in localization for English, Chinese (Simplified), and Japanese
- **Comment Translation** - Japanese and Chinese code comments translated to English
- **XML Documentation** - Added comprehensive XML documentation to public APIs

#### Development & Quality
- **Unit Test Framework** - Integrated Unity Test Framework with EditMode and PlayMode tests
- **Copilot Instructions** - Project-specific AI development guidelines
- **Gothic Fonts Support** - Additional font options

#### Bug Fixes
- Fixed image loading issues
- Fixed WebP format errors
- Fixed EmueraContent generation logic

### Download
----

[https://github.com/noa3/uEmuera/releases](https://github.com/noa3/uEmuera/releases)

### How to Use
--------

1. Ensure that all era-related files are encoded in UTF-8, including \*.csv, \*.ERB, and \*.ERH files.

2. When running the app for the first time, grant "File Access" permission.

3. Place the processed era script folder in the emuera folder on your sdcard. Full paths: storage/emulated/0/emuera, storage/emulated/1/emuera, storage/emulated/2/emuera

### Known Issues / Areas for Improvement
-------------------

1. Cannot modify era game configuration within the app

2. No debugging functionality

3. Some game instructions have low efficiency, causing lag

4. May consume more battery (a common issue with Unity3D applications)

5. OGG and MP3 audio formats require async loading - WAV recommended for synchronous playback to avoid timing issues

6. ...

### Screenshots
----

Start Screen
![Screenshot1](Screenshot/screenshot1.png)
Game Running Screen
![Screenshot2](Screenshot/screenshot2.png)
Quick Buttons
![Screenshot3](Screenshot/screenshot3.png)
Command Input
![Screenshot4](Screenshot/screenshot4.png)
Zoom Control
![Screenshot5](Screenshot/screenshot5.png)

---

## 中文

**Android10 特别说明：**
**如果放入sdcard/uEmuera下无法找到的话，可以放入sdcard/Android/data/xerysherry.uEmuera/files/下**

Emuera是Emulator of Eramaker的缩写，是Windows平台下文字游戏平台。

该项目为Emuera的Unity3D移植版本。意在利用Unity3D多平台特性，方便移植到非Windows平台。

当前项目以基于emuera1824v15版本源代码，并添加了EM/EE（Emuera MultipleEx/Emuera EnhanceEx）扩展支持。

几乎可以执行所有era脚本游戏！

### 本分支的新功能 (相比 [xerysherry/uEmuera](https://github.com/xerysherry/uEmuera))

本分支包含重大改进和现代化更新：

#### 引擎与平台更新
- **Unity 6 (6000.2.14f1)** - 从Unity 2018/2019升级，获得更好的性能和现代特性
- **更新已弃用的Unity API** - 迁移到现代Unity 6兼容的API
- **轻量级渲染** - 配置优化渲染

#### 扩展游戏兼容性
- **Emuera EM/EE扩展** - 添加扩展Emuera命令支持：
  - `BINPUT` / `BINPUTS` - 二进制输入命令
  - `TRYCALLF` / `TRYCALLFORMF` - Try-call函数变体
- **音频命令** - 完整的音频实现（AudioManager）：
  - `PLAYSOUND` / `STOPSOUND` - 音效播放
  - `PLAYBGM` / `STOPBGM` - 背景音乐播放
  - `EXISTSOUND` - 音频文件存在检查
  - 支持WAV格式和音量控制
- **GXX图形指令** - 实现GXX绘图命令支持
- **文件夹名大小写不敏感** - 自动检测 `resources/`、`Resources/`、`RESOURCES/` 等

#### 国际化与文档
- **多语言README** - 英文、中文和德文文档
- **Unity本地化支持** - 内置英文、中文（简体）和日文本地化
- **注释翻译** - 日文和中文代码注释翻译为英文
- **XML文档** - 为公共API添加全面的XML文档

#### 开发与质量
- **单元测试框架** - 集成Unity测试框架，包含EditMode和PlayMode测试
- **Copilot指南** - 项目特定的AI开发指南
- **哥特字体支持** - 额外的字体选项

#### 错误修复
- 修复图片加载问题
- 修复WebP格式错误
- 修复EmueraContent生成逻辑

### 下载
----

[https://github.com/noa3/uEmuera/releases](https://github.com/noa3/uEmuera/releases)

### 如何使用：
--------

1. 请确保era相关文件编码为UTF8，包括\*.csv, \*.ERB, \*.ERH。

2. 请在初次运行app时，选择允许"文件访问"的权限。

3. 请把处理完毕的era脚本文件夹放置在sdcard下的emuera文件夹内。完整路径为storage/emulated/0/emuera, storage/emulated/1/emuera, storage/emulated/2/emuera

### 已知问题/需要改进项：
-------------------

1. 无法在app内修改era游戏配置

2. 无调试功能

3. 部分游戏的某些指令效率较低，导致卡顿

4. 可能会比较耗电（Unity3D程序通病）

5. OGG和MP3音频格式需要异步加载 - 建议使用WAV格式进行同步播放以避免时序问题

6. ...

### 截图
----

开始界面
![Screenshot1](Screenshot/screenshot1.png)
游戏运行界面
![Screenshot2](Screenshot/screenshot2.png)
快捷按钮
![Screenshot3](Screenshot/screenshot3.png)
指令输入
![Screenshot4](Screenshot/screenshot4.png)
缩放控制
![Screenshot5](Screenshot/screenshot5.png)

---

## Deutsch

**Hinweis für Android 10:**
**Wenn die App Dateien in sdcard/uEmuera nicht finden kann, versuche sie in sdcard/Android/data/xerysherry.uEmuera/files/ zu platzieren.**

Emuera steht für "Emulator of Eramaker", eine textbasierte Spielplattform für Windows.

Dieses Projekt ist eine Unity3D-Portierung von Emuera. Es nutzt die plattformübergreifenden Fähigkeiten von Unity3D, um die Portierung auf Nicht-Windows-Plattformen zu erleichtern.

Das aktuelle Projekt basiert auf dem emuera1824v15-Quellcode mit zusätzlich implementierten EM/EE (Emuera MultipleEx/Emuera EnhanceEx)-Erweiterungen.

Es kann fast alle Era-Skriptspiele ausführen!

### Neuerungen in diesem Fork (vs [xerysherry/uEmuera](https://github.com/xerysherry/uEmuera))

Dieser Fork enthält bedeutende Verbesserungen und Modernisierungen:

#### Engine- & Plattform-Updates
- **Unity 6 (6000.2.14f1)** - Upgrade von Unity 2018/2019 für bessere Leistung und moderne Funktionen
- **Aktualisierte veraltete Unity-APIs** - Migration auf moderne Unity 6-kompatible APIs
- **Leichtgewichtiges Rendering** - Optimierte Rendering-Konfiguration

#### Erweiterte Spielkompatibilität
- **Emuera EM/EE-Erweiterungen** - Unterstützung für erweiterte Emuera-Befehle:
  - `BINPUT` / `BINPUTS` - Binäre Eingabebefehle
  - `TRYCALLF` / `TRYCALLFORMF` - Try-Call-Funktionsvarianten
- **Sound-Befehle** - Vollständige Audio-Implementierung mit `AudioManager`:
  - `PLAYSOUND` / `STOPSOUND` - Soundeffekt-Wiedergabe
  - `PLAYBGM` / `STOPBGM` - Hintergrundmusik-Wiedergabe
  - `EXISTSOUND` - Prüfung auf Sounddatei-Existenz
  - Unterstützt WAV-Format mit Lautstärkeregelung
- **GXX-Grafikbefehle** - Implementierungsunterstützung für GXX-Zeichenbefehle
- **Groß-/Kleinschreibung bei Ordnernamen ignorieren** - Erkennt automatisch `resources/`, `Resources/`, `RESOURCES/`, etc.

#### Internationalisierung & Dokumentation
- **Mehrsprachiges README** - Englische, chinesische und deutsche Dokumentation
- **Unity-Lokalisierungsunterstützung** - Integrierte Lokalisierung für Englisch, Chinesisch (vereinfacht) und Japanisch
- **Kommentar-Übersetzungen** - Japanische und chinesische Code-Kommentare ins Englische übersetzt
- **XML-Dokumentation** - Umfassende XML-Dokumentation für öffentliche APIs

#### Entwicklung & Qualität
- **Unit-Test-Framework** - Integriertes Unity Test Framework mit EditMode- und PlayMode-Tests
- **Copilot-Anweisungen** - Projektspezifische KI-Entwicklungsrichtlinien
- **Gothic-Schriftarten-Unterstützung** - Zusätzliche Schriftoptionen

#### Fehlerbehebungen
- Bildladeproblem behoben
- WebP-Formatfehler behoben
- EmueraContent-Generierungslogik behoben

### Download
----

[https://github.com/noa3/uEmuera/releases](https://github.com/noa3/uEmuera/releases)

### Verwendung:
--------

1. Stelle sicher, dass alle Era-bezogenen Dateien UTF-8-kodiert sind, einschließlich \*.csv, \*.ERB und \*.ERH.

2. Erteile beim ersten Start der App die Berechtigung "Dateizugriff".

3. Platziere den verarbeiteten Era-Skriptordner im emuera-Ordner auf deiner SD-Karte. Vollständige Pfade: storage/emulated/0/emuera, storage/emulated/1/emuera, storage/emulated/2/emuera

### Bekannte Probleme / Verbesserungsbereiche
-------------------

1. Kann Era-Spielkonfiguration nicht innerhalb der App ändern

2. Keine Debugging-Funktionalität

3. Einige Spielanweisungen haben geringe Effizienz und verursachen Verzögerungen

4. Kann mehr Batterie verbrauchen (ein häufiges Problem bei Unity3D-Anwendungen)

5. OGG- und MP3-Audioformate erfordern asynchrones Laden - WAV wird für synchrone Wiedergabe empfohlen, um Timing-Probleme zu vermeiden

6. ...

### Screenshots
----

Startbildschirm
![Screenshot1](Screenshot/screenshot1.png)
Spielbildschirm
![Screenshot2](Screenshot/screenshot2.png)
Schnelltasten
![Screenshot3](Screenshot/screenshot3.png)
Befehlseingabe
![Screenshot4](Screenshot/screenshot4.png)
Zoom-Steuerung
![Screenshot5](Screenshot/screenshot5.png)
