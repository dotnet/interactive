# Formatting

This document contains notes on how formatters are specified.

## User Specifications of Preferred Mime Types

Preferred mime types can be specified by using `Formatter.Register`, for example:

```csharp
Formatter.SetPreferredMimeTypeFor(typeof(System.Guid), "text/plain");
```

## User Specifications of Formatters

Formatters can be specified by using `Formatter.Register`, keyed by type, for example:

```csharp
Formatter.Register<System.Type>(t => t.GUID.ToString());
```

Formatters will apply to all subtypes of the given type, see the selection rules below.
Formatters can be specified by using a generic type definition as a key, for example:

```csharp
Formatter.Register(
    type: typeof(List<>),
    formatter: (obj: object, writer) =>
    {
        writer.Write("quack");
    }, mimeType);
```
Then 
```
var list = new List<int> { 1, 2, 3, 4, 5 };
```
displays as `quack`.  Reflection can also be used to operate on the object at its more specific type.


##  Selecting a Formatter

A formatter is chosen for a formatting operation on an object of type A as follows:

1. If no mime type is specified, determine one:

   - Choose the most-specific user-registered mime type preference relevant to A

   - If no user-registered mime-types are relevant, then choose a default mime type.

2. Next, determine a formatter:

   - Choose the most-specific user-registered formatter relevant to A

   - If no user-registered formatters are relevant, then choose a default formatter.

Here "most specific" is in terms of the class and interface hierarchy.   In the event of an exact tie in
ordering or some other conflict, more recent registerations are
preferred. Type-instantiations of generic types are preferred to generic
formatters when their type definition are the same.

The default sets of formatters for a mime type always include a formatter for `object`.

### Examples

For example:

* If the user registers a formatter for type `A` then it is used for all objects of type `A` (until alternative formatter for type A is later specified)

* If the user registers a formatter for `System.Object`, it is preferred over all other formatters except other user-defined formatters

* If the user registers a formatter for any sealed type, it is preferred over all other formatters (unless more formatters for that type are specified)

* If the user registers `List<>` and `List<int>` formatters the `List<int>` formatter is preferred for objects of type `List<int>`

* If the user registers a confusing conflicting mess of overlapping formatters incrementally, they should Formatters.Clear() or restart the kernel.

* If the user registers `text/plain` as the mime type for `object` then it is used as the mime type for everything (likewise any other mime type)


## Default Formatters

See DefaultHtmlFormatters.cs, DefaultPlainTextFormatters.cs and DefaultJsonFormatters.cs among others.
