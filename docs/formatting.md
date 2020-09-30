# Formatting

This document contains notes on how formatters are specified, and some specific details about plain text and HTML formatting.

Formatting is invoked when values are displayed either implicitly (using a trailing expression) or using helper methods such as `display`.

##  Registering preferred MIME types

See the language-specific documentation on using `SetPreferredMimeTypeFor`.

```csharp
Formatter.SetPreferredMimeTypeFor(typeof(System.Guid), "text/plain");
```

##  Registering formatters

Formatters can be specified by using `Formatter.Register<T>`, keyed by type. See the language-specific documentation on using `Formatter.Register`.

For example, to customize the way a `Guid` is rendered, you can do the following:

```csharp
Formatter.Register<System.Type>(t => t.GUID.ToString());
```

Formatters can be specified by using an open generic type definition as a key as well. The following will register a formatter for variants of `List<T>` for all types `T`:

```csharp
Formatter.Register(
    type: typeof(List<>),
    formatter: (obj: object, writer) =>
    {
        writer.Write("quack");
    }, mimeType);
```

Then following will now render `"quack"`.

```csharp
var list = new List<int> { 1, 2, 3, 4, 5 };
list
```

##  How a formatter is chosen

The applicable formatter is chosen for an object of type `A` as follows:

1. If no MIME type is specified, determine one:

   - Choose the most specific user-registered MIME type preference relevant to `A`.

   - If no user-registered MIME types are relevant, then choose a default MIME type.

2. Next, determine a formatter:

   - Choose the most specific user-registered formatter relevant to `A`.

   - If no user-registered formatters are relevant, then choose a default formatter.

Here, "most specific" is in terms of the class and interface hierarchy. In the event of an exact tie in
ordering or some other conflict, more recent registerations are preferred. Type-instantiations of generic types are preferred to generic formatters when their type definitions are the same.

The default sets of formatters for a MIME type always include a formatter for `object`.

### Examples

Here are some examples to illustrate how formatter selection works.

* If you register a formatter for type `A` then it is used for all objects of type `A` (until an alternative formatter for type `A` is later specified).

* If you register a formatter for `System.Object`, it is preferred over all other formatters except other user-defined formatters that are more specific.

* If you register a formatter for any sealed type, it is preferred over all other formatters (unless more formatters for that type are specified).

* If you register `List<>` and `List<int>` formatters, the `List<int>` formatter is preferred for objects of type `List<int>`.

* If you register a confusing, conflicting mess of overlapping formatters incrementally, you can reset them by calling `Formatter.Clear()` or by restarting the kernel.

* If you register `text/plain` as the preferred MIME type for `object` then it is used as the MIME type for everything (and likewise any other MIME type).


## Default Formatters

See [`DefaultHtmlFormatterSet.cs`](https://github.com/dotnet/interactive/blob/main/src/Microsoft.DotNet.Interactive.Formatting/DefaultHtmlFormatterSet.cs), [`DefaultPlainTextFormatterSet.cs`](https://github.com/dotnet/interactive/blob/main/src/Microsoft.DotNet.Interactive.Formatting/DefaultJsonFormatterSet.cs) and [`DefaultJsonFormatterSet.cs`](https://github.com/dotnet/interactive/blob/main/src/Microsoft.DotNet.Interactive.Formatting/DefaultJsonFormatterSet.cs) among others.

## User Configuration of Default Formatters

The following global settings can be set to change formatting behaviors:

* `Formatter.RecursionLimit` = 20

  Gets or sets the limit to how many levels the formatter will recurse into an object graph.

* `Formatter.ListExpansionLimit` = 20

  Gets or sets the limit to the number of items that will be written out in detail from an `IEnumerable` sequence.

* `Formatter<T>.ListExpansionLimit` = (not set)

  An optional type-specific list expansion limit

* `PlainTextFormatter.MaxProperties` = 20

  Indicates the maximum number of properties to show in the default plaintext display of arbitrary objects. If set to zero no properties are shown.

* `HtmlFormatter.MaxProperties` = 20

  Indicates the maximum number of properties to show in HTML table displays of arbitrary objects. If set to zero no properties are shown.

## HTML Formatting

### The `CSS` function

The `CSS` function can be used to add CSS styling to the host HTML DOM.

Here are some examples:

```csharp
CSS("h3 { background: red; }");

CSS(".dni-plaintext { text-align: left; white-space: pre; font-family: monospace; }");
```

### CSS classes emitted

When .NET Interactive renders HTML content, some CSS classes are applied to enable easier styling using custom stylesheets.

| tag | content|
|:------|:-----------|
| `dni-plaintext` |  In HTML displays of values, any content generated by formatting arbitrary embedded object values as plaintext |

In the future, additional classes will be added to this list.





