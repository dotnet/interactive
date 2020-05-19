# Command line API

## Configuring the HTTP API

A number of APIs are available over HTTP in `dotnet-interactive`. You can enable the API and specify which port it is enabled on in a number of ways:

  Command                                                      | HTTP behavior
---------------------------------------------------------------|--------------------------------
`dotnet interactive http`                                      | Enabled on auto-selected port
`dotnet interactive http --http-port *`                        | Enabled on auto-selected port
`dotnet interactive http --http-port 1234`                     | Enabled on port 1234
`dotnet interactive stdio`                                     | Disabled
`dotnet interactive jupyter`                                   | Enabled on autoselected port between 1000 and 3000
`dotnet interactive jupyter --http-port-range 8001-9000`       | Enabled on autoselected port between 8001 and 9000

When installing `dotnet-interactive` as a Jupyter kernel, you can also configure the HTTP API's availability when the kernel is started, as follows: 

  Command                                                          | HTTP behavior of kernel 
-------------------------------------------------------------------|--------------------------------
`dotnet interactive jupyter install`                               | Enabled on autoselected port between 1000 and 3000
`dotnet interactive jupyter install --http-port-range 8001-9000`   | Enabled on autoselected port between 8001 and 9000                      

## Installing kernels

When installing `dotnet-interactive` you can provide a path where the kernelspec  files used by Jupyter to configure a kernel will be installed. The provided path must be an existing directory.

  Command                                                      | Destination path
---------------------------------------------------------------|--------------------------------
`dotnet interactive jupyter install`                           | Use the kernelspec module if available. If not found, attempt to use well-known platform-specific folders for Python or Anaconda .
`dotnet interactive jupyter install --path c:\my_path`         | Will install the kernelspecs at the location if it exists

