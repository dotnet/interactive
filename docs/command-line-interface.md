# The `dotnet-interactive` Command Line Interface

Polyglot Notebooks and .NET support for Jupyter are powered by the [`dotnet-interactive`](https://www.nuget.org/packages/Microsoft.dotnet-interactive) .NET tool. Once installed, you can get help by running:

```console
dotnet interactive --help
```

Here is a brief overview of the available commands.

## `dotnet interactive stdio` 

This command starts `dotnet-interactive` as a server in standard I/O mode. In this mode, JSON-serialized commands and events are sent in both directions over both stdin and stout. 

This mode is used by the Polyglot Notebooks extension for VS Code, as well as other editors.

## `dotnet interactive jupyter` 

This command starts `dotnet-interactive` in Jupyter mode, allowing it to be used as a kernel by any Jupyter frontend or Jupyter Server.

## `dotnet interactive jupyter install` 

This command installs kernelspecs that register the .NET Interactive kernel with Jupyter. Three kernelspecs are installed: C#, F#, and PowerShell. But in each case the underlying kernel is the same, and each of the kernelspecs simply starts `dotnet-interactive` in a different default language mode. The full polyglot capabilities are still available at runtime using kernel selector magic commands, regardless of the default language.

By default, this command will use the `jupyter kernelspec` module, if it is available in the host terminal environment. If the kernelspec module is not found, `dotnet interactive jupyter install` will attempt to install the kernelspecs in well-known platform-specific folders for Python or Anaconda. You can also install the kernelspecs in a specific location by running:

```console
dotnet interactive jupyter install --path /location/to/install
```

## `dotnet interactive notebook-parser` 

This command starts `dotnet-interactive` in parser server mode, which can be used to read and write notebook files, including the `.ipynb` and `.dib` formats.

The notebook parser server is used by the Polyglot Notebooks extension for VS Code, as well as other editors.
