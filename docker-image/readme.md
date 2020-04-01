# >NET interactive docker image

This folder conains a docker fiel to generate an image to use latest .NET interactive tool without the need to isntall anacoda on your machine.

## Build instructions

Build the docker file to create the image.

```powershell
> docker build . --tag dotnet-interactive:1.0
```

The container exposes port 8888 and the port range 1100-1200. A different port range can be specified at build time.

```powershell
> docker build . --tag dotnet-interactive:1.0 --build-args HTTP_PORT_RANGE=1000-1100
```

## Run the container

To run the container  execute

```powershell
> docker run --rm -it -p 8888:8888 -p 1100-1200:1100-1200  --name dotnet-interactive-image dotnet-interactive:1.0
```

Rember to match the port range with the range used when building the image.

A jupyterlab instance will be published on `http://127.0.0.1:8888/`.
Check the output of the docker run command for the full url with token value.
