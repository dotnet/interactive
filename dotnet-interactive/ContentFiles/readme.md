# dotnet-interactive Jupyter kernel

https://github.com/dotnet/interactive

1. If you have installed Jupyter via Anaconda, open an Anaconda command prompt.

2. Execute the following in the command prompt:

	```console
	> dotnet interactive jupyter install
	```

3. You should see output similar to:

	```console
	[InstallKernelSpec] Installed kernelspec .net in C:\Users\AppData\Roaming\jupyter\kernels\.net-csharp
	[InstallKernelSpec] Installed kernelspec .net in C:\Users\AppData\Roaming\jupyter\kernels\.net-fsharp
	.NET kernel installation succeeded
	```

4. Now, executing `jupyter kernelspec list` will show the `dotnet-interactive` kernel:

	```console
	.net-csharp    C:\Users\AppData\Roaming\jupyter\kernels\.net-csharp
	.net-fsharp    C:\Users\AppData\Roaming\jupyter\kernels\.net-fsharp
	```
