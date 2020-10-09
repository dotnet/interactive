#!/bin/bash -e

##########################################################
### Install System.Drawing dependencies and virtualenv ###
##########################################################
echo "Installing dependencies..."
sudo apt install -y libgdiplus virtualenv build-essential libssl-dev libffi-dev python-dev
echo ""


####################
### Install .NET ###
####################
echo "Installing .NET..."
curl -L https://dot.net/v1/dotnet-install.sh | bash -e
echo ""

echo "Updating PATH, DOTNET_ROOT environment variables..."
if ! grep -q ".NET Core SDK tools" "/home/pi/.bashrc"; then
  cat << \EOF >> "/home/pi/.bashrc"
# .NET Core SDK tools
export PATH=${PATH}:/home/pi/.dotnet
export PATH=${PATH}:/home/pi/.dotnet/tools
export DOTNET_ROOT=/home/pi/.dotnet
EOF
fi
export PATH=${PATH}:/home/pi/.dotnet
export PATH=${PATH}:/home/pi/.dotnet/tools
export DOTNET_ROOT=/home/pi/.dotnet
echo ""


echo "Installing NuGet sources..."
nugetListSource="$(dotnet nuget list source)"

add_nuget_src() {
  local packageSourcePath="${1}"
  local name="${2}"

  if echo "${nugetListSource}" | grep -q "${packageSourcePath}"; then
    dotnet nuget update source "${name}" -s "${packageSourcePath}"
  else
    dotnet nuget add source "${packageSourcePath}" -n "${name}"
  fi
}

mkdir -p "/home/pi/localNuget"
add_nuget_src "/home/pi/localNuget" local
add_nuget_src https://api.nuget.org/v3/index.json nuget.org
add_nuget_src https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json dotnet-tools
add_nuget_src https://www.powershellgallery.com/api/v2/ PSGallery
echo ""


################################
### Install .NET interactive ###
################################

dotnetToolList="$(dotnet tool list -g)"

if echo "${dotnetToolList}" | grep -q "microsoft.dotnet-interactive"; then
  echo ".NET Interactive installation found - updating..."
  dotnet tool update -g --add-source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" Microsoft.dotnet-interactive
else
  echo ".NET Interactive installation not found - installing..."
  dotnet tool install -g --add-source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" Microsoft.dotnet-interactive
fi
echo ""

#######################
### Install Jupyter ###
#######################
cd

# Create virtual env
echo "Installing Jupyter: creating virtualenv..."
rm -rf .jupyter_venv || true
virtualenv .jupyter_venv -p python3
source .jupyter_venv/bin/activate

# Inside the virtual env: Install jupyter
echo "virtualenv: pip install jupyter jupyterlab..."
pip3 install jupyter jupyterlab

# Inside the virtual env: Install .NET kernel
echo "virtualenv: install .NET kernel..."
dotnet interactive jupyter install

deactivate
