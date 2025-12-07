# Migration Guide: Unity 6.3 Refactoring

This guide helps developers understand the changes made in the Unity 6.3 refactoring and how to work with the new codebase.

## Package Changes

### New Dependencies
The following packages have been added:
- `com.unity.mathematics` (1.3.4)
- `com.unity.burst` (1.8.21)
- `com.unity.jobs` (0.51.1)

These are automatically installed when you open the project in Unity 6.

### Assembly Definition Updates
`uEmuera.Scripts.asmdef` now references:
- `Unity.Mathematics`
- `Unity.Collections`
- `Unity.Burst`
- `Unity.Jobs`

## Breaking Changes

### Color Struct
The `Color` struct internal representation has changed:

**Before:**
```csharp
public readonly struct Color {
    public readonly float r, g, b, a;
}
```

**After:**
```csharp
public readonly struct Color {
    private readonly Unity.Mathematics.float4 rgba;
    public float r => rgba.x;
    public float g => rgba.y;
    public float b => rgba.z;
    public float a => rgba.w;
}
```

**Migration**: Public API remains the same. No code changes needed unless you were using reflection or unsafe code to access internal fields.

### Vector Types in EmueraContent
Several internal fields changed from `Vector2` to `float2`:

**Before:**
```csharp
Vector2 local_position;
Vector2 drag_begin_position;
Vector2 drag_curr_position;
Vector2 drag_delta;
```

**After:**
```csharp
float2 local_position;
float2 drag_begin_position;
float2 drag_curr_position;
float2 drag_delta;
```

**Migration**: These are internal fields. External code should not be affected.

### Math Operations
Several math operations now use `Unity.Mathematics` instead of `Mathf`:

**Before:**
```csharp
float max = Mathf.Max(a, b);
float clamped = Mathf.Clamp(value, min, max);
```

**After:**
```csharp
float max = math.max(a, b);
float clamped = math.clamp(value, min, max);
```

**Migration**: 
- Add `using Unity.Mathematics;` to files using math operations
- Replace `Mathf` calls with `math` calls (lowercase)
- Note: `math` functions are often overloaded for `float2`, `float3`, `float4`

## New Features

### MathUtilities Class
New static class with Burst-compiled helper functions:

```csharp
using uEmuera;

// Apply drag damping
float2 velocity = MathUtilities.ApplyDragDamping(velocity, dragAmount, deltaTime);

// Limit position within bounds
float2 position = MathUtilities.LimitPosition(position, displaySize, contentSize, offset);

// Distance calculation
float distance = MathUtilities.Distance(pointA, pointB);

// Clamp float2
float2 clamped = MathUtilities.Clamp(value, min, max);

// Lerp float2
float2 interpolated = MathUtilities.Lerp(start, end, t);
```

### Burst-Compiled Jobs
New job types for batch operations in `uEmuera.Jobs` namespace:

#### Color Conversion Job
```csharp
using Unity.Collections;
using uEmuera.Jobs;

var inputColors = new NativeArray<int>(1000, Allocator.TempJob);
var outputColors = new NativeArray<float4>(1000, Allocator.TempJob);

var job = new ColorConversionJob {
    inputArgb = inputColors,
    outputRgba = outputColors
};

JobHandle handle = job.Schedule(inputColors.Length, 64);
handle.Complete();

// Use outputColors...

inputColors.Dispose();
outputColors.Dispose();
```

#### Line Position Job
```csharp
var job = new LinePositionJob {
    linePositionsY = positions,
    lineHeights = heights,
    scrollOffset = new float2(scrollX, scrollY),
    displayHeight = displayHeight,
    visibilityFlags = flags
};

JobHandle handle = job.Schedule(count, 64);
handle.Complete();

// Check visibility: flags[i] == 1 means visible
```

### Modern Data Structures
New immutable record structs in `uEmuera.Data` namespace:

#### DisplayConfig
```csharp
using uEmuera.Data;

var config = new DisplayConfig {
    Width = 1920,
    Height = 1080,
    ContentWidth = 2000,
    ContentHeight = 5000,
    OffsetHeight = 100
};

// Access computed properties
float2 size = config.DisplaySize;
bool needsScroll = config.RequiresVerticalScroll;
```

#### DragState
```csharp
// Create initial state
var dragState = DragState.Inactive;

// Update immutably
dragState = dragState with { 
    BeginPosition = startPos,
    IsActive = true 
};

// Update position
dragState = dragState.WithPosition(newPos);

// Update delta
dragState = dragState.WithDelta(newDelta);

// Access properties
float2 distance = dragState.DragDistance;
float magnitude = dragState.DragMagnitude;
```

#### ViewportBounds
```csharp
var bounds = new ViewportBounds {
    Top = 0,
    Bottom = 1080,
    Left = 0,
    Right = 1920
};

// Check containment
bool contains = bounds.Contains(new float2(100, 200));

// Check intersection
bool intersects = bounds.Intersects(otherBounds);

// Access properties
float2 center = bounds.Center;
float width = bounds.Width;
```

## Best Practices

### Using Unity.Mathematics

