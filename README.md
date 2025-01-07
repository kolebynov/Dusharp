# Dusharp

[![NuGet](https://img.shields.io/nuget/v/Dusharp)](https://www.nuget.org/packages/Dusharp/)

**Dusharp** is a C# source generator library for creating **discriminated unions**. This library allows you to define union types with ease, using attributes and partial methods. It is inspired by functional languages but built for C# developers.

## Features

- ✅ **Create unions**: Define discriminated unions using attributes.
- ✅ **Match method**: Pattern match on union cases in a type-safe way.
- ✅ **Equality**: Automatic equality comparison for unions.
- ✅ **Generics**: Generics support for union types.
- ✅ **Pretty print**: Using overloaded `ToString()`.
- ✅ **Struct unions**: With efficient memory layout for unions as structs.
- ✅ **JSON serialization/deserialization**: Support for unions with `System.Text.Json`.

## Installation

Dusharp is available as a NuGet package. You can install it using the NuGet package manager:

```bash
dotnet add package Dusharp
```

## Usage

`Dusharp` uses attributes to generate discriminated unions and case methods. Here's how to get started:

### 1. Define a Union
To define a union, annotate a class with the `[Dusharp.UnionAttribute]` attribute.  `[Dusharp.Json.GenerateJsonConverterAttribute]` generates JSON converter for the union (will explain below).

```csharp
using Dusharp;
using Dusharp.Json;

[Union]
[GenerateJsonConverter]
public partial class Shape<T>
    where T : struct, INumber<T>
{
}
```

### 2. Define Union Cases
Define union cases by creating public static partial methods and marking them with the `[Dusharp.UnionCaseAttribute]` attribute. The method body will be automatically generated.

```csharp
using Dusharp;
using Dusharp.Json;

[Union]
[GenerateJsonConverter]
public partial class Shape<T>
    where T : struct, INumber<T>
{
    [UnionCase]
    public static partial Shape<T> Point();

    [UnionCase]
    public static partial Shape<T> Circle(T radius);

    [UnionCase]
    public static partial Shape<T> Rectangle(T width, T height);
}
```

### 3. Match on Union
You can easily perform pattern matching on a union using the `Match` method. The source generator will create the `Match` method based on the defined union cases.

```csharp
Shape<double> shape = Shape<double>.Circle(5.0);

string result = shape.Match(
    () => "Point",
    radius => $"Circle with radius {radius}",
    (width, height) => $"Rectangle with width {width} and height {height}");

Console.WriteLine(result); // Output: Circle with radius 5.0
```

### 4. Compare Unions
Union cases can be compared for equality using the auto-generated equality methods. This allows for checking if two unions are the same.

```csharp
Shape<double> shape1 = Shape<double>.Circle(5.0);
Shape<double> shape2 = Shape<double>.Circle(5.0);

Console.WriteLine(shape1.Equals(shape2)); // True
Console.WriteLine(shape1 == shape2); // True
```

## Struct unions
`Dusharp` supports struct unions, allowing you to reduce allocations. You can define struct union the same way as for class union using the `[Dusharp.UnionAttribute]` attribute. This feature generates memory efficient unions.

### Blittable Types
Blittable types (e.g., `int`, `double`, etc., and structs contain only blittable types) from different cases will share the same memory space using the `[StructLayout(LayoutKind.Explicit)]` attribute. This enables efficient memory usage by overlapping the fields in the union.

### Reference Types
For reference type parameters, `Dusharp` uses a shared `object` fields to store reference type parameters from different cases. The `object` fields will be cast to their target types using the no-op `Unsafe.As` method, providing an efficient way to handle reference types in struct unions.

### Example
For instance, consider a union that contains both blittable and reference type parameters:

```csharp
[Union]
public partial struct TestUnion
{
    [UnionCase]
    public static partial TestUnion Case1(long value1, long value2);

    [UnionCase]
    public static partial TestUnion Case2(Guid value1, Guid value2);

    [UnionCase]
    public static partial TestUnion Case3(string value, Exception value2);

    [UnionCase]
    public static partial TestUnion Case4(Action value);
}
```

#### Generated Code Explanation
The source generator produces efficient code for this union by optimizing how blittable and reference types are stored and managed in memory. Here's the structure of the generated code:

```csharp
partial struct TestUnion : System.IEquatable<TestUnion>
{
    private object Field0;
    private object Field1;
    private TestUnionBlittableData UnionBlittableDataField;
    private byte Index;
}
```

- `Field0` and `Field1` are used to store reference type parameters (e.g., `string`, `Exception`, `Action`). Reference types share the same object references in memory.
- `UnionBlittableDataField` is an instance of `TestUnionBlittableData`, where the blittable data (e.g., `long`, `Guid`) is stored.
- `Index` tracks the active case, allowing the `Match` and equality methods to know which union case is currently stored.

#### Memory Layout Optimization for Blittable Types
For blittable types, the generator uses a memory-efficient layout where the fields of different union cases are overlapped in memory using the `[StructLayout(LayoutKind.Explicit)]` attribute. This reduces memory usage by sharing memory space for compatible types.

```csharp
[StructLayout(LayoutKind.Explicit)]
internal struct TestUnionBlittableData
{
    [FieldOffset(0)]
    public Case1BlittableData Case1Data;

    [FieldOffset(0)]
    public Case2BlittableData Case2Data;

    [StructLayout(LayoutKind.Auto)]
    public struct Case1BlittableData
    {
        public long value1;
        public long value2;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct Case2BlittableData
    {
        public System.Guid value1;
        public System.Guid value2;
    }
}
```

- `TestUnionBlittableData` contains the blittable data for the union cases that involve `long` and `Guid` types.
- `FieldOffset(0)` ensures that the memory space for `Case1Data` and `Case2Data` is shared, meaning both cases will occupy the same memory region. This is a key feature that allows for efficient memory usage when dealing with blittable types.

#### Size of a union
In this example, the size of the `TestUnion` union is **56 bytes**:

- 2 `object` fields: **16 bytes** (each object reference is **8 bytes** on a 64-bit system)
- `TestUnionBlittableData`: **32 bytes** (the size of the largest blittable case, which contains 2 `Guid` parameters, each being **16 bytes**)
- `Index` field: **1 byte** + padding for alignment, which totals **8 bytes**

Thus, the total size is `16 + 32 + 8 = 56 bytes`.

#### Important Note
All of these details about memory layout and struct size are implementation-specific and subject to change. Users should not rely on these internal details or use them directly in their code. The behavior and memory management may evolve in future versions to improve performance or efficiency.

## Unions serialization/deserialization
`Dusharp` supports serialization and deserialization of unions using either a default union JSON converter or a source-generated JSON converter specific to the union type.

To generate a specific JSON converter, the union type must be marked with the `[Dusharp.Json.GenerateJsonConverterAttribute]` attribute.
The source-generated converter is slightly faster and avoids boxing/unboxing struct unions during serialization and deserialization.
- **Parameterless Union Cases:** Serialized as a string containing only the case name.
- **Union Cases with Parameters:** Serialized as an object where the case name is the key, followed by an object containing the case parameters.

```csharp
using System.Text.Json;

Shape<double> shape1 = Shape<double>.Point();
Shape<double> shape2 = Shape<double>.Circle(5.0);
Shape<double> shape3 = Shape<double>.Rectangle(2.0, 2.0);

JsonSerializerOptions options = new JsonSerializerOptions
{
    Converters =
    {
        // Default generic JSON converter can convert any union type.
        new DefaultUnionJsonConverter(),
        // Or
        // Specific source-generated JSON converter for the Shape<double> union.
        new Shape<double>.JsonConverter(),
    },
};

string serializedShape3 = JsonSerializer.Serialize(shape3, options);

Console.WriteLine(JsonSerializer.Serialize(shape1, options)); // "Point"
Console.WriteLine(JsonSerializer.Serialize(shape2, options)); // {"Circle":{"radius": 5.0}}
Console.WriteLine(serializedShape3); // {"Rectangle":{"width": 2.0,"height": 2.0}}

Shape<double> deserializedShape = JsonSerializer.Deserialize<Shape<double>>(serializedShape3, options);
Console.WriteLine(deserializedShape.IsRectangle); // True
```

### Json converters benchmark results

```

BenchmarkDotNet v0.14.0, Fedora Linux 41 (KDE Plasma)
AMD Ryzen 7 5800H with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  Job-GFQIYP : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2

Platform=X64  Runtime=.NET 9.0  MaxIterationCount=8
MaxWarmupIterationCount=7  MinIterationCount=2  MinWarmupIterationCount=2

```

#### Class union

| Method                                | ClassUnion               |          Mean |         Error |       StdDev |       Gen0 | Allocated |
|---------------------------------------|--------------------------|--------------:|--------------:|-------------:|-----------:|----------:|
| **DefaultConverter_ClassUnion_Write** | **Case1**                |  **49.83 ns** |  **2.756 ns** | **1.441 ns** | **0.0076** |  **64 B** |
| SpecializedConverter_ClassUnion_Write | Case1                    |      23.46 ns |      0.642 ns |     0.336 ns |          - |         - |
| DefaultConverter_ClassUnion_Read      | Case1                    |      55.44 ns |      2.343 ns |     1.226 ns |     0.0076 |      64 B |
| SpecializedConverter_ClassUnion_Read  | Case1                    |      38.99 ns |      0.612 ns |     0.272 ns |          - |         - |
| **DefaultConverter_ClassUnion_Write** | **Case4(...):00 } [86]** | **352.12 ns** | **11.235 ns** | **5.876 ns** | **0.0076** |  **64 B** |
| SpecializedConverter_ClassUnion_Write | Case4(...):00 } [86]     |     312.50 ns |      4.621 ns |     2.052 ns |          - |         - |
| DefaultConverter_ClassUnion_Read      | Case4(...):00 } [86]     |     695.45 ns |     23.740 ns |    12.417 ns |     0.0191 |     160 B |
| SpecializedConverter_ClassUnion_Read  | Case4(...):00 } [86]     |     620.00 ns |     13.440 ns |     5.967 ns |     0.0114 |      96 B |

