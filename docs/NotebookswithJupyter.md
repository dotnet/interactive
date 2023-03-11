# Using .NET Interactive with Jupyter 

To use the .NET Interactive kernel for your multi-language notebooks in Jupyter Notebook, JupyterLab, and other Jupyter frontends, you first need to register .NET Interactive as a kernel with Jupyter.  

Make sure you have the following installed:

* The [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download).
* **Jupyter**. An easy way to install Jupyter is through [Anaconda](https://www.anaconda.com/distribution).

* You can verify the installations by opening the **Anaconda Prompt** (Windows) or a terminal (macOS, Linux) and running the following commands to ensure that Jupyter and .NET are installed and present on the path:

```console
> jupyter kernelspec list
  python3        ~\jupyter\kernels\python3
> dotnet --version
  7.0.200
```

(The `dotnet` minor version isn't important.)

* Next, in an **ordinary console**, install the `dotnet interactive` global tool:

```console
> dotnet tool install -g Microsoft.dotnet-interactive
```

* **Switch back to your Anaconda prompt** and install the .NET kernel by running `dotnet interactive jupyter install`:

```console
> dotnet interactive jupyter install
Installing using jupyter kernelspec module.
Installed ".NET (C#)" kernel.
Installing using jupyter kernelspec module.
Installed ".NET (F#)" kernel.
Installing using jupyter kernelspec module.
Installed ".NET (PowerShell)" kernel.
```
    
* You can verify the installation by running the following again in the **Anaconda Prompt**. You should now see a `kernelspec` entry for each of the default supported .NET languages:

```console
> jupyter kernelspec list
  .net-csharp        ~\jupyter\kernels\.net-csharp
  .net-fsharp        ~\jupyter\kernels\.net-fsharp
  .net-powershell    ~\jupyter\kernels\.net-powershell
  python3            ~\jupyter\kernels\python3
```

## Updating .NET Interactive

To update to the latest version of .NET Interactive, open an **ordinary console** and run the following code: 

```console
> dotnet tool update -g Microsoft.dotnet-interactive
```

## Running the .NET Interactive Jupyter kernel

To launch Jupyter, you can run either `jupyter lab` or `jupyter notebook` from your **Anaconda Prompt**, or you can launch Jupyter using the Anaconda Navigator.

Once Jupyter has launched in your browser, you have the option to create notebooks using C#, F#, or PowerShell.

<img src = "https://user-images.githubusercontent.com/547415/78056370-ddd0cc00-7339-11ea-9379-c40f8b5c1ae5.png" width = "70%">
