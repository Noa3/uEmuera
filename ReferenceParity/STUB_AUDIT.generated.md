# Generated stub audit

> Scanned `Assets/Scripts` and `Assets/Tests`. Review classification before fixing; not every `return false` is a defect.

| Classification | Count |
|---|---:|
| COMMENT_ONLY | 29 |
| PLAYER_REACHABLE | 9 |
| TEST_FIXTURE | 4 |

| File | Line | Kind | Classification | Evidence |
|---|---:|---|---|---|
| `Assets/Scripts/Emuera/Config/Config.cs` | 39 | `NotSupportedException` | `PLAYER_REACHABLE` | `catch (NotSupportedException)` |
| `Assets/Scripts/Emuera/GameData/Function/Creator.cs` | 58 | `TODO` | `COMMENT_ONLY` | `//TODO:1810` |
| `Assets/Scripts/Emuera/GameData/Function/Creator.Method.cs` | 418 | `TODO` | `COMMENT_ONLY` | `//TODO` |
| `Assets/Scripts/Emuera/GameData/Variable/VariableEvaluator.cs` | 1455 | `NYI` | `COMMENT_ONLY` | `//Problems accompanying the abolition of SP characters are handled by the caller` |
| `Assets/Scripts/Emuera/GameData/Variable/VariableEvaluator.cs` | 1465 | `NYI` | `COMMENT_ONLY` | `//Problems accompanying the abolition of SP characters are handled by the caller` |
| `Assets/Scripts/Emuera/GameData/Variable/VariableEvaluator.cs` | 1512 | `NYI` | `COMMENT_ONLY` | `//Problems accompanying the abolition of SP characters are handled by the caller` |
| `Assets/Scripts/Emuera/GameData/Variable/VariableToken.cs` | 506 | `TODO` | `COMMENT_ONLY` | `//TODO reference to const` |
| `Assets/Scripts/Emuera/GameProc/ErbLoader.cs` | 529 | `TODO` | `PLAYER_REACHABLE` | `if (symbol.Type == '[')//TODO:subNames maybe not implemented after all` |
| `Assets/Scripts/Emuera/GameProc/Function/FunctionArgType.cs` | 50 | `TODO` | `COMMENT_ONLY` | `//TODO: There are differences in processing when omitted but they should be able to be unified` |
| `Assets/Scripts/Emuera/GameProc/Process.CalledFunction.cs` | 181 | `TODO` | `COMMENT_ONLY` | `//TODO 1810alpha007 want to decide clearly whether to allow chara type. currently leaning to not allow` |
| `Assets/Scripts/Emuera/GameView/ConsoleDisplayLine.cs` | 113 | `TODO` | `COMMENT_ONLY` | `////TODO clear` |
| `Assets/Scripts/Emuera/GameView/EmueraConsole.cs` | 159 | `TODO` | `PLAYER_REACHABLE` | `redrawTimer.Enabled = false;//TODO:1824???????????????????` |
| `Assets/Scripts/Emuera/GameView/EmueraConsole.cs` | 1061 | `TODO` | `COMMENT_ONLY` | `//1819 TODO:?????(????TWAIT)???????????????????` |
| `Assets/Scripts/Emuera/GameView/EmueraConsole.cs` | 1074 | `TODO` | `COMMENT_ONLY` | `//TODO:?????????????????????????????????` |
| `Assets/Scripts/Emuera/GameView/EmueraConsole.cs` | 1726 | `Point.Empty` | `PLAYER_REACHABLE` | `return Point.Empty;` |
| `Assets/Scripts/Emuera/GameView/EmueraConsole.Print.cs` | 372 | `simplified implementation` | `COMMENT_ONLY` | `/// Simplified implementation: displays on each call (no deferred flush needed).` |
| `Assets/Scripts/Emuera/GameView/PrintStringBuffer.cs` | 269 | `NotImplementedException` | `COMMENT_ONLY` | `// instead of leaving a player-reachable NotImplementedException.` |
| `Assets/Scripts/Emuera/Program.cs` | 37 | `TODO` | `COMMENT_ONLY` | `TODO: 1819 Want to at least separate the MainWindow & Console input/display group from the Process & Data processing group` |
| `Assets/Scripts/Emuera/Sub/EraEncoding.cs` | 36 | `NotSupportedException` | `PLAYER_REACHABLE` | `catch (NotSupportedException) { return new UTF8Encoding(false, false); }` |
| `Assets/Scripts/Emuera/Sub/EraEncoding.cs` | 187 | `NotSupportedException` | `PLAYER_REACHABLE` | `catch (Exception ex) when (ex is ArgumentException \|\| ex is IOException \|\| ex is NotSupportedException)` |
| `Assets/Scripts/Emuera/Sub/LexicalAnalyzer.cs` | 116 | `TODO` | `COMMENT_ONLY` | `//		double d = Convert.ToDouble(numstr);` |
| `Assets/Scripts/Emuera/Sub/LexicalAnalyzer.cs` | 313 | `TODO` | `PLAYER_REACHABLE` | `return Convert.ToDouble(st.Substring(start, st.CurrentPosition - start));` |
| `Assets/Scripts/MathUtilities.cs` | 116 | `NotImplementedException` | `PLAYER_REACHABLE` | `throw new NotImplementedException("Binary search requires NativeArray-based line storage");` |
| `Assets/Scripts/SpriteManager.cs` | 688 | `TODO` | `COMMENT_ONLY` | `//Todo: ??????` |
| `Assets/Scripts/uEmuera/Forms.cs` | 170 | `TODO` | `COMMENT_ONLY` | `//todo` |
| `Assets/Scripts/uEmuera/Forms.cs` | 196 | `Point.Empty` | `PLAYER_REACHABLE` | `return mousePosition is Point point ? point : Point.Empty;` |
| `Assets/Scripts/uEmuera/Window.cs` | 17 | `NotImplementedException` | `COMMENT_ONLY` | `//throw new NotImplementedException();` |
| `Assets/Scripts/uEmuera/Window.cs` | 22 | `NotImplementedException` | `COMMENT_ONLY` | `//throw new NotImplementedException();` |
| `Assets/Scripts/uEmuera/Window.cs` | 27 | `NotImplementedException` | `COMMENT_ONLY` | `//throw new NotImplementedException();` |
| `Assets/Scripts/uEmuera/Window.cs` | 46 | `NotImplementedException` | `COMMENT_ONLY` | `//throw new NotImplementedException();` |
| `Assets/Scripts/uEmuera/Window.cs` | 51 | `NotImplementedException` | `COMMENT_ONLY` | `//throw new NotImplementedException();` |
| `Assets/Scripts/uEmuera/Window.cs` | 71 | `NotImplementedException` | `COMMENT_ONLY` | `//throw new NotImplementedException();` |
| `Assets/Scripts/uEmuera/Window.cs` | 76 | `NotImplementedException` | `COMMENT_ONLY` | `//throw new NotImplementedException();` |
| `Assets/Scripts/uEmuera/Window.cs` | 82 | `NotImplementedException` | `COMMENT_ONLY` | `//throw new NotImplementedException();` |
| `Assets/Scripts/uEmuera/Window.cs` | 88 | `NotImplementedException` | `COMMENT_ONLY` | `//throw new NotImplementedException();` |
| `Assets/Tests/EditMode/CompatibilityScannerTests.cs` | 140 | `TODO` | `COMMENT_ONLY` | `// TODO: Add fixture with CP932-encoded .ERB file to test encoding detection.` |
| `Assets/Tests/EditMode/DrawingPrimitivesTests.cs` | 36 | `Point.Empty` | `TEST_FIXTURE` | `Assert.AreEqual(0, Point.Empty.X);` |
| `Assets/Tests/EditMode/DrawingPrimitivesTests.cs` | 37 | `Point.Empty` | `TEST_FIXTURE` | `Assert.AreEqual(0, Point.Empty.Y);` |
| `Assets/Tests/EditMode/DrawingPrimitivesTests.cs` | 38 | `Point.Empty` | `TEST_FIXTURE` | `Assert.IsTrue(Point.Empty.IsEmpty);` |
| `Assets/Tests/EditMode/Phase3ConformanceTests.cs` | 146 | `TODO` | `COMMENT_ONLY` | `// static-analysis boundary here. Deeper integration is a TODO.` |
| `Assets/Tests/EditMode/Phase3ConformanceTests.cs` | 150 | `TODO` | `COMMENT_ONLY` | `// Div tag integration test is tracked as TODO.` |
| `Assets/Tests/EditMode/Phase3ConformanceTests.cs` | 215 | `NotSupportedException` | `TEST_FIXTURE` | `catch (NotSupportedException)` |
