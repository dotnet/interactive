#!/bin/bash
###############################################################
#                Unofficial 'Bash strict mode'                #
# http://redsymbol.net/articles/unofficial-bash-strict-mode/  #
###############################################################
set -euo pipefail
IFS=$'\n\t'
###############################################################


#############################
### Install dependencies  ###
#############################
echo "Installing dependencies..."
sudo apt install -y libgdiplus libffi-dev zlib1g
echo ""


####################
### Install .NET ###
####################
echo "Installing .NET..."
curl -L https://dot.net/v1/dotnet-install.sh | bash -e
curl -L https://dot.net/v1/dotnet-install.sh | bash -e -s -- --channel 7.0
echo ""

echo "Updating PATH and DOTNET_ROOT environment variables..."
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

add_nuget_src https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json dotnet-public
add_nuget_src https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json dotnet-eng
add_nuget_src https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json dotnet-tools
add_nuget_src https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet3.1/nuget/v3/index.json dotnet3-dev
add_nuget_src https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json dotnet5
add_nuget_src https://pkgs.dev.azure.com/dnceng/public/_packaging/MachineLearning/nuget/v3/index.json MachineLearning
echo ""


################################
### Install .NET Interactive ###
################################

if echo "$(dotnet tool list -g)" | grep -q "microsoft.dotnet-interactive"; then
  echo ".NET Interactive installation found - updating..."
  command="update"
else
  echo ".NET Interactive installation not found - installing..."
  command="install"
fi

dotnet tool "${command}" -g --add-source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" Microsoft.dotnet-interactive
