# Create .NET Jupyter Notebooks on your machine

## Installing Jupyter and .NET Interactive 

First, make sure you have the following installed:

- The [.NET 3.1 SDK](https://dotnet.microsoft.com/download).
- **Jupyter**. An easy way to install Jupyter is through [Anaconda](https://www.anaconda.com/distribution).

- Open the Anaconda Prompt (Windows) or Terminal (macOS) and verify that Jupyter is installed and present on the path:

```console
> jupyter kernelspec list
  python3        ~\jupyter\kernels\python3
```

- Next, in an ordinary console, install the `dotnet interactive` global tool:

```console
> dotnet tool install -g --add-source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" Microsoft.dotnet-interactive
```

- Install the .NET kernel by running the following within your Anaconda Prompt:

```console
> dotnet interactive jupyter install
Installing using jupyter kernelspec module.
Installed ".NET (C#)" kernel.
Installing using jupyter kernelspec module.
Installed ".NET (F#)" kernel.
Installing using jupyter kernelspec module.
Installed ".NET (PowerShell)" kernel.
```
    
- You can verify the installation by running the following again in the Anaconda Prompt:

```console
> jupyter kernelspec list
  .net-csharp        ~\jupyter\kernels\.net-csharp
  .net-fsharp        ~\jupyter\kernels\.net-fsharp
  .net-powershell    ~\jupyter\kernels\.net-powershell
  python3            ~\jupyter\kernels\python3
```

## Using Jupyter with .NET

To launch JupyterLab, you can either type `jupyter lab` in the Anaconda Prompt or launch a notebook using the Anaconda Navigator.

Once Jupyter Lab has launched in your browser, you have the option to create notebooks using C#, F#, or PowerShell.

<img src = "https://user-images.githubusercontent.com/547415/78056370-ddd0cc00-7339-11ea-9379-c40f8b5c1ae5.png" width = "70%">

For more information on the .NET notebook experience, please check out our samples and documentation on [Binder](https://mybinder.org/v2/gh/dotnet/interactive/master?urlpath=lab) or in this repo under [`samples`](../samples/readme.md).

 Once you've created a .NET notebook, you might want to share it with others. In the [next document](CreateBinder.md), you will learn how to share your .NET notebook with others using Binder. 
