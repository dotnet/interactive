# Using .NET Interactive on small factor devices

We support running .NET Interactive on small factor devices like [Raspberry Pi](https://www.raspberrypi.org/) and [pi-top [4]](https://github.com/pi-top/pi-top-4-.NET-Core-API).

## Installing .NET Interactive on a Raspberry Pi

We suggest running on a Pi 4 or above as the device provides more computing power. We provide a simple way to get the .NET Interactive tool and .NET Core SDK.

Open a terminal and run:
```bash
curl -L https://raw.githubusercontent.com/dotnet/interactive/master/tools/setup-raspbian.sh | bash -e
```

To start using the notebook experiences you can use Visual Studio Code or Jupyter.

## Installing Visual Studio Code Insiders Notebook Experience

Install [Visual Studio Code Insiders](https://code.visualstudio.com/insiders/) on your device. 
Next, get the [.NET Interactive Notebooks](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode) extension.

## Installing and configuring Jupyter on a Raspberry Pi

Open a terminal and run:
```bash
curl -L https://raw.githubusercontent.com/dotnet/interactive/master/tools/setup-raspbian-jupyter.sh | bash -e
```

Now you can activate the Jupyter virtual environment using the following command:
```bash
pi@raspberrypi:~ $ source ~/.jupyter_venv/bin/activate
```

The prompt in the active environment will look like this:
```bash
(.jupyter_venv) pi@raspberrypi:~ $ 
```

Finally, start JupyterLab by running:
```bash
(.jupyter_venv) pi@raspberrypi:~ $ jupyter lab --ip 0.0.0.0
```
You can also use Jupyter by running:
```bash
(.jupyter_venv) pi@raspberrypi:~ $ jupyter  --ip 0.0.0.0
```

## Installing .NET Interactive on a piTop[4] 

Follow the instructions on the [piTop[4] .NET SDK](https://github.com/pi-top/pi-top-4-.NET-SDK) repository.

## Libraries and tools

The [dotnet/iot](https://github.com/dotnet/iot) provides a wide palette of device bindings that you can consume as NuGet packages.