#### Struct union

| Method                                 | StructUnion              |          Mean |        Error |       StdDev |       Gen0 | Allocated |
|----------------------------------------|--------------------------|--------------:|-------------:|-------------:|-----------:|----------:|
| **DefaultConverter_StructUnion_Write** | **Case1**                |  **55.55 ns** | **2.735 ns** | **1.431 ns** | **0.0134** | **112 B** |
| SpecializedConverter_StructUnion_Write | Case1                    |      24.13 ns |     0.549 ns |     0.287 ns |          - |         - |
| DefaultConverter_StructUnion_Read      | Case1                    |      59.09 ns |     1.132 ns |     0.175 ns |     0.0134 |     112 B |
| SpecializedConverter_StructUnion_Read  | Case1                    |      39.33 ns |     0.674 ns |     0.240 ns |          - |         - |
| **DefaultConverter_StructUnion_Write** | **Case4(...):00 } [86]** | **347.04 ns** | **5.616 ns** | **0.869 ns** | **0.0134** | **112 B** |
| SpecializedConverter_StructUnion_Write | Case4(...):00 } [86]     |     307.65 ns |     5.607 ns |     2.000 ns |          - |         - |
| DefaultConverter_StructUnion_Read      | Case4(...):00 } [86]     |     649.65 ns |     8.134 ns |     1.259 ns |     0.0191 |     160 B |
| SpecializedConverter_StructUnion_Read  | Case4(...):00 } [86]     |     610.82 ns |     4.753 ns |     0.736 ns |     0.0057 |      48 B |

## Upcoming Features
- Unsafe features support (type pointers, method pointers).

## License
This project is licensed under the MIT License - see the LICENSE file for details.