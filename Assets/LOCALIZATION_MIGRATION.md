# Unity Localization Package Migration Guide

## Übersicht

Dieses Projekt wurde aktualisiert, um das Unity Localization Package zu verwenden. Die Übersetzungen werden jetzt automatisch angewendet, wenn die Sprache gewechselt wird, anstatt manuell über Button-Klicks.

## Installation

### 1. Unity Localization Package installieren

1. Öffnen Sie den Unity Package Manager (`Window > Package Manager`)
2. Klicken Sie auf `+` und wählen Sie `Add package by name...`
3. Geben Sie ein: `com.unity.localization`
4. Klicken Sie auf `Add`

### 2. Localization Settings erstellen

1. Gehen Sie zu `Edit > Project Settings > Localization`
2. Klicken Sie auf `Create` um die Localization Settings zu erstellen
3. Wählen Sie einen Ordner (empfohlen: `Assets/Localization`)

## String Tables Setup

### 1. String Tables erstellen

Erstellen Sie zwei String Tables:

#### UI Table (für UI-Elemente)
1. `Window > Asset Management > Localization Tables`
2. Klicken Sie auf `New Table Collection`
3. Name: `UI`
4. Type: `String Table Collection`
5. Wählen Sie die Sprachen (Locales):
   - Chinese (Simplified) - zh-Hans
   - English - en
   - Japanese - ja

#### Messages Table (für dynamische Nachrichten)
1. Wiederholen Sie die Schritte oben
2. Name: `Messages`
3. Type: `String Table Collection`

### 2. Locales hinzufügen

Falls Locales fehlen:

1. `Window > Asset Management > Localization Tables`
2. Klicken Sie auf `Locale Generator`
3. Fügen Sie hinzu:
   - Chinese (Simplified) - Code: `zh-Hans`
   - English - Code: `en`
   - Japanese - Code: `ja`

### 3. String Table Einträge erstellen

#### UI Table Einträge

Basierend auf den existierenden Sprachdateien (`Assets/Resources/Lang/*.txt`):

##### Menu Texte
| Key | zh-Hans | en | ja |
|-----|---------|----|----|
| `Options.MenuPad.Menu1.resolution.Text` | ????? | Resolution | ????????? |
| `Options.MenuPad.Menu1.language.Text` | ???? | Language | ????? |
| `Options.MenuPad.Menu1.github.Text` | Github | Github | Github |
| `Options.MenuPad.Menu1.exit.Text` | ???? | Exit | ????????? |
| `Options.MenuPad.Menu2.back.Text` | ???? | Back To Menu | ??????? |
| `Options.MenuPad.Menu2.intent.Text` | ???? | Intent Setting | ??????? |
| `Options.MenuPad.Menu2.savelog.Text` | ???? | Save Log | ??????? |
| `Options.MenuPad.Menu2.gototitle.Text` | ???? | Back To Title | ??????? |
| `Options.MenuPad.Menu2.restart.Text` | ???? | Reload | ????? |
| `Options.MenuPad.Menu2.exit.Text` | ???? | Exit | ?????? |

##### Resolution Texte
| Key | zh-Hans | en | ja |
|-----|---------|----|----|
| `Options.Resolution.Pad.1080p.Text` | 1080p | 1080p | 1080p |
| `Options.Resolution.Pad.900p.Text` | 900p | 900p | 900p |
| `Options.Resolution.Pad.720p.Text` | 720p | 720p | 720p |
| `Options.Resolution.Pad.540p.Text` | 540p | 540p | 540p |

##### Language Box Texte
| Key | zh-Hans | en | ja |
|-----|---------|----|----|
| `Options.LanguageBox.border.titlebar.title` | ???? | Choose Language | ????? |
| `Options.LanguageBox.border.zh_cn.Text` | ???? | ???? | ???? |
| `Options.LanguageBox.border.jp.Text` | ??? | ??? | ??? |
| `Options.LanguageBox.border.en_us.Text` | English | English | English |

##### Intent Box Texte
| Key | zh-Hans | en | ja |
|-----|---------|----|----|
| `Options.IntentBox.border.titlebar.title` | ???? | Intent Setting | ??????? |
| `Options.IntentBox.border.L_left.Text` | ? | ? | ? |
| `Options.IntentBox.border.L_right.Text` | ? | ? | ? |
| `Options.IntentBox.border.R_left.Text` | ? | ? | ? |
| `Options.IntentBox.border.R_right.Text` | ? | ? | ? |
| `Options.IntentBox.border.close.Text` | ?? | Close | ??? |
| `Options.IntentBox.border.reset.Text` | ?? | Reset | ???? |

