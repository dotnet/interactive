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

If you're looking for the latest pre-release version, you can specify it like this:

```csharp
#r "nuget:System.Text.Json,*-*"
```

## Adding a Nuget Source

If your nuget package is not hosted on the main Nuget feed you can specify an alternative nuget source using `#i`. 

### Remote Nuget Sources

It is common for organizations to store packages on a private or pre-release feed. In the following example we are adding the [dotnet project](https://github.com/dotnet) pre-release nuget feed.

```csharp
#i "nuget:https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json"
```

### Local Nuget Sources

You may also use a local folder as a nuget source:

```csharp
#i "nuget:C:\myorg\mypackage\src\bin\Release"
#r "nuget:MyOrg.MyPackage"
```
