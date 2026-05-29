# YACL.BetterEnums

[Documentacion en español](/docs/packages/YACL.BetterEnums_spa.md)

A .NET library that improves the way `enum` types are used through **Source Generators**, **Extension Methods**, and **Roslyn Analyzers**.

The goal of the library is to eliminate repetitive tasks, reduce common mistakes, and provide more performant alternatives for several traditional enum operations.

---

# ⚙️ Installation

```bash
dotnet add package YACL.BetterEnums
```

---

# 🚀 What does this library provide?

YACL.BetterEnums automatically generates optimized code to work with enums in a simpler, more consistent, and more performant way.

Main features include:

- Fast `string` to `enum` conversion
- Optimized `enum` to `string` conversion
- Reflection-free dictionary generation
- Lifecycle helpers for state transitions
- Roslyn Analyzers with Code Fix support
- Automatically generated code through Source Generators
- Usage of `FrozenDictionary` and generated `switch` statements for performance optimization

The library does not require any special configuration to start using it.

---

# 📋 Dependencies

This project does not rely on external package dependencies.

---

# 💡 Basic Usage

All examples in this documentation use the following enum:

```csharp
internal enum WorkFlow
{
    None = 0,
    Pending = 1,
    Processing = 2,
    Success = 3,
    Error = 4
}
```

---

# 🔄 String to Enum

Traditionally, converting a `string` to an `enum` is usually done using `Enum.TryParse`.

While functional, that approach performs internal string handling and validations that may impact performance in high-frequency scenarios.

YACL.BetterEnums automatically generates optimized methods based on `switch` statements.

## Generated methods

```csharp
var value = "Processing";

var enumValue1 = value.ToWorkFlow();
var enumValue2 = WorkFlow.FromString(value);
```

---

## Supported casing

The conversion supports:

- Original casing
- Fully lowercase values
- Fully uppercase values

Valid examples:

```text
Processing
processing
PROCESSING
```

Unsupported example:

```text
pRoCeSsInG
```

---

## Error behavior

In invalid scenarios, the library may throw:

```csharp
ArgumentException
```

---

# 🔤 Enum to String

Using `enum.ToString()` is common, but it involves allocations and internal processing.

The library automatically generates a `ToStringV2()` method based on `switch` statements, avoiding reflection and reducing allocations.

## Example

```csharp
var status = WorkFlow.Success;

var value1 = status.ToString();
var value2 = status.ToStringV2();
```

---

# 📦 Enum To Dictionary

When exposing enums through DTOs or endpoints, using `Enum.GetValues<T>()` is very common.

That approach relies on reflection and usually requires manual caching.

YACL.BetterEnums automatically generates an optimized method:

```csharp
var dictionary = WorkFlow.ToDictionary();
```

---

## Internal implementation

The implementation uses:

- `Lazy<T>`
- `FrozenDictionary`
- Deferred initialization

This means:

- The dictionary is never created if it is not used
- It is created only once
- The same instance is reused across calls
- The implementation is thread-safe

---

# 🔁 Enum Lifecycle

In many systems, enums represent states within a workflow.

The library allows modeling those transitions through attributes and automatically generated helpers.

To achieve this, the desired enum values must use the `EnumStep` attribute (it is not required on every value).

`EnumStep` receives the following optional parameters:

- Next enum value
- Previous enum value
- Error enum value

This configuration only defines helper behavior. The business logic that decides when transitions should happen is still the developer's responsibility.

## Definition

```csharp
internal enum WorkFlow
{
    [EnumStep(WorkFlow.Processing, null, WorkFlow.Error)]
    Pending = 1,

    [EnumStep(WorkFlow.Success, WorkFlow.Pending, WorkFlow.Error)]
    Processing = 2,

    Success = 3,
    Error = 4
}
```

---

## Generated methods

- `NextStep()`
- `PreviousStep()`
- `ErrorStep()`

---

## Example

```csharp
var status = WorkFlow.Pending;

status = status.NextStep(); // Processing
status = status.NextStep(); // Success
status = status.NextStep(); // Success
```

---

## Important behavior