##### First Window Texte
| Key | zh-Hans | en | ja |
|-----|---------|----|----|
| `FirstWindow.Titlebar.title` | ???? | Game List | ?????? |

##### Menu Width Einstellungen (sprachabhängig)
| Key | zh-Hans | en | ja |
|-----|---------|----|----|
| `<Menu1>` | 200 | 200 | 310 |
| `<Menu2>` | 200 | 240 | 280 |

#### Messages Table Einträge

Für dynamische Nachrichten (Bracket-Keys):

| Key | zh-Hans | en | ja |
|-----|---------|----|----|
| `[Wait]` | ?? | Wait | ??? |
| `[WaitContent]` | ???????? | Wait Processing Finish! | ????????????????? |
| `[ReloadGame]` | ?????? | Reload Game | ??????????? |
| `[ReloadGameContent]` | ????????? | Will reload game. Are you sure? | ????????????? |
| `[BackMenu]` | ???? | Back To Menu | ??????? |
| `[BackMenuContent]` | ??????? | Will back to menu. Are you sure? | ??????????? |
| `[BackTitle]` | ???? | Back To title | ??????? |
| `[BackTitleContent]` | ??????? | Will back to title. Are you sure? | ??????????? |
| `[SaveLog]` | ???? | Save Log | ????? |
| `[SavePath]` | ???? | Log Path | ???? |
| `[Failure]` | ?? | Failure | ?? |
| `[Exit]` | ???? | Exit | ????????? |
| `[ExitContent]` | ??????? | Will exit. Are you sure? | ??????????? |

#### System Table Einträge

Für System-Nachrichten und Fehlermeldungen:

| Key | zh-Hans | en | ja |
|-----|---------|----|----|
| `System.Loading` | ???... | Loading... | ?????... |
| `System.Initializing` | ????... | Initializing... | ????... |
| `System.LoadingERB` | ???? ERB ??... | Loading ERB files... | ERB??????????... |
| `System.LoadingCSV` | ???? CSV ??... | Loading CSV files... | CSV??????????... |
| `System.ProcessingComplete` | ???? | Processing Complete | ???? |
| `System.PressEnterToExit` | ????????? | Press Enter key or click to exit | ???????????????????? |
| `Error.NotImplemented` | ???????????? | This feature is not available in the current version | ?????????????????? |
| `Error.FolderNotFound` | ?????? | Folder not found | ???????????? |
| `Error.CSVFolderNotFound` | ??? CSV ??? | CSV folder not found | csv???????????? |
| `Error.ERBFolderNotFound` | ??? ERB ??? | ERB folder not found | erb???????????? |
| `Error.DebugFolderCreateFailed` | ?? debug ????? | Failed to create debug folder | debug?????????????? |
| `Error.FileOrFolderNotFound` | ???????????? | The specified file or folder does not exist | ????????????????????? |
| `Error.OnlyERBFilesAllowed` | ???? ERB ?? | Only ERB files can be dropped | ????????????ERB???????? |
| `Error.AlreadyRunning` | ?????? | Already running | ????????? |
| `Error.AllowMultipleInstancesHint` | ??????????? emuera.config | To allow multiple instances, modify emuera.config | ????????????emuera.config????????? |
| `MessageBox.Title` | ?? | Notice | ?? |
| `MessageBox.FolderNotFoundTitle` | ?????? | Folder not found | ?????? |

### 4. String Table CSV Import (Optional)

Sie können CSV-Dateien verwenden, um String Tables schnell zu importieren:

1. Erstellen Sie eine CSV-Datei mit diesem Format:
```csv
Key,zh-Hans,en,ja
Options.MenuPad.Menu1.resolution.Text,?????,Resolution,?????????
Options.MenuPad.Menu1.language.Text,????,Language,?????
```

2. Importieren Sie die CSV:
   - Öffnen Sie die String Table Collection
   - Klicken Sie auf `Import > CSV`
   - Wählen Sie Ihre CSV-Datei

## LocalizationHelper GameObject Setup

### 1. LocalizationHelper zur Szene hinzufügen

1. Erstellen Sie ein leeres GameObject in Ihrer Hauptszene
2. Benennen Sie es `LocalizationManager`
3. Fügen Sie die `LocalizationHelper` Komponente hinzu
4. Dieses GameObject wird mit `DontDestroyOnLoad` über Szenen hinweg erhalten bleiben

