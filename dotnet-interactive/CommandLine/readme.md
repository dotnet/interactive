# Command line API

## Configuring the HTTP API

A number of APIs are available over HTTP in `dotnet-interactive`. You can enable the API and specify which port it is enabled on in a number of ways:

| Command                                                      | HTTP behavior
|--------------------------------------------------------------|--------------------------------
dotnet interactive http                                        | Enabled on auto-selected port
dotnet interactive http --http-port *                          | Enabled on auto-selected port
dotnet interactive http --http-port 1234                       | Enabled on port 1234
dotnet interactive http --http-port-range 8001-9000            | Enabled on a port between 8001 and 9000
dotnet interactive stdio                                       | Disabled
dotnet interactive stdio --http-port *                         | Enabled on auto-selected port
dotnet interactive stdio --http-port 1234                      | Enabled on port 1234
dotnet interactive stdio --http-port-range 8001-9000           | Enabled on a port between 8001 and 9000
dotnet interactive jupyter                                     | Disabled
dotnet interactive jupyter --http-port *                       | UNSUPPORTED
dotnet interactive jupyter --http-port 1234                    | UNSUPPORTED
dotnet interactive jupyter --http-port-range 8001-9000         | Enabled on a port between 8001 and 9000

When installing `dotnet-interactive` as a Jupyter kernel, you can also configure the HTTP API's availability when the kernel is started, as follows: 

| Command                                                      | HTTP behavior of kernel 
|--------------------------------------------------------------|--------------------------------
dotnet interactive jupyter install                             | Disabled
dotnet interactive jupyter install --http-port-range 8001-9000 | Enabled on autoselected port between 8001 and 9000                      

 


## Installing kernels

When installing `dotnet-interactive` it is possible to provide the path for installing the kernel specs, the path msut exists.

| Command                                                      | Destination path
|--------------------------------------------------------------|--------------------------------
dotnet interactive jupyter install                             | Will use the kernelspec module if available, if it is not present will attempt ot use platform specific folders for python or anaconda paths
dotnet interactive jupyter install --path c:\my_path | Will install the kernelspecs at the location if it exists



