# Command line API

## Installing kernels

When installing `dotnet-interactive` you can provide a path where the kernelspec files used by Jupyter to configure a kernel will be installed. The provided path must be an existing directory.

  Command                                                      | Destination path
---------------------------------------------------------------|--------------------------------
`dotnet interactive jupyter install`                           | Use the kernelspec module if available. If not found, attempt to use well-known platform-specific folders for Python or Anaconda .
`dotnet interactive jupyter install --path c:\my_path`         | Will install the kernelspecs at the location if it exists

