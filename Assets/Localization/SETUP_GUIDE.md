# String Tables Import & Setup Anleitung

## Schritt 1: Unity Localization Package installieren

Falls noch nicht installiert:

1. Öffnen Sie Unity
2. Gehen Sie zu `Window > Package Manager`
3. Klicken Sie auf `+` ? `Add package by name...`
4. Geben Sie ein: `com.unity.localization`
5. Klicken Sie auf `Add`

## Schritt 2: Localization Settings erstellen

1. Gehen Sie zu `Edit > Project Settings > Localization`
2. Klicken Sie auf `Create` um die Localization Settings zu erstellen
3. Wählen Sie einen Ordner: `Assets/Localization`

## Schritt 3: Locales hinzufügen

1. Öffnen Sie `Window > Asset Management > Localization Tables`
2. Klicken Sie auf `Locale Generator`
3. Fügen Sie folgende Locales hinzu:
   - **Chinese (Simplified)** - Code: `zh-Hans`
   - **English** - Code: `en`
   - **Japanese** - Code: `ja`

## Schritt 4: String Table Collections erstellen

### UI String Table

1. Öffnen Sie `Window > Asset Management > Localization Tables`
2. Klicken Sie auf `New Table Collection`
3. Konfiguration:
   - **Name**: `UI`
   - **Type**: `String Table Collection`
   - **Save Location**: `Assets/Localization/StringTables`
4. Wählen Sie alle drei Locales:
   - ? Chinese (Simplified) - zh-Hans
   - ? English - en
   - ? Japanese - ja
5. Klicken Sie auf `Create`

### Messages String Table

1. Wiederholen Sie die Schritte oben
2. **Name**: `Messages`
3. Wählen Sie alle drei Locales
4. Klicken Sie auf `Create`

### System String Table

1. Wiederholen Sie die Schritte oben
2. **Name**: `System`
3. Wählen Sie alle drei Locales
4. Klicken Sie auf `Create`

## Schritt 5: CSV-Dateien importieren

### UI String Table importieren

1. Öffnen Sie `Window > Asset Management > Localization Tables`
2. Wählen Sie die `UI` String Table Collection
3. Klicken Sie auf das **Menü-Icon** (drei Punkte) ? `Import > CSV...`
4. Wählen Sie die Datei: `Assets/Localization/StringTables/UI_StringTable.csv`
5. Klicken Sie auf `Import`
6. Überprüfen Sie, dass alle 28 Einträge importiert wurden

### Messages String Table importieren

1. Wählen Sie die `Messages` String Table Collection
2. Klicken Sie auf das **Menü-Icon** ? `Import > CSV...`
3. Wählen Sie die Datei: `Assets/Localization/StringTables/Messages_StringTable.csv`
4. Klicken Sie auf `Import`
5. Überprüfen Sie, dass alle 13 Einträge importiert wurden

### System String Table importieren

1. Wählen Sie die `System` String Table Collection
2. Klicken Sie auf das **Menü-Icon** ? `Import > CSV...`
3. Wählen Sie die Datei: `Assets/Localization/StringTables/System_StringTable.csv`
4. Klicken Sie auf `Import`
5. Überprüfen Sie, dass alle 17 Einträge importiert wurden

## Schritt 6: LocalizationHelper GameObject zur Szene hinzufügen

1. Öffnen Sie Ihre Hauptszene
2. Erstellen Sie ein neues leeres GameObject (`GameObject > Create Empty`)
3. Benennen Sie es `LocalizationManager`
4. Fügen Sie die `LocalizationHelper` Komponente hinzu:
   - Klicken Sie auf `Add Component`
   - Suchen Sie nach `LocalizationHelper`
   - Wählen Sie die Komponente aus

Das GameObject wird automatisch mit `DontDestroyOnLoad` markiert.

## Schritt 7: LocalizedTextComponent zu GameObjects hinzufügen

### Automatisch (Empfohlen)

