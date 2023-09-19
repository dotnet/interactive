# Using Python and R in Polyglot Notebooks 

Polyglot notebooks is now offering Jupyter support, meaning you can use Python and R in your polyglot notebook along with previously supported languages like C#, JavaScript, and SQL. 

This feature is currently in preview.

## Setup
Before you begin, make sure you have the following installed:
1. [The Anaconda distribution](https://docs.anaconda.com/free/anaconda/install/index.html)
2. [Python](https://www.python.org/downloads/) and/or [R](https://cran.r-project.org/)

## Connecting to a Python kernel
Run the following command in a notebook cell:
```
#!connect jupyter --kernel-name pythonkernel --conda-env base --kernel-spec python3
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

Then restart VS Code from the Anaconda prompt, and run the following command in a notebook cell:
```
#!connect jupyter --kernel-name Rkernel --conda-env base --kernel-spec ir
```

Once connected, create a new cell and select your R kernel from the kernel picker.

## Connecting to a remote Jupyter server. 
To connect to a remote Jupyter server, run this command in a notebook cell:
```
#!connect jupyter --url <url_for_jupyter> --token <token_you_used_for_jupyter> --kernel-name pythonkernel --kernel-spec python3
```
For R, run the same command but replace `python3` with `ir` under `--kernel-spec` and give a new name for `kernel-name`.