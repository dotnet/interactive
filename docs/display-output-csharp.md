# Displaying output in a notebook (C#)

##  Return values using trailing expressions

When writing C# in a .NET notebook, the [C# scripting](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting) dialect is used, which you might be familiar with from using the C# Interactive window in Visual Studio. This dialect of C# allows you to end a code submission with an expression and no semicolon. This tells C# scripting to return the value of the expression, which is also the value of the code submission. In a notebook, each cell corresponds to a code submission. This is known as a trailing expression in C#. The notebook will display this as an "execute result" cell output. In Jupyter, this is indicated by the bracketed submission number indicator on the left.

<img src="https://user-images.githubusercontent.com/547415/81757505-f4f3f500-9473-11ea-9890-6345837449ae.png" width="70%">

## Display helpers

A submission can only have a single return value, and a Jupyter cell can only have a single execute result. If you would like to produce more than one output from a single submission, you can use the `display` method. In Jupyter, the `display` method creates a "display data" output. You can see that this type of output doesn't have a corresponding submission number indicator to the left.

<img src="https://user-images.githubusercontent.com/547415/81757530-09d08880-9474-11ea-94e4-a1bd1b804fde.png" width="70%">

You can use `display` multiple times in a single submission, and you can combine this with a trailing expression.

<img src="https://user-images.githubusercontent.com/547415/81757387-975fa880-9473-11ea-9431-411ffe5a2866.png" width="70%">

Display outputs can be updated in place, for example to show the progress of an operation. The `display` method returns a `DisplayedValue` object and by calling `Update` you can replace what was displayed previously.

<img src="https://user-images.githubusercontent.com/547415/81757983-59638400-9475-11ea-8f05-257bb3e560dd.gif" width="70%">

## Console output

It's common to use `Console.WriteLine` (or `Console.Out.WriteLine`) in C# to write some information for display, so .NET Interactive captures output and redirects it to the notebook. It does the same for `Console.Error`, with a different display style to indicate that it was an error.

<img src="https://user-images.githubusercontent.com/547415/81758123-cd059100-9475-11ea-8474-02d2f71966e4.png" width="70%">

## Displaying Collections

The default display format for a type that implements `IEnumerable` is a tree view showing a row for each item and a one-line text representation of that object. The values can be expanded to show a more detailed view of the object. If you have a flat object with a small number of fields and properties it's often more useful to display the properties as columns in the top level table so you can see them without drilling down into each item. This can be done using the `ToTabularDataResource` extension method, which is in the `Microsoft.DotNet.Interactive.Formatting.TabularData` namespace.

For example:
```
using Microsoft.DotNet.Interactive.Formatting.TabularData;

class Person
{
    public string Title {get; set;}
    public string Name {get; set;}
}

var groupofheroes = new[]{
    new Person{Title = "Captain", Name = "Marvel"},
    new Person{Title = "General", Name = "Okoye"},
    new Person{Title = "Team Lead", Name = "Romanova"},
    new Person{Title = "Lead Engineer", Name = "Washington"}
};

groupofheroes.ToTabularDataResource()
```
