# VariableData Dynamic Registration API - Quick Reference

## Overview
The `VariableData` class now supports dynamic variable registration, enabling mods and plugins to extend the variable system without modifying core code.

## API Methods

### 1. RegisterVariable
Registers a new variable with optional aliases.

```csharp
public bool RegisterVariable(
    string name,              // Variable name
    VariableToken token,      // The variable token
    params string[] aliases   // Optional aliases
)
```

**Returns:** `true` if successful, `false` if name already exists

**Thread-Safety:** NOT thread-safe. Must be called during initialization only.

**Example:**
```csharp
var token = new Int1DVariableToken(VariableCode.GLOBAL, variableData);
bool success = variableData.RegisterVariable("CUSTOM_GOLD", token, "?????", "MOD_GOLD");
```

### 2. IsVariableRegistered
Checks if a variable name is already registered.

```csharp
public bool IsVariableRegistered(string name)
```

**Returns:** `true` if the variable exists, `false` otherwise

**Example:**
```csharp
if (!variableData.IsVariableRegistered("CUSTOM_GOLD"))
{
    // Register it
}
```

### 3. GetAllVariableNames
Gets all registered variable names (for debugging/introspection).

```csharp
public string[] GetAllVariableNames()
```

**Returns:** Array of all registered variable names

**Example:**
```csharp
string[] allVars = variableData.GetAllVariableNames();
foreach (var varName in allVars)
{
    Debug.Log($"Variable: {varName}");
}
```

## Use Cases

### Mod: Custom Currency System
```csharp
// Register custom currencies
var goldToken = new Int1DVariableToken(VariableCode.GLOBAL, variableData);
variableData.RegisterVariable("MOD_GEMS", goldToken, "??");

var silverToken = new Int1DVariableToken(VariableCode.GLOBAL, variableData);
variableData.RegisterVariable("MOD_CRYSTALS", silverToken, "??");
```

Now ERA scripts can use:
```
MOD_GEMS:0 = 100
??:0 += 50
```

### Plugin: Debug Variables
```csharp
if (Program.DebugMode)
{
    var debugToken = new Int1DVariableToken(VariableCode.GLOBAL, variableData);
    variableData.RegisterVariable("DEBUG_COUNTER", debugToken);
}
```

### Tool: Variable Inspector
```csharp
public void InspectVariables()
{
    string[] allVars = variableData.GetAllVariableNames();
    Console.WriteLine($"Total variables: {allVars.Length}");
    
    foreach (var name in allVars.OrderBy(n => n))
    {
        if (variableData.IsVariableRegistered(name))
            Console.WriteLine($"  - {name}");
    }
}
```

## Best Practices

### 1. Register During Initialization
```csharp
// ? Good: Register during game startup
public void OnGameInitialize()
{
    variableData.RegisterVariable("MY_VAR", myToken);
}

// ? Bad: Don't register during gameplay
public void OnGameUpdate()
{
    variableData.RegisterVariable("MY_VAR", myToken); // NOT thread-safe!
}
```

### 2. Check Before Registering
```csharp
// ? Good: Check first to avoid conflicts
if (!variableData.IsVariableRegistered("MY_VAR"))
{
    variableData.RegisterVariable("MY_VAR", myToken);
}

// ?? Risky: Might fail if another mod registered it
variableData.RegisterVariable("MY_VAR", myToken);
```

### 3. Use Namespaced Names
```csharp
// ? Good: Use mod prefix to avoid conflicts
variableData.RegisterVariable("MYMOD_GOLD", token);
variableData.RegisterVariable("MYMOD_LEVEL", token);

// ? Bad: Generic names might conflict
variableData.RegisterVariable("GOLD", token);
variableData.RegisterVariable("LEVEL", token);
```

### 4. Provide Localized Aliases
```csharp
// ? Good: Support multiple languages
variableData.RegisterVariable(
    "MYMOD_GOLD",
    token,
    "??MOD_?",     // Japanese
    "????_??"    // Chinese
);
```

## Case Sensitivity

The API respects `Config.ICVariable`:

```csharp
// If Config.ICVariable = true (case-insensitive):
variableData.RegisterVariable("MyVar", token);
// Can be accessed as: MYVAR, myvar, MyVar

// If Config.ICVariable = false (case-sensitive):
variableData.RegisterVariable("MyVar", token);
// Must be accessed exactly as: MyVar
```

## Error Handling

```csharp
try
{
    bool success = variableData.RegisterVariable(null, token);
    // Throws ArgumentNullException
}
catch (ArgumentNullException ex)
{
    // Handle error
}

// Check return value for name conflicts
if (!variableData.RegisterVariable("EXISTING_VAR", token))
{
    Debug.LogWarning("Variable already exists!");
}
```

## Limitations

1. **Not Thread-Safe**: Only call during initialization
2. **No Unregister**: Once registered, variables cannot be removed
3. **No Override**: Cannot replace existing variable registrations
4. **Memory**: Each variable consumes memory for its storage

## Advanced Example: Mod System

```csharp
public class CustomModSystem
{
    private VariableData variableData;
    
    public void Initialize(VariableData varData)
    {
        variableData = varData;
        RegisterModVariables();
    }
    
    private void RegisterModVariables()
    {
        // Custom stats
        RegisterIntArray("MOD_STAT_STRENGTH", "??");
        RegisterIntArray("MOD_STAT_AGILITY", "??");
        RegisterIntArray("MOD_STAT_MAGIC", "??");
        
        // Custom flags
        RegisterIntArray("MOD_FLAG_QUEST", "???????");
        RegisterIntArray("MOD_FLAG_EVENT", "???????");
        
        // Custom strings
        RegisterStrArray("MOD_TEXT_DIARY", "??");
    }
    
    private void RegisterIntArray(string name, params string[] aliases)
    {
        if (!variableData.IsVariableRegistered(name))
        {
            var token = new Int1DVariableToken(VariableCode.GLOBAL, variableData);
            variableData.RegisterVariable(name, token, aliases);
        }
    }
    
    private void RegisterStrArray(string name, params string[] aliases)
    {
        if (!variableData.IsVariableRegistered(name))
        {
            var token = new Str1DVariableToken(VariableCode.GLOBALS, variableData);
            variableData.RegisterVariable(name, token, aliases);
        }
    }
}
```

## Testing Your Registration

```csharp
[Test]
public void TestVariableRegistration()
{
    // Arrange
    var varData = new VariableData(gamebase, constant);
    var token = new Int1DVariableToken(VariableCode.GLOBAL, varData);
    
    // Act
    bool success = varData.RegisterVariable("TEST_VAR", token);
    
    // Assert
    Assert.IsTrue(success);
    Assert.IsTrue(varData.IsVariableRegistered("TEST_VAR"));
}

[Test]
public void TestDuplicateRegistration()
{
    // Arrange
    var varData = new VariableData(gamebase, constant);
    var token = new Int1DVariableToken(VariableCode.GLOBAL, varData);
    
    // Act
    varData.RegisterVariable("TEST_VAR", token);
    bool secondAttempt = varData.RegisterVariable("TEST_VAR", token);
    
    // Assert
    Assert.IsFalse(secondAttempt); // Should fail
}
```

## Conclusion

The dynamic registration API enables powerful extensibility while maintaining compatibility with existing ERA games. Use it to create mods, plugins, and custom game mechanics without modifying core code.
