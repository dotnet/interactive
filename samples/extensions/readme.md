# Sample .NET Interactive Extensions

The projects in this directory provide examples of how you can create and publish .NET Interactive kernel extensions.

The `ClockExtension` sample builds a standalone extension that adds a magic command and applies custom formatting in order to visualize `System.DateTime` using an SVG rendering of a clock. This example is fairly simple because it only works with types already referenced by .NET Interactive. You can see a walkthrough showing how to build and install the extension in [this notebook](ClockExtension.ipynb).

The `Library` sample is more complex. This sample illustrates how you can augment your own libraries with notebook-specific enhancements that will be available when someone uses you library in .NET Interactive, but without adding a direct dependency from your library to .NET Interactive.

You can read more about building extensions [here](../../docs/extending-dotnet-interactive.md).