#### DO:
✅ Use `float2`, `float3`, `float4` for vectors
✅ Use `math.*` functions for calculations
✅ Use `quaternion` instead of `Quaternion` for calculations
✅ Convert to Unity types only for API calls

```csharp
// Good: Use float2 for calculations
float2 pos = new float2(x, y);
pos = math.clamp(pos, min, max);
transform.position = new Vector3(pos.x, pos.y, 0);
```

#### DON'T:
❌ Don't mix Vector2 and float2 unnecessarily
❌ Don't use Mathf for new code
❌ Don't forget to include `using Unity.Mathematics;`

```csharp
// Bad: Mixing types
Vector2 pos = new Vector2(x, y);
float2 pos2 = new float2(pos.x, pos.y); // Unnecessary conversion
```

### Using Burst Jobs

#### DO:
✅ Use jobs for batch operations (100+ items)
✅ Schedule jobs early, complete late
✅ Use appropriate batch sizes (32-64 typical)
✅ Dispose NativeArrays after use

```csharp
// Good: Schedule early
var job = CreateJob();
JobHandle handle = job.Schedule(count, 64);

// Do other work...

// Complete when needed
handle.Complete();
job.inputData.Dispose();
job.outputData.Dispose();
```

#### DON'T:
❌ Don't use jobs for small datasets (<50 items)
❌ Don't forget to dispose NativeCollections
❌ Don't access managed objects in jobs
❌ Don't use jobs for one-off operations

```csharp
// Bad: Job overhead exceeds benefit
var job = CreateJob();
job.Schedule(10, 64).Complete(); // Only 10 items!
```

### Using Record Structs

#### DO:
✅ Use for immutable data
✅ Use `with` expressions for updates
✅ Leverage computed properties
✅ Use `init` for initialization

```csharp
// Good: Immutable updates
var newState = oldState with { 
    Position = newPos,
    Velocity = newVel 
};
```

#### DON'T:
❌ Don't make record structs with many large fields
❌ Don't use mutable properties
❌ Don't use for frequently mutated data

```csharp
// Bad: Mutable record struct
public record struct MutableState {
    public float2 Position { get; set; } // Should be init
}
```

## Performance Tips

### Memory Management
1. **Reuse NativeArrays**: Don't allocate every frame
2. **Use Persistent allocators**: For long-lived data
3. **Batch operations**: Group operations to reduce overhead

### Job Scheduling
1. **Pipeline jobs**: Schedule dependent jobs in sequence
2. **Batch size**: 32-64 is optimal for most cases
3. **Complete late**: Do other work while job runs

### Math Operations
1. **Use SIMD types**: `float2`, `float3`, `float4` enable vectorization
2. **Minimize conversions**: Stay in `math` types as long as possible
3. **Inline simple operations**: Compiler can optimize better

## Testing

### Running Tests
```bash
# From Unity Editor
Window > General > Test Runner > Run All

# From command line
Unity -runTests -testPlatform EditMode -testResults results.xml
```

### Writing Tests
```csharp
using NUnit.Framework;
using Unity.Mathematics;

[TestFixture]
public class MyTests
{
    [Test]
    public void Math_Clamp_Works()
    {
        float2 value = new float2(150, 150);
        float2 min = new float2(0, 0);
        float2 max = new float2(100, 100);
        
        var result = math.clamp(value, min, max);
        
        Assert.AreEqual(max, result);
    }
}
```

## Troubleshooting

### Common Issues

#### Issue: "Type or namespace 'Mathematics' could not be found"
**Solution**: 
1. Check that `com.unity.mathematics` is in `Packages/manifest.json`
2. Add to assembly definition references
3. Add `using Unity.Mathematics;` to your file

#### Issue: "Burst compilation failed"
**Solution**:
1. Check for managed references in job structs
2. Ensure all types are blittable
3. Use `[BurstCompile]` with appropriate flags
4. Check Burst menu: Jobs > Burst > Enable Compilation

#### Issue: "NativeArray has not been disposed"
**Solution**:
1. Always dispose NativeCollections after use
2. Use try-finally blocks
3. Consider using `using` statements (C# 8.0+)

```csharp
// Proper disposal
var array = new NativeArray<float>(100, Allocator.TempJob);
try {
    // Use array...
}
finally {
    array.Dispose();
}
```

#### Issue: "Performance worse after refactoring"
**Solution**:
1. Enable Burst compilation: Jobs > Burst > Enable Compilation
2. Use Release build configuration
3. Enable IL2CPP backend
4. Check profiler for actual bottlenecks
5. Ensure batch sizes are appropriate

## Support

For issues or questions:
1. Check [Performance Optimizations](PERFORMANCE_OPTIMIZATIONS.md) documentation
2. Review [Unity Mathematics Manual](https://docs.unity3d.com/Packages/com.unity.mathematics@latest)
3. See [Burst Manual](https://docs.unity3d.com/Packages/com.unity.burst@latest)
4. Create an issue on GitHub

## Next Steps

1. **Profile your code**: Use Unity Profiler to identify bottlenecks
2. **Measure improvements**: Compare before/after performance
3. **Extend optimizations**: Apply patterns to more areas
4. **Stay updated**: Watch for Unity package updates
