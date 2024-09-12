# Dusharp

[![NuGet](https://img.shields.io/nuget/v/Dusharp)](https://www.nuget.org/packages/Dusharp/)

**Dusharp** is a C# source generator for creating **discriminated unions**. This library allows you to define union types with ease, using attributes and partial methods. It is inspired by functional languages but built for C# developers.

## Features

- ✅ **Create unions**: Define discriminated unions using attributes.
- ✅ **Match method**: Pattern match on union cases in a type-safe way.
- ✅ **Equality**: Automatic equality comparison for unions.
- ❌ **Pretty print**: Using overloaded `ToString()` (coming soon).
- ❌ **JSON serialization/deserialization**: Support for unions with `System.Text.Json` (coming soon).
- ❌ **Struct unions**: With efficient memory layout for unions as structs (coming soon).

## Installation

Dusharp is available as a NuGet package. You can install it using the NuGet package manager:

```bash
dotnet add package Dusharp
```

## Usage

`Dusharp` uses attributes to generate discriminated unions and case methods. Here's how to get started:

### 1. Define a Union
To define a union, annotate a class with the `[Dusharp.UnionAttribute]` attribute.

```csharp
using Dusharp;

[Union]
public partial class Shape { }
```

### 2. Define Union Cases
Define union cases by creating public static partial methods and marking them with the `[Dusharp.UnionCaseAttribute]` attribute. The method body will be automatically generated.

```csharp
using Dusharp;

[Union]
public partial class Shape
{
    [UnionCase]
    public static partial Shape Circle(double radius);

    [UnionCase]
    public static partial Shape Rectangle(double width, double height);
}
```

### 3. Match on Union
You can easily perform pattern matching on a union using the `Match` method. The source generator will create the `Match` method based on the defined union cases.

```csharp
Shape shape = Shape.Circle(5.0);

string result = shape.Match(
    radius => $"Circle with radius {radius}",
    (width, height) => $"Rectangle with width {width} and height {height}"
);

Console.WriteLine(result); // Output: Circle with radius 5.0
```

### 4. Compare Unions
Union cases can be compared for equality using the auto-generated equality methods. This allows for checking if two unions are the same.

```csharp
Shape shape1 = Shape.Circle(5.0);
Shape shape2 = Shape.Circle(5.0);

bool areEqual = shape1.Equals(shape2); // true
```

## Upcoming Features
- **Pretty printing**: A default ToString() implementation for unions that provides a readable representation of union cases.
- **JSON serialization/deserialization**: Support for JSON (de)serialization via System.Text.Json.
- **Struct unions**: More efficient unions using structs with effective data layout.

## License
This project is licensed under the MIT License - see the LICENSE file for details.