### 2. Initialisierung

Die Initialisierung erfolgt automatisch beim Start:
- Die gespeicherte Sprache wird aus `PlayerPrefs` geladen
- Falls keine Sprache gespeichert ist, wird Chinesisch (Simplified) als Standard verwendet
- Das System abonniert das `SelectedLocaleChanged` Event

## UI-Komponenten Setup

### Methode 1: LocalizedTextComponent verwenden (Empfohlen)

Für automatische Übersetzung:

1. Wählen Sie ein GameObject mit einer `Text` Komponente
2. Fügen Sie die `LocalizedTextComponent` Komponente hinzu
3. Konfigurieren Sie:
   - `Table Name`: `UI` oder `Messages`
   - `Key`: Der Lokalisierungsschlüssel (z.B. `Options.MenuPad.Menu1.resolution.Text`)
   - `Use Legacy For Bracket Keys`: ? (für `[Wait]`, `[Exit]`, etc.)

**Automatische Key-Ableitung:**
Die Komponente kann Keys automatisch aus der GameObject-Hierarchie ableiten:
- GameObject-Pfad: `Options/MenuPad/Menu1/resolution`
- Abgeleiteter Key: `Options.MenuPad.Menu1.resolution.Text`

### Methode 2: Unity's LocalizeStringEvent verwenden

Alternative Methode mit Unity's eingebauter Komponente:

1. Fügen Sie `Localize String Event` Komponente hinzu
2. Setzen Sie `String Reference` auf den gewünschten Key
3. Fügen Sie ein `Update String` Event hinzu
4. Verknüpfen Sie es mit der `Text.text` Property

### Methode 3: Code-basiert

```csharp
// UI String abrufen
string menuText = LocalizationHelper.GetUIString("Options.MenuPad.Menu1.resolution.Text");

// Message String abrufen
string waitText = LocalizationHelper.GetMessageString("[Wait]");

// Sprache wechseln
LocalizationHelper.SetLanguageByCode("en_us");

// Auf Sprachwechsel reagieren
LocalizationHelper.OnLanguageChanged += OnLanguageChanged;

void OnLanguageChanged()
{
    // Texte aktualisieren
}
```

## Migration von bestehenden UI-Elementen

### Schritt-für-Schritt Anleitung

1. **Prefabs öffnen:**
   - `Assets/Resources/Prefab/FirstWindow.prefab`
   - `Assets/Resources/Prefab/Options.prefab`

2. **LocalizedTextComponent hinzufügen:**
   - Wählen Sie alle Text-Komponenten im Prefab
   - Fügen Sie `LocalizedTextComponent` hinzu
   - Lassen Sie den Key leer für automatische Ableitung

3. **Keys manuell setzen (falls nötig):**
   - Wenn die automatische Ableitung nicht funktioniert
   - Setzen Sie den Key manuell entsprechend der String Table

4. **Testen:**
   - Spielen Sie die Szene ab
   - Wechseln Sie die Sprache über das Menü
   - Alle Texte sollten sich automatisch aktualisieren

## Sprache wechseln

### Programmatisch

```csharp
// Via MultiLanguage (bevorzugt - behält Kompatibilität)
MultiLanguage.SetLanguage("en_us");

// Via LocalizationHelper direkt
LocalizationHelper.SetLanguageByCode("zh_cn");
```

### Language Codes Mapping

| Alter Code | Unity Locale | Beschreibung |
|------------|--------------|--------------|
| `default` | `zh-Hans` | Chinesisch (Vereinfacht) |
| `zh_cn` | `zh-Hans` | Chinesisch (Vereinfacht) |
| `en_us` | `en` | Englisch |
| `jp` | `ja` | Japanisch |

## Erweiterte Features

### Custom String Tables

Um neue String Tables zu erstellen:

1. Erstellen Sie eine neue String Table Collection
2. Verwenden Sie `LocalizationHelper.GetLocalizedString(tableName, key)`

```csharp
string customText = LocalizationHelper.GetLocalizedString("MyCustomTable", "MyKey");
```

### Async String Loading

Für große Übersetzungen oder On-Demand Loading:

```csharp
LocalizationHelper.GetLocalizedStringAsync("UI", "MyKey", (result) => {
    myText.text = result;
});
```

### Width Anpassung für Menüs

Einige UI-Elemente benötigen sprachabhängige Breitenänderungen:

