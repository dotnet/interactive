# Using Python and R in Polyglot Notebooks 

Polyglot notebooks is now offering Jupyter support, meaning you can use Python and R in your polyglot notebook along with previously supported languages like C#, JavaScript, and SQL. 

This feature is currently in preview.

## Setup
Before you begin, make sure you have the following installed:
1. [The Anaconda distribution](https://docs.anaconda.com/free/anaconda/install/index.html). Comes with Python and Jupyter.
2. OR Install [Python](https://www.python.org/downloads/) and add to your PATH. You would need to install [Jupyter](https://jupyter.org/install#jupyter-notebook)
3. If you are working with R - Install [R](https://cran.r-project.org/)

## Connecting to a Python kernel
Run the following command in a notebook cell:
If working with Anaconda:
```
#!connect jupyter --kernel-name pythonkernel --conda-env base --kernel-spec python3
```

If working with Python and Jupyter directly:
```
#!connect jupyter --kernel-name pythonkernel --kernel-spec python3
```

Once connected, create a new cell and select your Python kernel from the kernel picker in the bottom right hand corner of the cell.

## Connecting to an R kernel
First, ensure that R is added to Jupyter. If not, switch to the Anaconda Prompt, and run this command:
```console
conda install -c r r-irkernel
```
Check to see if your R kernel appears in the Jupyter kernel spec list. If not, add your R kernel to Jupyter by running these commands in the R console:
```
install.packages('IRkernel')
IRkernel::installspec() 
```

You might need to restart VSCode.
If working with Anaconda, run the following command in a notebook cell:
```
#!connect jupyter --kernel-name Rkernel --conda-env base --kernel-spec ir
```
If working with Jupyter directly:
```
#!connect jupyter --kernel-name Rkernel --kernel-spec ir
```

Once connected, create a new cell and select your R kernel from the kernel picker.

## Connecting to a remote Jupyter server. 
To connect to a remote Jupyter server, run this command in a notebook cell:
```
#!connect jupyter --url <url_for_jupyter> --token <token_you_used_for_jupyter> --kernel-name pythonkernel --kernel-spec python3
```
For R, run the same command but replace `python3` with `ir` under `--kernel-spec` and give a new name for `kernel-name`.


## Using Virtual environments 

Both with Python venv and Conda environments, you can create the environments and add them to Jupyter as a kernel spec. 

For Python venv, run the following commands in the terminal:
```
python3 -m venv myenv
myenv\Scripts\activate

pip install ipykernel
python -m ipykernel install --user --name=myenv
```

For Conda, run the following commands in the terminal or Anaconda Bash Prompt (Windows):
```
conda create -n myenv 
conda activate myenv

conda install ipykernel
python -m ipykernel install --user --name=myenv
```

These environments can then be accessible as a kernel-spec in `#connect` command. 

Additionally, for Conda environments, you can use the `--conda-env` option in the `#connect` command to use the environment.

For example, if you create a conda environment `condaenvpython3.9` to use the python==3.9 version:
```
conda create -n condaenvpython3.9 python==3.9
conda activate condaenvpython3.9

conda install ipykernel
python -m ipykernel install --user --name=condaenvpython3.9
```

You can target it using the following command and be able to use python==3.9 in your notebook.
```
#!connect jupyter --kernel-name pythonkernel --conda-env condaenvpython3.9 --kernel-spec python3
```

Or, you can get similar experience by adding `condaenvpython3.9` as a kernel spec to Jupyter and then using the `--kernel-spec` option to target it.

```
#!connect jupyter --kernel-name pythonkernel --conda-env base --kernel-spec condaenvpython3.9
```