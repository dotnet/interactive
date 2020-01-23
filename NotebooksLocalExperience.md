# Create .NET Jupyter Notebooks

## Installing the .NET kernel for Jupyter 

First, make sure you have the following installed:

- The [.NET 3.0 SDK](https://dotnet.microsoft.com/download).
- **Jupyter**. Jupyter can be installed using [Anaconda](https://www.anaconda.com/distribution).

- Open the Anaconda Prompt (Windows) or Terminal (macOS) and verify that Jupyter is installed and present on the path:

```console
> jupyter kernelspec list
  python3        ~\jupyter\kernels\python3
```

- Next, in an ordinary console, install the `dotnet interactive` global tool:

```console
> dotnet tool install -g --add-source "https://dotnet.myget.org/F/dotnet-try/api/v3/index.json" dotnet-interactive
```

- Install the .NET kernel by running the following within your Anaconda Prompt:

```console
> dotnet interactive jupyter install
[InstallKernelSpec] Installed kernelspec .net-csharp in ~\jupyter\kernels\.net-csharp
.NET kernel installation succeeded

[InstallKernelSpec] Installed kernelspec .net-fsharp in ~\jupyter\kernels\.net-fsharp
.NET kernel installation succeeded
```
    
- You can verify the installation by running the following again in the Anaconda Prompt:

```console
> jupyter kernelspec list
  .net-csharp    ~\jupyter\kernels\.net-csharp
  .net-fsharp    ~\jupyter\kernels\.net-fsharp
  python3        ~\jupyter\kernels\python3
```

## Using Jupyter with .NET

To launch JupyterLab, you can either type `jupyter lab` in the Anaconda Prompt or launch a notebook using the Anaconda Navigator.

Once Jupyter Lab has launched in your browser, you have the option to create a notebook for C# or F#.

<img src = "https://user-images.githubusercontent.com/2546640/72949473-60477900-3d56-11ea-8bc4-47352a613b78.png" width = "80%">

Once the notebook opens, you can start working with .NET in the language you chose.

<img src = "https://user-images.githubusercontent.com/2546640/67981834-db860c80-fbf7-11e9-89b5-29d2480ed1fa.png" width = "70%">

For more information on the .NET notebook experience, please check out our samples and documentation on [Binder](https://mybinder.org/v2/gh/dotnet/try/master?urlpath=lab).

 Once you've created a .NET notebook, you might want to share it with others. In the [next document](CreateBinder.md), you will learn how to share your .NET notebook with others using binder. 
