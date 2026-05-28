# YACL.BetterEnums

Una librería para .NET que mejora el uso de `enum` mediante **Source Generators**, **Extension Methods** y **Roslyn Analyzers**.

El objetivo de la librería es eliminar tareas repetitivas, reducir errores comunes y ofrecer alternativas más performantes a varias operaciones tradicionales relacionadas con enums.

---

# ⚙️ Instalación

```bash
dotnet add package YACL.BetterEnums
```

---

# 🚀 ¿Qué ofrece esta librería?

YACL.BetterEnums genera automáticamente código optimizado para trabajar con enums de forma más simple, consistente y performante.

Entre las funcionalidades principales se encuentran:

- Conversión rápida de `string` a `enum`
- Conversión optimizada de `enum` a `string`
- Generación de diccionarios sin reflection
- Helpers para modelar ciclos de vida
- Roslyn Analyzers con Code Fix
- Código generado automáticamente mediante Source Generators
- Uso de `FrozenDictionary` y generación por `switch` para optimizar performance

La librería no requiere configuración especial para comenzar a utilizarse.

---

# 📋 Dependencias

Este proyecto no posee dependencias externas.

---

# 💡 Uso Básico

Todos los ejemplos de esta documentación utilizan el siguiente enum:

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

Tradicionalmente, convertir un `string` a un `enum` suele hacerse mediante `Enum.TryParse`.

Aunque funcional, esa aproximación implica validaciones y manipulaciones internas de strings que pueden impactar en performance cuando la operación ocurre de forma intensiva.

YACL.BetterEnums genera automáticamente métodos optimizados basados en `switch`.

## Métodos generados

```csharp
var value = "Processing";

var enumValue1 = value.ToWorkFlow();
var enumValue2 = WorkFlow.FromString(value);
```

---

## Compatibilidad de casing

La conversión contempla:

- Valor original
- Valor completamente en minúsculas
- Valor completamente en mayúsculas

Ejemplos válidos:

```text
Processing
processing
PROCESSING
```

Ejemplo no contemplado:

```text
pRoCeSsInG
```

---

## Comportamiento ante errores

En escenarios inválidos, la librería puede lanzar:

```csharp
ArgumentException
```

---

# 🔤 Enum to String

Usar `enum.ToString()` es una práctica común, pero implica allocations y procesamiento interno.

La librería genera automáticamente un método `ToStringV2()` basado en `switch`, evitando reflection y reduciendo allocations.

## Ejemplo

```csharp
var status = WorkFlow.Success;

var value1 = status.ToString();
var value2 = status.ToStringV2();
```

---

# 📦 Enum To Dictionary

Cuando se necesita exponer enums mediante DTOs o endpoints, es común utilizar `Enum.GetValues<T>()`.

Ese enfoque utiliza reflection y suele requerir caching manual.

YACL.BetterEnums genera automáticamente un método optimizado:

```csharp
var dictionary = WorkFlow.ToDictionary();
```

---

## ¿Cómo funciona internamente?

La implementación utiliza:

- `Lazy<T>`
- `FrozenDictionary`
- Inicialización diferida

Esto significa que:

- El diccionario no se crea si nunca se utiliza
- Solo se construye una vez
- La misma instancia se reutiliza en llamadas posteriores
- La implementación es thread-safe

---

# 🔁 Ciclo de Vida del Enum

En muchos sistemas, un enum representa estados dentro de un flujo.

La librería permite modelar esas transiciones mediante atributos y generar helpers automáticamente.

## Definición

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

## Métodos generados

- `NextStep()`
- `PreviousStep()`
- `ErrorStep()`

---

## Ejemplo

```csharp
var status = WorkFlow.Pending;

status = status.NextStep(); // Processing
status = status.NextStep(); // Success
status = status.NextStep(); // Success
```

---

## Comportamiento importante

> [!IMPORTANT]
> Las transiciones requieren obligatoriamente el uso de `EnumStepAttribute`.

