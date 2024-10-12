0.5.3
------

- Make generated helper classes and attributes internal.

0.5.2
------

- Remove using of `System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode` as default hash code implementation for class unions.

0.5.1
------

- Downgrade required version of `Microsoft.CodeAnalysis.CSharp`.

0.5.0
------

- Add support for struct unions.

0.4.0
------

- Add pretty printing for union cases using overloaded `ToString` method

0.3.0
------

- Add generic support for unions
- Implement code generation using nullable reference types feature

0.2.0
------

- Add equality comparison for unions

0.1.0
------

- Initial version
- Unions creation
- Unions matching