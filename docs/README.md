# .NET Interactive Documentation 

## Getting started 

There are several ways to get started using .NET Interactive with Jupyter.

* [Try sample .NET notebooks online using Binder](NotebooksOnBinder.md).
* [Create and run .NET notebooks on your machine](NotebooksLocalExperience.md).
* [Share .NET notebooks online using Binder](CreateBinder.md).

### Telemetry

<!-- TODO:  What we collect, how to disable it  -->

https://docs.microsoft.com/en-us/dotnet/core/tools/telemetry

## Features

* Support for multiple languages
* Features:
    * Display output
    * Display HTML
    * Import NuGet packages 
    * Plotting with [Xplot](https://fslab.org/XPlot/)
* Specific language: 
    * C#
        * The [C# scripting](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting) dialect
        * PocketView
    * PowerShell
        * PowerShell profile support
        * PowerShell host support 
        * AzShell support
    * F#
        * F# Interactive (FSI)
    * JavaScript
* ["Magic commands"](./magic-commands.md)
* Getting input from the user
* Multi-language notebooks
    * Switching between languages
        * Per cell
        * Within a single cell
    * .NET variable sharing
    * Accessing kernel variables from the client with JavaScript 

## Technical details

* Architecture
* How Jupyter kernel installation works

## Visualization

* XPlot
* Visuaization with JavaScript libraries

## .NET Interactive API Guides

* Using the .NET Interactive [command-line interface](../src/dotnet-interactive/CommandLine/readme.md)

### .NET API Guide

* Commands and events
* Formatter APIS 
    * Working with mime types 
* PocketView
* Magic commands
* Kernel APIs
    * Variable sharing
* JSON API for Standard I/O and HTTP modes

### JavaScript API Guide

* Variable access from the client
* require.js support
 
## Extending .NET Interactive

* Adding magic commands
* Build your own extension
* Publish your extension 