> [!IMPORTANT]
> Transitions require the use of `EnumStepAttribute`.

> [!NOTE]
> If no transition is configured for a state, the library returns the current value and does not throw exceptions.

---

# ⚡ How does it improve performance?

The library avoids several traditionally expensive operations:

| Traditional operation | Generated alternative |
|---|---|
| `Enum.TryParse()` | Automatically generated `switch` |
| `enum.ToString()` | Automatically generated `switch` |
| `Enum.GetValues<T>()` | Cached `FrozenDictionary` |
| Reflection | Compile-time generated code |

---

# 🧠 Source Generator

The library uses Roslyn Source Generators to automatically generate Extension Methods during compilation.

This allows:

- Avoiding runtime reflection
- Reducing allocations
- Improving performance
- Keeping a simple developer experience
- Eliminating repetitive boilerplate code

---

# 🔎 Analyzer Rules

> [!TIP]
> Click the rule code to jump directly to its documentation.

| Code | Description |
|---|---|
| [YACLENUM001](#yaclenum001) | Enums should explicitly define numeric values |
| [YACLENUM002](#yaclenum002) | Enum properties should not be nullable |
| [YACLENUM003](#yaclenum003) | All enums should define `None = 0` |

---

# 🛠️ Code Fix

The analyzers include automatic Code Fix support.

Severity can be configured through `.editorconfig` depending on project needs.

---

# 📖 Analyzer Documentation

## YACLENUM001

Enums should explicitly define numeric values.

This prevents issues when enums are persisted or shared across systems using integer values.

### ❌ Invalid

```csharp
public enum MyFirstEnum
{
    Item1,
    Item2,
    Item3
}
```

### ✅ Valid

```csharp
public enum MyFirstEnum
{
    Item1 = 1,
    Item2 = 2,
    Item3 = 3
}
```

---

## YACLENUM002

Enum properties should not be nullable.

The recommendation is to use `None = 0` as the default value.

### ❌ Invalid

```csharp
public enum DocumentType
{
    None = 0,
    DNI = 1
}

public class Person
{
    public DocumentType? DocumentType { get; set; }
}
```

### ✅ Valid

```csharp
public enum DocumentType
{
    None = 0,
    DNI = 1
}

public class Person
{
    public DocumentType DocumentType { get; set; }
}
```

---

## YACLENUM003

All enums should define `None = 0`.

This allows representing a default state without relying on nullable values.

### ❌ Invalid

```csharp
public enum MyFirstEnum
{
    Item1 = 1,
    Item2 = 2,
    Item3 = 3
}
```

### ✅ Valid

```csharp
public enum MyFirstEnum
{
    None = 0,
    Item1 = 1,
    Item2 = 2,
    Item3 = 3
}
```

---

# ⚠️ Considerations

## Private enums

Private enums are ignored by the Source Generator.

---

## Duplicate values

The library reinforces the `CA1069` policy to prevent duplicated enum values.

---

## Flags enums

Enums marked with `[Flags]` can coexist with the library, although no additional special behavior is provided.

---

# 📊 Benchmarks

> [!TIP]
> Full benchmarks were moved to the end of the document to improve readability.

---

## String to Enum

| Method | Mean |
|---|---|
| Enum.TryParse | ~15-24 ns |
| Generated FromString | ~2 ns |
| Generated ToEnum | ~2 ns |

---

## Enum to String

| Method | Mean | Allocations |
|---|---|---|
| Traditional ToString | ~17-20 ns | 24 B |
| Generated ToStringV2 | ~1.6 ns | 0 B |

---

## Enum To Dictionary

| Method | Mean | Allocated |
|---|---|---|
| Enum.GetValues | 325.022 ns | 656 B |
| Generated ToDictionary | 1.456 ns | 0 B |

---

# 📚 Full Benchmarks

## String to Enum

```text
The original benchmark tables can be preserved in a dedicated benchmark section or separate document.
```

---

## Enum to String

```text
The original benchmark tables can be preserved in a dedicated benchmark section or separate document.
```

---

## Enum To Dictionary

```text
The original benchmark tables can be preserved in a dedicated benchmark section or separate document.
```
