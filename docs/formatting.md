# Formatting

When using a tool backed by .NET Interactive (including Polyglot Notebooks, Jupyter, and others), the output you typically see is produced using .NET Interactive formatters, a set of APIs under the `Microsoft.DotNet.Interactive.Formatting` namespace. (These APIs are available in a [NuGet package](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.Formatting) that can be used independently of notebooks.) Formatters create string representations of objects. These string representations can vary from plain text to HTML to machine-readable formats like JSON and CSV. The following are examples of code you can write in a notebook that result in objects being formatted for display:

* A `return` statement or trailing expression at the end of a C# cell.
* A trailing expression at the end of an F# cell.
* A call to the `Display` and `ToDisplayString` extension methods, available for all objects in C# and F#.
* A call to `Out-Display` in a PowerShell cell.

Formatters are also used to format the output you see for .NET objects in the Polyglot Notebooks Variables View. (Formatting of values in other languages doesn't rely on .NET).

> _The term "formatting" refers to the process of creating a string representation of an object. This is done by the .NET Interactive kernel using the APIs described here. When a formatted string is then displayed in a notebook in VS Code or JupyterLab, that's referred to as "rendering."_

## MIME Types and `Display`

For any given object, many different string representations are possible. These different representations have associated MIME types, identified by short strings such as `text/html` or `application/json`. MIME types can be used to request specific formatting for an object using the `Display` extension method, which you can call with any object. In this example, we can display a `Rectangle` object assigned to variable `rect` by calling `rect.Display()`:

<img width="519" alt="hmmmm" src="https://user-images.githubusercontent.com/547415/223595260-b465d560-1b09-479b-a930-3c5ba271992d.png">

Note that the default MIME type in Polyglot Notebooks is `text/html`. This can vary from one type to another, but in the example above, no custom settings have been applied for the `Rectangle` type. (We'll show more about how to do that below.)

> _Note: For a cell's return value in C# or F#, only the formatter for the default MIME type can be used._ 

You can also specify a different MIME type than the default when using `Display`. To do this, you simply pass the desired MIME type as a parameter, for example: `rect.Display("text/plain")`. 

<img width="512" alt="image" src="https://user-images.githubusercontent.com/547415/223600244-c21863d4-61cc-4f11-a5c7-06eeffc9428b.png">

Another MIME type that's generally available is `application/json`. When using this MIME type in Polyglot Notebooks, the object is formatted using `System.Text.Json`.

<img width="511" alt="image" src="https://user-images.githubusercontent.com/547415/223600837-e50a3597-7589-4ed2-add7-254139e8aaec.png">

In the above screen shot, the code coloring in the JSON output is provided by the [VS Code notebook renderer](https://code.visualstudio.com/api/extension-guides/notebook#notebook-renderer) for `application/json`.  

## Configuring formatting

The .NET Interactive formatting APIs are highly configurable. The next section describes the various ways that you can change how formatting behaves in notebooks or in any own code that uses `Microsoft.DotNet.Interactive.Formatting` directly.

### Limiting list output

When formatting sequences, such as arrays or objects implementing `IEnumerable`, .NET Interactive's formatters will expand them so that you can see the values. The following example calls `DirectoryInfo(".").GetFileSystemInfos()` to display the files and directories under the current directory:

<img width="517" alt="image" src="https://user-images.githubusercontent.com/547415/223888595-c4c37c2d-9327-4a4c-bc0d-f33c9c77175d.png">

At the end of the displayed list is the text `(31 more)`. Twenty items were displayed after which the formatter stopped and showed the remaining count. If you'd like to see more or fewer items in the output, this limit can be configured both globally and per-.NET type.

To change this limit globally so that it applies to objects of any type, you can set `Formatter.ListExpansionLimit`.

<img width="515" alt="image" src="https://user-images.githubusercontent.com/547415/223889502-d3041962-3431-42a2-99f0-1933c939d91b.png">

In this example, by setting `Formatter.ListExpansionLimit = 5` and then displaying the same file list, .NET Interactive now displays only the first five items, followed by `(46 more)`.

You can similarly limit output for a specific type by setting `Formatter<T>.ListExpansionLimit`. Note that the type `T` here must be an exact match for the items in the list. Here's an example using `int`:

```csharp
Formatter<int>.ListExpansionLimit = 3;
Enumerable.Range(1, 10)
```

This produces the following output:

```console
[ 1, 2, 3 ... (more) ]
```

You'll note that this sequence ends with `(more)` rather than `(7 more)`. This is because the return type of the `Enumerable.Range` call is `IEnumerable<int>` and so its count can't be known without enumerating the entire sequence. In this case, the .NET Interactive formatter stops when it reaches the configured `ListExpansionLimit` and doesn't count the rest of the sequence.  

### Limiting object graph recursion

It's common for object graphs to contain reference cycles. The .NET Interactive formatter will traverse object graphs but in order to avoid both oversized outputs and possible infinite recursion when there is a reference cycle, the formatter will only recurse to a specific depth. 

Consider the following C# code, which defines a simple `Node` class, creates a reference cycle, and formats it using a C# Script trailing expression (which is equivalent to a return statement):

```csharp
public class Node
{
    public Node Next { get; set; } 
}

Node node1 = new();
Node node2 = new();

node1.Next = node2;
node2.Next = node1;

node1
```

This code produces the following output (presented here without the Polyglot Notebooks styling):

<details open="open" class="dni-treeview"><summary><span class="dni-code-hint"><code>Submission#3+Node</code></span></summary><div><table><thead><tr></tr></thead><tbody><tr><td>Next</td><td><details class="dni-treeview"><summary><span class="dni-code-hint"><code>Submission#3+Node</code></span></summary><div><table><thead><tr></tr></thead><tbody><tr><td>Next</td><td><details class="dni-treeview"><summary><span class="dni-code-hint"><code>Submission#3+Node</code></span></summary><div><table><thead><tr></tr></thead><tbody><tr><td>Next</td><td><details class="dni-treeview"><summary><span class="dni-code-hint"><code>Submission#3+Node</code></span></summary><div><table><thead><tr></tr></thead><tbody><tr><td>Next</td><td><details class="dni-treeview"><summary><span class="dni-code-hint"><code>Submission#3+Node</code></span></summary><div><table><thead><tr></tr></thead><tbody><tr><td>Next</td><td><details class="dni-treeview"><summary><span class="dni-code-hint"><code>Submission#3+Node</code></span></summary><div><table><thead><tr></tr></thead><tbody><tr><td>Next</td><td>Submission#3+Node</td></tr></tbody></table></div></details></td></tr></tbody></table></div></details></td></tr></tbody></table></div></details></td></tr></tbody></table></div></details></td></tr></tbody></table></div></details></td></tr></tbody></table></div></details>

What this shows is that the formatter stopped recursing after formatting to a depth of 6. This depth can be changed using the `Formatter.RecursionLimit` method:

```csharp
Formatter.RecursionLimit = 2;
node1
```

Running this code now produces this shorter output:

<details open="open" class="dni-treeview"><summary><span class="dni-code-hint"><code>Submission#3+Node</code></span></summary><div><table><thead><tr></tr></thead><tbody><tr><td>Next</td><td><details class="dni-treeview"><summary><span class="dni-code-hint"><code>Submission#3+Node</code></span></summary><div><table><thead><tr></tr></thead><tbody><tr><td>Next</td><td>Submission#3+Node</td></tr></tbody></table></div></details></td></tr></tbody></table></div></details>

### Replacing the default formatting for a type

The default formatters typically show the values of the objects being displayed by printing lists and properties. The output is mostly textual. If you would like to see something different for a given type, whether a different textual output or an image or a plot, you can do this by register custom formatters for specific types. These can be types you defined or types defined in other .NET libraries. One common case where a custom formatter is used is when rendering plots. Some NuGet packages, such as [Plotly.NET](https://www.nuget.org/packages/Plotly.NET/), carry .NET Interactive [extensions](extending-dotnet-interactive.md) that use this feature to provide interactive HTML- and JavaScript-based outputs.

The simplest method for registering a custom formatter is `Formatter.Register<T>`, which has a few different overloads. The friendliest one for use in a notebook accepts two arguments: 

* A delegate that takes an object of the the type you want to register and returns a string. Here, you can specify the string transformation you need.
* A MIME type. Your custom formatter will only be called whenthis MIME type is being used.

The following example formats instances of `System.Drawing.Rectangle` as a SVG rectangles.  

```csharp
Formatter.Register<Rectangle>(
    rect => $"""
         <svg width="100" height="100">
           <rect width="{rect.Width}" 
                 height="{rect.Height}" 
                 style="fill:rgb(0,255,200)" />
         </svg>
         """, 
    mimeType: "text/html");
```

After this code has been run, `Rectangle` objects will be displayed as graphical rectangles rather than lists of property values. (The following examples use the C# Script trailing expression syntax which will usually be configured to use the `text/html` MIME type in notebooks.)

<img width="511" alt="image" src="https://user-images.githubusercontent.com/547415/224179749-46f59895-f3a8-4da8-a935-c76c61426360.png">

It's also worth pointing out that when the customized type is encountered within a list or as an object property, custom formatters will still be invoked. 

Here's an example showing an array:

<img width="514" alt="image" src="https://user-images.githubusercontent.com/547415/224182165-de606d25-3cdb-43ec-99ce-e697fd1d9856.png">

Here's an example showing an anonymous object with a property of type `Rectangle`:

<img width="514" alt="image" src="https://user-images.githubusercontent.com/547415/224182122-de0bfa4f-a293-4b86-9a62-9505342a2b80.png">

Other overloads of `Formatter.Register` enable you to handle some additional complexities. 

#### Open generic types

Formatters can be specified by using an open generic type definition as a key. The following will register a formatter for variants of `List<T>` for all types `T` and print each element along with its hash code. (Note that it's necessary to cast the object in order to iterate over its items.)

```csharp
Formatter.Register(
    type: typeof(List<>),
    formatter: (list, writer) =>
    {
        foreach (var obj in (IEnumerable)list)
        {
            writer.WriteLine($"{obj} ({obj.GetHashCode()})");
        }
    }, "text/html");
```

After running the code above, the following will no longer print just the values in the list.

```csharp
var list = new List<string> { "one", "two", "three" };
list
```

The output will now look like this:

```console
one (254814599) two (656421459) three (-1117028319)
```

#### `TypeFormatterSourceAttribute`

Another approach that can be used to register custom formatters is to decorate a type with `TypeFormatterSourceAttribute`. For use within a notebook, this isn't as convenient as `Formatter.Register`. But if you're writing a .NET Interactive extension or a library or app that uses `Microsoft.DotNet.Interactive.Formatting`, this is the recommended approach.

###  Registering preferred MIME types

We mentioned above that the default MIME type used for formatting in Polyglot Notebooks is `text/html`. This default is applied when using the `Display()` method without passing a value to the `mimeType` parameter or when using a `return` statement or trailing expression in C# or F#. This default can be changed globally or for a specific type.

The following example changes the default for `Rectangle` to `text/plain`.

```csharp
using System.Drawing;

Formatter.SetPreferredMimeTypesFor(typeof(Rectangle), "text/plain");

new Rectangle
{   
    Height = 50, 
    Width = 100
}
```

```console
Rectangle
  Location: Point
    IsEmpty: True
    X: 0
    Y: 0
  Size: Size
    IsEmpty: False
    Width: 100
    Height: 50
  X: 0
  Y: 0
  Width: 100
  Height: 50
  Left: 0
  Top: 0
  Right: 100
  Bottom: 50
  IsEmpty: False
```

### Resetting formatting configurations

As you experiment with different formatting configurations, you might find you want to reset everything to the defaults that you see when you first start the kernel. You can do this easily:

```csharp
Formatter.ResetToDefault();
```

This will reset all of the configurations that might have been changed by using the APIs described above. Note that this might also reset formatting set up by extensions installed using NuGet packages. 

##  How a formatter is chosen

It's possible to have more than one formatter registered that might apply to the same type. For example, formatters can be registered for `object`, `IEnumerable<string>`, and `IList<string>`, any of which might reasonably apply to an instance of `List<string>`. For these reasons, it can be useful to understand how a formatter is chosen.

The applicable formatter is chosen for an object of type `A` as follows:

1. If no MIME type is specified, determine one:

   - Choose the most specific user-registered MIME type preference relevant to `A`.

   - If no user-registered MIME types are relevant, then use the configured default MIME type.

   This MIME type is looked up by by calling `Formatter.GetPreferredMimeTypesFor(typeof(A))`, which returns one or more MIME types. If the one specified by `Formatter.DefaultMimeType` is in the list, it will be preferred.

2. Next, determine a formatter:

   - Choose the most specific user-registered formatter relevant to `A`.

   - If no user-registered formatters are relevant, then choose a default formatter.

Here, "most specific" is in terms of the class and interface hierarchy. In the event of an exact tie in ordering or some other conflict, more recent registrations are preferred. Type-instantiations of generic types are preferred to generic formatters when their type definitions are the same.

The default sets of formatters for a MIME type always include a formatter for `object`.

This set of rules is implemented by the `Formatter.GetPreferredFormatterFor` method. Here's an example of how you can use it to get a formatter instance and format an object with it:

```csharp
using System.Drawing;
using System.IO;
using Microsoft.DotNet.Interactive.Formatting;

ITypeFormatter formatter = 
    Formatter.GetPreferredFormatterFor(
        typeof(Rectangle), 
        Formatter.DefaultMimeType);

var rect = new Rectangle { X = 100, Y = 50 };

var writer = new StringWriter();

formatter.Format(rect, writer);

Console.WriteLine(writer.ToString());
```

### Examples

Here are some examples to illustrate how formatter selection works.

* If you register a formatter for type `A` then it is used for all objects of type `A` (until an alternative formatter for type `A` is later specified).

* If you register a formatter for `System.Object`, it is preferred over all other formatters except other user-defined formatters that are more specific.

* If you register a formatter for any sealed type, it is preferred over all other formatters (unless more formatters for that type are specified).

* If you register `List<>` and `List<int>` formatters, the `List<int>` formatter is preferred for objects of type `List<int>`, while the `List<>` formatter is preferred for other generic instantiations, for example `List<string>`.

## See also

* [PocketView](pocketview.md)