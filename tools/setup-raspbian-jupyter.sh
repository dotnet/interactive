#!/bin/bash
###############################################################
#                Unofficial 'Bash strict mode'                #
# http://redsymbol.net/articles/unofficial-bash-strict-mode/  #
###############################################################
set -euo pipefail
IFS=$'\n\t'
###############################################################


##########################################################
### Install virtualenv ###
##########################################################
echo "Installing virtualenv..."
sudo apt install -y virtualenv
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
