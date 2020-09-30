# Using .NET notebooks on your machine

There are two ways to use .NET notebooks on your machine with .NET Interactive:

* Install .NET Interactive as a Jupyter kernel for use with Jupyter Notebook, JupyterLab, nteract, Azure Data Studio, and others.

or 

* Install the [.NET Interactive Notebooks extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode)

Both of these methods can read and write `.ipynb` files, which are fully portable between them.

## Installing .NET Interactive as a Jupyter kernel

First, make sure you have the following installed:

* The [.NET 3.1 SDK](https://dotnet.microsoft.com/download).
* **Jupyter**. An easy way to install Jupyter is through [Anaconda](https://www.anaconda.com/distribution).

* Open the **Anaconda Prompt** (Windows) or Terminal (macOS) and verify that Jupyter is installed and present on the path:

```console
> jupyter kernelspec list
  python3        ~\jupyter\kernels\python3
```

* Next, in an **ordinary console**, install the `dotnet interactive` global tool:

```console
> dotnet tool install -g --add-source "https://dotnet.myget.org/F/dotnet-try/api/v3/index.json" Microsoft.dotnet-interactive
```

*Note: The [MyGet](https://dotnet.myget.org) feed is where the most up-to-date version will be published. Older, more stable versions will be made available on [NuGet.org](https://nuget.org).*

* **Switch back to your Anaconda prompt** and install the .NET kernel by running the following:

```console
> dotnet interactive jupyter install
Installing using jupyter kernelspec module.
Installed ".NET (C#)" kernel.
Installing using jupyter kernelspec module.
Installed ".NET (F#)" kernel.
Installing using jupyter kernelspec module.
Installed ".NET (PowerShell)" kernel.
```
    
* You can verify the installation by running the following again in the **Anaconda Prompt**:

```console
> jupyter kernelspec list
  .net-csharp        ~\jupyter\kernels\.net-csharp
  .net-fsharp        ~\jupyter\kernels\.net-fsharp
  .net-powershell    ~\jupyter\kernels\.net-powershell
  python3            ~\jupyter\kernels\python3
```

## Running the .NET Interactive Jupyter kernel

To launch Jupyter, you can run either `jupyter lab` or `jupyter notebook` from your **Anaconda Prompt**, or you can launch Jupyter using the Anaconda Navigator.

Once Jupyter has launched in your browser, you have the option to create notebooks using C#, F#, or PowerShell.

<img src = "https://user-images.githubusercontent.com/547415/78056370-ddd0cc00-7339-11ea-9379-c40f8b5c1ae5.png" width = "70%">

For more information on the .NET notebook experience, please check out our samples and documentation on [Binder](https://mybinder.org/v2/gh/dotnet/interactive/master?urlpath=lab) or in this repo under [`docs`](../docs/readme.md) and [`samples`](../samples/readme.md).

Once you've created a .NET notebook, you might want to share it with others. In the [next document](CreateBinder.md), you will learn how to share your .NET notebook with others using Binder. 