```csharp
// In LocalizedTextComponent
adjustWidth = true;
widthReference = "Menu1"; // oder "Menu2"
```

Dies passt die RectTransform-Breite basierend auf dem `<Menu1>` oder `<Menu2>` Wert in der String Table an.

## Debugging

### Localization Settings überprüfen

1. `Edit > Project Settings > Localization`
2. Überprüfen Sie:
   - `Selected Locale` ist gesetzt
   - `Available Locales` enthält alle Sprachen
   - `String Database` referenziert die Tables

### String Table überprüfen

1. `Window > Asset Management > Localization Tables`
2. Wählen Sie Ihre Table
3. Überprüfen Sie, ob alle Keys und Übersetzungen vorhanden sind

### Debug Logs

Aktivieren Sie Debug-Logs in `LocalizationHelper`:

```csharp
// Fügen Sie in LocalizationHelper.cs hinzu
#define LOCALIZATION_DEBUG

#if LOCALIZATION_DEBUG
Debug.Log($"Loading localized string: {tableName}.{key}");
#endif
```

## Legacy System Kompatibilität

Das alte `MultiLanguage` System bleibt voll funktionsfähig:

- `MultiLanguage.GetText(key)` funktioniert weiterhin
- Alte Sprachdateien in `Assets/Resources/Lang/` werden für Bracket-Keys verwendet
- `UseUnityLocalization` Flag erlaubt Umschaltung zwischen Systemen

Um nur das Legacy System zu verwenden:

```csharp
MultiLanguage.UseUnityLocalization = false;
```

## Troubleshooting

### Problem: Texte werden nicht übersetzt

**Lösung:**
1. Überprüfen Sie, ob `LocalizationHelper` GameObject in der Szene existiert
2. Überprüfen Sie, ob die String Tables korrekt erstellt wurden
3. Überprüfen Sie, ob die Keys genau übereinstimmen (case-sensitive)
4. Prüfen Sie die Console auf Warnungen

### Problem: Sprachwechsel funktioniert nicht

**Lösung:**
1. Überprüfen Sie, ob `LocalizationHelper.Instance` nicht null ist
2. Stellen Sie sicher, dass die Locales korrekt konfiguriert sind
3. Überprüfen Sie, ob `OnLanguageChanged` Events abonniert sind

### Problem: Bracket-Keys ([Wait], [Exit]) funktionieren nicht

**Lösung:**
1. Erstellen Sie die `Messages` String Table
2. Fügen Sie alle Bracket-Keys hinzu (mit den Brackets!)
3. Setzen Sie `useLegacyForBracketKeys = true` in `LocalizedTextComponent`

### Problem: Automatische Key-Ableitung funktioniert nicht

**Lösung:**
1. Überprüfen Sie die GameObject-Hierarchie
2. Setzen Sie den Key manuell in `LocalizedTextComponent`
3. Stellen Sie sicher, dass der GameObject-Name dem Key-Schema entspricht

## Best Practices

1. **Verwenden Sie konsistente Key-Namen:**
   - Folgen Sie dem Muster: `Category.Subcategory.Element.Property`
   - Beispiel: `Options.MenuPad.Menu1.resolution.Text`

2. **Dokumentieren Sie spezielle Keys:**
   - Bracket-Keys wie `[Wait]`, `[Exit]`
   - Width-Keys wie `<Menu1>`, `<Menu2>`

3. **Testen Sie alle Sprachen:**
   - Wechseln Sie zwischen allen Sprachen
   - Überprüfen Sie UI-Layout und Textüberlauf

4. **Verwenden Sie LocalizedTextComponent:**
   - Für automatische Updates
   - Einfacher zu warten als manuelle Text-Updates

5. **Backup der Legacy-Dateien:**
   - Behalten Sie `Assets/Resources/Lang/*.txt` als Referenz
   - Nützlich für schnelle Übersetzungsvergleiche

## Nächste Schritte

1. Installieren Sie das Unity Localization Package
2. Erstellen Sie die String Tables (UI und Messages)
3. Importieren Sie alle Übersetzungen
4. Fügen Sie `LocalizationHelper` GameObject zur Szene hinzu
5. Fügen Sie `LocalizedTextComponent` zu UI-Elementen hinzu
6. Testen Sie die Sprachwechsel-Funktionalität

Bei Fragen oder Problemen konsultieren Sie die [Unity Localization Package Dokumentation](https://docs.unity3d.com/Packages/com.unity.localization@latest/).