> [!NOTE]
> Si un estado no tiene transición configurada, la librería devuelve el mismo valor actual y no arroja excepciones.

---

# ⚡ ¿Cómo logra mejorar la performance?

La librería evita varias operaciones tradicionales costosas:

| Operación tradicional | Alternativa generada |
|---|---|
| `Enum.TryParse()` | `switch` generado automáticamente |
| `enum.ToString()` | `switch` generado automáticamente |
| `Enum.GetValues<T>()` | `FrozenDictionary` cacheado |
| Reflection | Código generado en compilación |

---

# 🧠 Source Generator

La librería utiliza Roslyn Source Generators para generar Extension Methods automáticamente durante la compilación.

Esto permite:

- Evitar reflection en runtime
- Reducir allocations
- Mejorar performance
- Mantener una API simple para el desarrollador
- Eliminar código repetitivo manual

---

# 🔎 Analyzer Rules

> [!TIP]
> Hacé click sobre el código para ir directo a la documentación de cada regla.

| Código | Descripción |
|---|---|
| [YACLENUM001](#yaclenum001) | Los enums deben definir explícitamente sus valores numéricos |
| [YACLENUM002](#yaclenum002) | Las propiedades enum no deberían ser nullable |
| [YACLENUM003](#yaclenum003) | Todos los enums deberían tener `None = 0` |

---

# 🛠️ Code Fix

Los analyzers incluyen soporte para Code Fix automático.

La severidad puede configurarse desde `.editorconfig` según las necesidades del proyecto.

---

# 📖 Analyzer Documentation

## YACLENUM001

Los enums deben definir explícitamente sus valores numéricos.

Esto evita problemas cuando los enums son persistidos o compartidos con otros sistemas utilizando valores enteros.

### ❌ Inválido

```csharp
public enum MyFirstEnum
{
    Item1,
    Item2,
    Item3
}
```

### ✅ Válido

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

Las propiedades enum no deberían ser nullable.

La recomendación es utilizar `None = 0` como valor por defecto.

### ❌ Inválido

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

### ✅ Válido

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

Todos los enums deberían definir `None = 0`.

Esto permite representar un estado por defecto sin necesidad de utilizar valores nullable.

### ❌ Inválido

```csharp
public enum MyFirstEnum
{
    Item1 = 1,
    Item2 = 2,
    Item3 = 3
}
```

### ✅ Válido

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

# ⚠️ Consideraciones

## Enums privados

Los enums privados son ignorados por el Source Generator.

---

## Valores duplicados

La librería refuerza la política de `CA1069` para evitar valores duplicados dentro de enums.

---

## Flags Enums

Los enums marcados con `[Flags]` pueden convivir con la librería, aunque no poseen comportamiento especial adicional.

---

# 📊 Benchmarks

> [!TIP]
> Los benchmarks completos fueron movidos al final del documento para mejorar la lectura principal del README.

---

## String to Enum

| Método | Mean |
|---|---|
| Enum.TryParse | ~15-24 ns |
| Generated FromString | ~2 ns |
| Generated ToEnum | ~2 ns |

---

## Enum to String

| Método | Mean | Allocations |
|---|---|---|
| Traditional ToString | ~17-20 ns | 24 B |
| Generated ToStringV2 | ~1.6 ns | 0 B |

---

## Enum to Dictionary

| Method                    | Mean       | Allocated |
|-------------------------- |-----------:|----------:|
| Enum.GetValues            | 325.022 ns |     656 B |
| Generated ToDictionary    |   1.456 ns |         - |

---

# 📚 Benchmarks completos

## String to Enum

| Method                          | value        | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |------------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| **&#39;Original Enum.TryParse&#39;**        | **invalidValue** |        **NA** |        **NA** |        **NA** |        **NA** |     **?** |       **?** |        **NA** |           **?** |
| &#39;New &lt;enum&gt;.FromString(string)&#39; | invalidValue |        NA |        NA |        NA |        NA |     ? |       ? |        NA |           ? |
| &#39;New &quot;someText&quot;.To&lt;enum&gt;()&#39;     | invalidValue |        NA |        NA |        NA |        NA |     ? |       ? |        NA |           ? |
|                                 |              |           |           |           |           |       |         |           |             |
| **&#39;Original Enum.TryParse&#39;**        | **Opt1**         | **15.457 ns** | **0.3423 ns** | **0.5528 ns** | **15.248 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| &#39;New &lt;enum&gt;.FromString(string)&#39; | Opt1         |  2.034 ns | 0.0751 ns | 0.1146 ns |  1.975 ns |  0.13 |    0.01 |         - |          NA |
| &#39;New &quot;someText&quot;.To&lt;enum&gt;()&#39;     | Opt1         |  1.934 ns | 0.0219 ns | 0.0194 ns |  1.928 ns |  0.13 |    0.00 |         - |          NA |
|                                 |              |           |           |           |           |       |         |           |             |
| **&#39;Original Enum.TryParse&#39;**        | **opt2**         |        **NA** |        **NA** |        **NA** |        **NA** |     **?** |       **?** |        **NA** |           **?** |
| &#39;New &lt;enum&gt;.FromString(string)&#39; | opt2         |  2.373 ns | 0.0841 ns | 0.1334 ns |  2.294 ns |     ? |       ? |         - |           ? |
| &#39;New &quot;someText&quot;.To&lt;enum&gt;()&#39;     | opt2         |  2.078 ns | 0.0779 ns | 0.1280 ns |  2.082 ns |     ? |       ? |         - |           ? |
|                                 |              |           |           |           |           |       |         |           |             |
| **&#39;Original Enum.TryParse&#39;**        | **OPT3**         |        **NA** |        **NA** |        **NA** |        **NA** |     **?** |       **?** |        **NA** |           **?** |
| &#39;New &lt;enum&gt;.FromString(string)&#39; | OPT3         |  2.813 ns | 0.0914 ns | 0.1986 ns |  2.769 ns |     ? |       ? |         - |           ? |
| &#39;New &quot;someText&quot;.To&lt;enum&gt;()&#39;     | OPT3         |  2.730 ns | 0.0909 ns | 0.1493 ns |  2.655 ns |     ? |       ? |         - |           ? |
|                                 |              |           |           |           |           |       |         |           |             |
| **&#39;Original Enum.TryParse&#39;**        | **Opt4**         | **24.416 ns** | **0.5166 ns** | **0.4832 ns** | **24.144 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| &#39;New &lt;enum&gt;.FromString(string)&#39; | Opt4         |  1.971 ns | 0.0413 ns | 0.0366 ns |  1.961 ns |  0.08 |    0.00 |         - |          NA |
| &#39;New &quot;someText&quot;.To&lt;enum&gt;()&#39;     | Opt4         |  2.036 ns | 0.0739 ns | 0.1314 ns |  1.972 ns |  0.08 |    0.01 |         - |          NA |
|                                 |              |           |           |           |           |       |         |           |             |
| **&#39;Original Enum.TryParse&#39;**        | **opt5**         |        **NA** |        **NA** |        **NA** |        **NA** |     **?** |       **?** |        **NA** |           **?** |
| &#39;New &lt;enum&gt;.FromString(string)&#39; | opt5         |  2.388 ns | 0.0846 ns | 0.0940 ns |  2.385 ns |     ? |       ? |         - |           ? |
| &#39;New &quot;someText&quot;.To&lt;enum&gt;()&#39;     | opt5         |  2.255 ns | 0.0532 ns | 0.0472 ns |  2.230 ns |     ? |       ? |         - |           ? |
|                                 |              |           |           |           |           |       |         |           |             |
| **&#39;Original Enum.TryParse&#39;**        | **OPT6**         |        **NA** |        **NA** |        **NA** |        **NA** |     **?** |       **?** |        **NA** |           **?** |
| &#39;New &lt;enum&gt;.FromString(string)&#39; | OPT6         |  2.669 ns | 0.0822 ns | 0.1040 ns |  2.637 ns |     ? |       ? |         - |           ? |
| &#39;New &quot;someText&quot;.To&lt;enum&gt;()&#39;     | OPT6         |  2.559 ns | 0.0879 ns | 0.1673 ns |  2.522 ns |     ? |       ? |         - |           ? |


---

## Enum to String

| Method                                                  | value | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------------------- |------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **&#39;Traditional ToString() to a variable that is a enum&#39;**   | **Opt1**  | **17.461 ns** | **0.3208 ns** | **0.3001 ns** |  **1.00** |    **0.02** | **0.0057** |      **24 B** |        **1.00** |
| &#39;New ToStringV2() version to a variable that is a enum&#39; | Opt1  |  1.559 ns | 0.0550 ns | 0.0514 ns |  0.09 |    0.00 |      - |         - |        0.00 |
|                                                         |       |           |           |           |       |         |        |           |             |
| **&#39;Traditional ToString() to a variable that is a enum&#39;**   | **Opt2**  | **17.667 ns** | **0.4241 ns** | **0.4355 ns** |  **1.00** |    **0.03** | **0.0057** |      **24 B** |        **1.00** |
| &#39;New ToStringV2() version to a variable that is a enum&#39; | Opt2  |  1.644 ns | 0.0757 ns | 0.0708 ns |  0.09 |    0.00 |      - |         - |        0.00 |
|                                                         |       |           |           |           |       |         |        |           |             |
| **&#39;Traditional ToString() to a variable that is a enum&#39;**   | **Opt3**  | **18.363 ns** | **0.2316 ns** | **0.2053 ns** |  **1.00** |    **0.02** | **0.0057** |      **24 B** |        **1.00** |
| &#39;New ToStringV2() version to a variable that is a enum&#39; | Opt3  |  1.674 ns | 0.0466 ns | 0.0436 ns |  0.09 |    0.00 |      - |         - |        0.00 |
|                                                         |       |           |           |           |       |         |        |           |             |
| **&#39;Traditional ToString() to a variable that is a enum&#39;**   | **Opt4**  | **18.073 ns** | **0.2001 ns** | **0.1872 ns** |  **1.00** |    **0.01** | **0.0057** |      **24 B** |        **1.00** |
| &#39;New ToStringV2() version to a variable that is a enum&#39; | Opt4  |  1.664 ns | 0.0443 ns | 0.0370 ns |  0.09 |    0.00 |      - |         - |        0.00 |
|                                                         |       |           |           |           |       |         |        |           |             |
| **&#39;Traditional ToString() to a variable that is a enum&#39;**   | **Opt5**  | **19.975 ns** | **0.3892 ns** | **0.4633 ns** |  **1.00** |    **0.03** | **0.0057** |      **24 B** |        **1.00** |
| &#39;New ToStringV2() version to a variable that is a enum&#39; | Opt5  |  1.662 ns | 0.0115 ns | 0.0096 ns |  0.08 |    0.00 |      - |         - |        0.00 |
|                                                         |       |           |           |           |       |         |        |           |             |
| **&#39;Traditional ToString() to a variable that is a enum&#39;**   | **Opt6**  | **20.484 ns** | **0.4541 ns** | **0.4459 ns** |  **1.00** |    **0.03** | **0.0057** |      **24 B** |        **1.00** |
| &#39;New ToStringV2() version to a variable that is a enum&#39; | Opt6  |  1.666 ns | 0.0201 ns | 0.0157 ns |  0.08 |    0.00 |      - |         - |        0.00 |



## Enum To Dictionary

| Method                    | Mean       | Error     | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Original Enum.GetValues&#39; | 325.022 ns | 5.9226 ns | 11.4109 ns | 1.001 |    0.05 | 0.1569 |     656 B |        1.00 |
| &#39;New &lt;enum&gt;.ToDictionary&#39; |   1.456 ns | 0.1074 ns |  0.1358 ns | 0.004 |    0.00 |      - |         - |        0.00 |