1. Gehen Sie zu `Tools > Localization > Setup GameObjects`
2. Ein Fenster öffnet sich mit drei Optionen:
   - **Add LocalizedTextComponent to FirstWindow** - Konfiguriert FirstWindow.prefab
   - **Add LocalizedTextComponent to Options** - Konfiguriert Options.prefab
   - **Setup All Prefabs** - Konfiguriert beide Prefabs auf einmal

3. Klicken Sie auf `Setup All Prefabs`

4. Überprüfen Sie die Console auf Erfolgsmeldungen:
   ```
   Added LocalizedTextComponent to titlebar with key: FirstWindow.Titlebar.title
   FirstWindow localization setup completed!
   Added LocalizedTextComponent to resolution with key: Options.MenuPad.Menu1.resolution.Text
   ... (weitere Meldungen)
   Options localization setup completed!
   ```

### Manuell (Falls automatisch nicht funktioniert)

Falls das automatische Setup nicht funktioniert, können Sie die Komponenten manuell hinzufügen:

#### FirstWindow.prefab

1. Öffnen Sie `Assets/Resources/Prefab/FirstWindow.prefab`
2. Finden Sie das GameObject `titlebar`
3. Fügen Sie `LocalizedTextComponent` hinzu:
   - **Table Name**: `UI`
   - **Key**: `FirstWindow.Titlebar.title`
   - **Use Legacy For Bracket Keys**: ? (nicht aktiviert)

#### Options.prefab

1. Öffnen Sie `Assets/Resources/Prefab/Options.prefab`
2. Für jedes Text-GameObject:

**Menu 1:**
- `MenuPad/Menu1/resolution` ? Key: `Options.MenuPad.Menu1.resolution.Text`
- `MenuPad/Menu1/language` ? Key: `Options.MenuPad.Menu1.language.Text`
- `MenuPad/Menu1/github` ? Key: `Options.MenuPad.Menu1.github.Text`
- `MenuPad/Menu1/exit` ? Key: `Options.MenuPad.Menu1.exit.Text`

**Menu 2:**
- `MenuPad/Menu2/back` ? Key: `Options.MenuPad.Menu2.back.Text`
- `MenuPad/Menu2/intent` ? Key: `Options.MenuPad.Menu2.intent.Text`
- `MenuPad/Menu2/savelog` ? Key: `Options.MenuPad.Menu2.savelog.Text`
- `MenuPad/Menu2/gototitle` ? Key: `Options.MenuPad.Menu2.gototitle.Text`
- `MenuPad/Menu2/restart` ? Key: `Options.MenuPad.Menu2.restart.Text`
- `MenuPad/Menu2/exit` ? Key: `Options.MenuPad.Menu2.exit.Text`

**Resolution Pad:**
- `resolution_pad/1080p` ? Key: `Options.Resolution.Pad.1080p.Text`
- `resolution_pad/900p` ? Key: `Options.Resolution.Pad.900p.Text`
- `resolution_pad/720p` ? Key: `Options.Resolution.Pad.720p.Text`
- `resolution_pad/540p` ? Key: `Options.Resolution.Pad.540p.Text`

**Language Box:**
- `language_box/border/titlebar/title` ? Key: `Options.LanguageBox.border.titlebar.title`
- `language_box/border/zh_cn/Text` ? Key: `Options.LanguageBox.border.zh_cn.Text`
- `language_box/border/jp/Text` ? Key: `Options.LanguageBox.border.jp.Text`
- `language_box/border/en_us/Text` ? Key: `Options.LanguageBox.border.en_us.Text`

**Intent Box:**
- `intentbox/border/titlebar/title` ? Key: `Options.IntentBox.border.titlebar.title`
- `intentbox/border/L_left/Text` ? Key: `Options.IntentBox.border.L_left.Text`
- `intentbox/border/L_right/Text` ? Key: `Options.IntentBox.border.L_right.Text`
- `intentbox/border/R_left/Text` ? Key: `Options.IntentBox.border.R_left.Text`
- `intentbox/border/R_right/Text` ? Key: `Options.IntentBox.border.R_right.Text`
- `intentbox/border/close/Text` ? Key: `Options.IntentBox.border.close.Text`
- `intentbox/border/reset/Text` ? Key: `Options.IntentBox.border.reset.Text`

