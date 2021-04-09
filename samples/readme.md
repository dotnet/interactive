# .NET Interactive Samples

This folder contains a number of samples that you can use to explore features of .NET Interactive.

* [`connect-wpf`](connect-wpf) shows how you can embed .NET Interactive kernels within a .NET Core application and connect to them from a notebook. You can then use code to inspect, visualize, and change application state.

* [`docker-image`](docker-image/readme.md) contains a Dockerfile that generates an image with the latest .NET Interactive and Jupyter. This lets you try out .NET Interactive's Jupyter experience without needing to install Jupyter directly.

* [`ExtensionLab`](ExtensionLab) contains notebooks demonstrating some of the features in the [Microsoft.DotNet.Interactive.ExtensionLab](https://www.nuget.org/packages/Microsoft.dotnet.interactive.extensionlab) package. 

* [`extensions`](extensions/readme.md) contains sample projects that provide examples of how to create shareable .NET Interactive extensions and publish them using NuGet packages.

* [`my binder`](my%20binder) contains a Dockerfile that can be used as a template for deploying your notebooks along with the .NET Interactive tool using the [Binder](https://mybinder.org/) service.

* [`simple-fsharp-console`](simple-fsharp-console) demonstrates the most basic use of Interactive: it is a console app, executing user's commands and printing the result.

## Running the samples

If you're using Jupyter, the easiest way to run these samples, once you've [installed](../docs/install-dotnet-interactive.md) the .NET Interactive Jupyter kernel, is to open your Jupyter-enabled terminal, change your working directory to the `samples` folder, and run `jupyter lab`.

If you're using the .NET Interactive Notebooks extension for Visual Studio Code, you can open the sample notebooks directly.
