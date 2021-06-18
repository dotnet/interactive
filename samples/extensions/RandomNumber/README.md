`RandomNumber` script-based extension
=====================================

On the surface this project is a simple `netstandard2.0` library that packs itself as a NuGet package.  There is no
runtime or build-time dependency on the `Microsoft.DotNet.Interactive` packages or tool, and any other project that
references this will not be forced to take on those dependencies.  When this package is loaded into a .NET Interactive
notebook, however, it _will_ register itself with the .NET Interactive host and prodide additional capabilities.

This is all handled via the `extension.dib` file.  There is one part in the project file that places `extension.dib` in
a well-known location within the resultant `.nupkg`:

``` xml
<ItemGroup>
  <None Include="extension.dib" Pack="true" PackagePath="interactive-extensions/dotnet" />
</ItemGroup>
```

When this NuGet package is referenced in a notebook via:

```
#r "nuget:RandomNumber, 0.1.0"
```

The contents of `extension.dib` are immediately executed.  In the case of this extension, a magic command
`#!get-random-number` is registered with the root kernel, but other extension-like behaviors like registering
formatters can also happen.
