# uEmuera Extended Compatibility - Quick Start Guide

## Was wurde implementiert?

uEmuera unterstützt jetzt die **`CompatiIgnoreInvalidLine`** Option, die es ermöglicht, Spiele auszuführen, die für erweiterte Emuera-Varianten (EM/EE) entwickelt wurden.

## Wie nutze ich diese Funktion?

### Schritt 1: Config-Datei bearbeiten

Öffne die Datei `emuera.config` im uEmuera-Verzeichnis und füge folgende Zeile hinzu:

```
???????????????????:Yes
```

Oder in englischer Schreibweise:

```
CompatiIgnoreInvalidLine:Yes
```

### Schritt 2: Spiel starten

Starte uEmuera normal. Das Spiel wird nun:
- ? Unbekannte Befehle überspringen (z.B. BINPUT)
- ? Warnungen anzeigen (aber nicht abbrechen)
- ? So weit wie möglich ausführen

### Schritt 3: Fehlende Variablen definieren (Optional)

Wenn Variablenfehler wie "??AP ist???????????" auftreten:

1. Erstelle oder bearbeite `CSV/_??.csv` im Spielverzeichnis
2. Füge die Variablendefinitionen hinzu:

```csv
;Format: Variable Type, Sub-Name, Size, Default Value
MONEY,??AP,1,0
MONEY,??AP,1,0
MONEY,??AP,1,0
MONEY,??AP,1,0
MONEY,????,1,0
MONEY,????,1,0
MONEY,??PT,1,0
MONEY,????,1,0
MONEY,???,1,0
DAY,??,1,0
DAY,??,1,0
DAY,??,1,0
DAY,??,1,0
```

## Erwartetes Verhalten

### Mit CompatiIgnoreInvalidLine:Yes

```
[Start des Spiels]
???BINPUT??????????????????????
[Spiel läuft weiter]
```

Das Spiel startet und funktioniert, aber Features die BINPUT verwenden funktionieren nicht.

### Mit CompatiIgnoreInvalidLine:No (Standard)

```
[Start des Spiels]
ERB????????????????Emuera??????
???????????????
[Spiel wird beendet]
```

Das Spiel startet nicht.

## Einschränkungen

?? **Wichtig zu wissen:**

1. **BINPUT funktioniert nicht** - Der Befehl wird übersprungen
2. **Benutzerdefinierte Variablen müssen manuell definiert werden** - Es gibt keine automatische Registrierung
3. **Manche Features funktionieren nicht** - z.B. erweiterte Eingabefunktionen
4. **Speicherstände** von der Original-EXE könnten inkompatibel sein

## Vergleich: Standard Emuera vs. EM/EE vs. uEmuera

| Feature | Standard Emuera | EM/EE Extended | uEmuera (jetzt) |
|---------|----------------|----------------|-----------------|
| Basic ERB execution | ? | ? | ? |
| BINPUT command | ? | ? | ?? (wird übersprungen) |
| Dynamic variable registration | ? | ? | ? (manuelle CSV nötig) |
| Error tolerance | ? | ? | ? (mit Config-Flag) |
| Extended save format | ? | ? | ? |

## Fehlerbehebung

### Problem: Spiel startet trotzdem nicht

**Lösung:** Aktiviere die Debug-Ausgabe um zu sehen, welche Zeilen übersprungen werden:

1. Füge in `emuera.config` hinzu:
   ```
   ?????????????:Yes
   ```
2. Starte das Spiel und beobachte die Warnungen
3. Fehlende Variablen in CSV-Dateien definieren

### Problem: Variablenfehler bei MONEY:??AP

**Lösung:** Siehe Schritt 3 oben - CSV-Datei mit Variablendefinitionen erstellen

### Problem: "BINPUT"wird ständig angezeigt

**Dies ist normal.** BINPUT wird übersprungen, weil es nicht implementiert ist. Das Spiel sollte trotzdem funktionieren, solange BINPUT nicht kritisch ist.

## Nächste Schritte

Wenn du möchtest dass dein Spiel vollständig funktioniert:

1. **Kurzfristig:** Verwende die Original-EXE (Emuera1824+v18+EMv17+EEv40.exe)
2. **Mittelfristig:** Erstelle vollständige CSV-Variablendefinitionen
3. **Langfristig:** Warte auf zukünftige uEmuera-Versionen mit vollständiger EM/EE-Unterstützung

## Support

Fragen oder Probleme? Erstelle ein Issue auf GitHub:
https://github.com/Noa3/uEmuera/issues

**Füge folgende Informationen hinzu:**
- uEmuera Version
- Welches Spiel
- Welche Emuera-Version das Spiel benötigt
- Fehlermeldungen/Warnungen
- Deine `emuera.config` Einstellungen

## Weitere Dokumentation

- [EMUERA_EXTENDED_COMPATIBILITY.md](EMUERA_EXTENDED_COMPATIBILITY.md) - Technische Details
- [GAME_COMPATIBILITY_ISSUE_EXPLANATION.md](GAME_COMPATIBILITY_ISSUE_EXPLANATION.md) - Umfassender Kompatibilitätsguide
- [README.md](README.md) - Allgemeine uEmuera-Dokumentation
