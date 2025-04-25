0.7.0
------

- `System.Text.Json` support moved to the separate package `Dusharp.Json`.
- Added support for Newtonsoft via the `Dusharp.Newtonsoft` package.

0.6.2
------

- Fixed possible error `Could not load file or assembly 'Dusharp.Common'`.

0.6.1
------

- Added generation of `CanConvert` method for source-generated JSON converters.

0.6.0
------

- Added JSON serialization/deserialization support.
- Added generation of `Is{CaseName}` property and `TryGet{CaseName}Data` method.

0.5.3
------

- Made generated helper classes and attributes internal.

0.5.2
------

- Removed using of `System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode` as default hash code implementation for class unions.

0.5.1
------

- Downgraded required version of `Microsoft.CodeAnalysis.CSharp`.

0.5.0
------

- Added support for struct unions.

0.4.0
------

- Added pretty printing for union cases using overloaded `ToString` method

0.3.0
------

- Added generic support for unions
- Implemented code generation using nullable reference types feature

0.2.0
------

- Added equality comparison for unions

0.1.0
------

- Initial version
- Unions creation
- Unions matching