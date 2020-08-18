# Working with NuGet packages

The C# and F# kernels in .NET Interactive allow you to import NuGet packages into your interactive session using the `#r nuget` magic command. The syntax is the same in both languages.

To import the latest version a package, you can use `#r nuget` without specifying a version number:

```csharp
#r "nuget:System.Text.Json"
```

If you'd like to use a specific version, you can specify it like this:

```csharp
#r "nuget:System.Text.Json,4.7.2"
```

## Adding a Nuget Source

If your nuget package is not hosted on the main Nuget feed you can specify an alternative nuget source using `#i`.

```csharp
#i "nuget:https://www.myget.org/F/{feedurl}/api/v3/index.json"
```
