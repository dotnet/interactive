# .NET Interactive Documentation 

## Getting started 

There are several ways to get started using .NET Interactive with Jupyter.

* [Try sample .NET notebooks online using Binder](NotebooksOnBinder.md).
* [Create and run .NET notebooks on your machine](NotebooksLocalExperience.md).
* [Share your .NET notebooks with others online using Binder](CreateBinder.md).

## Features

* Support for multiple languages
* [Directives, or "Magic commands"](directives.md)
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
* Jupyter: Gestures for input and password
* Mixing languages in a notebook
    * .NET variable sharing
    * Accessing kernel variables from the client with JavaScript 
* Using the .NET Interactive [command-line interface](../src/dotnet-interactive/CommandLine/readme.md)
* How Jupyter kernel installation works

## .NET Interactive API Guides

### .NET API Guide

* Commands and events
* Formatter APIS 
    * Working with mime types 
* PocketView
* Kernel APIs
    * Variable sharing
* Standard I/O and HTTP mode JSON API

### JavaScript API Guide

* Variable access from the client
* require.js support
 
### Extending .NET Interactive

* Build your own extension
* Publish your extension 


