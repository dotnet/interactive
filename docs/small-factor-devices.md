# Using .NET Interactive on small factor devices

We support running .NET Interactive on small factor devices like [Raspberry Pi](https://www.raspberrypi.org/) and [pi-top [4]](https://github.com/pi-top/pi-top-4-.NET-Core-API).

## Installing .NET Interactive on a Raspberry Pi

We suggest running on a Pi 4 or above as the device provides more computing power. We provide a simple way to get .NET Interactive global tool, .NET Core sdk, Jupyter and Jupyter lab ready to go.

Open a terminal and type
```bash
curl -L https://raw.githubusercontent.com/dotnet/interactive/master/tools/setup-raspbian.sh | bash -e
```

Now you can activate the jupyter virtual environment using the command
```bash
pi@raspberrypi:~ $ source ~/.jupyter_venv/bin/activate
```

The prompt in the active environment will look like this
```bash
(.jupyter_venv) pi@raspberrypi:~ $ 
```

Finally start Jupyter Lab 
```bash
(.jupyter_venv) pi@raspberrypi:~ $ jupyter lab --ip 0.0.0.0
```

## Installing .NET Interactive on a piTop[4] 

Follow the instructions on the [piTop[4] .NET sdk](https://github.com/pi-top/pi-top-4-.NET-SDK) repository.

## Libraries and tools

The [dotnet/iot](https://github.com/dotnet/iot) provides a wide palette of device bindings that you can consume as NuGet packages.