## Schritt 8: Testen

1. Öffnen Sie die Play-Mode in Unity
2. Die Anwendung sollte in der Standard-Sprache (Chinesisch) starten
3. Öffnen Sie das Options-Menü
4. Wählen Sie `????` (Language)
5. Wechseln Sie die Sprache zu:
   - **????** (Chinesisch)
   - **???** (Japanisch)
   - **English** (Englisch)
6. Überprüfen Sie, dass alle Texte korrekt übersetzt werden

## Schritt 9: Localization Settings konfigurieren

1. Gehen Sie zu `Edit > Project Settings > Localization`
2. Unter **Locale Selectors** fügen Sie hinzu:
   - `Player Pref Locale Selector` (falls noch nicht vorhanden)
3. Stellen Sie sicher, dass die **Default Locale** auf `Chinese (Simplified)` gesetzt ist

## Troubleshooting

### Problem: CSV-Import funktioniert nicht

**Lösung:**
1. Überprüfen Sie, dass die CSV-Dateien UTF-8 kodiert sind
2. Öffnen Sie die CSV-Datei in einem Texteditor und überprüfen Sie das Format
3. Stellen Sie sicher, dass keine leeren Zeilen am Ende der Datei sind

### Problem: Texte werden nicht übersetzt

**Lösung:**
1. Überprüfen Sie, ob alle String Tables korrekt importiert wurden
2. Öffnen Sie `Window > Asset Management > Localization Tables`
3. Wählen Sie jede Table und überprüfen Sie, ob alle Keys vorhanden sind
4. Überprüfen Sie die Console auf Fehlermeldungen

### Problem: LocalizationHelper nicht gefunden

**Lösung:**
1. Stellen Sie sicher, dass `LocalizationHelper.cs` in `Assets/Scripts/` liegt
2. Warten Sie, bis Unity das Script kompiliert hat
3. Überprüfen Sie die Console auf Compilation-Fehler

### Problem: Setup Helper findet GameObjects nicht

**Lösung:**
1. Öffnen Sie das Prefab manuell
2. Überprüfen Sie die Namen der GameObjects
3. Passen Sie die Pfade im `LocalizationSetupHelper.cs` an, falls nötig
4. Führen Sie das Setup erneut aus

## Zusammenfassung der erstellten Dateien

? `Assets/Localization/StringTables/UI_StringTable.csv` - 28 Einträge
? `Assets/Localization/StringTables/Messages_StringTable.csv` - 13 Einträge
? `Assets/Localization/StringTables/System_StringTable.csv` - 17 Einträge
? `Assets/Editor/LocalizationSetupHelper.cs` - Automatisches Setup-Tool

## Nächste Schritte

Nach dem erfolgreichen Setup können Sie:

1. **Neue Übersetzungen hinzufügen:**
   - Öffnen Sie die String Table in Unity
   - Fügen Sie neue Keys hinzu
   - Exportieren Sie als CSV für Backup

2. **Weitere UI-Elemente lokalisieren:**
   - Fügen Sie `LocalizedTextComponent` zu weiteren GameObjects hinzu
   - Erstellen Sie neue Keys in den String Tables

3. **Sprachen hinzufügen:**
   - Erstellen Sie neue Locales
   - Fügen Sie Übersetzungen zu den String Tables hinzu

4. **Legacy-System deaktivieren (Optional):**
   - Sobald alles funktioniert, können Sie `MultiLanguage.UseUnityLocalization = true` dauerhaft setzen
   - Die alten Sprachdateien in `Assets/Resources/Lang/` können als Backup behalten werden

Bei weiteren Fragen konsultieren Sie die [Unity Localization Package Dokumentation](https://docs.unity3d.com/Packages/com.unity.localization@latest/).
