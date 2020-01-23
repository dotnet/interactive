# Share your notebooks on [Binder](https://mybinder.org/)

## How to share your .NET Jupyter Notebook 
If you want to share notebooks you have made using the .NET Jupyter kernel, one easy way is to generate a Binder image that anyone can run on the web.

### Prerequisites: 

* A GitHub repo and at least one notebook to share
* **Dockerfile** to create the Binder image
* A **Nuget.Config** file to provide package sources needed by your notebooks

You can use the Dockerfile and Nuget.Config files from the folder `Binder Dependencies` to get started.

### Setup instructions

The repo file structure should look something like this:

<img src ="https://user-images.githubusercontent.com/375556/67017073-19137180-f0f1-11e9-9744-b5f8ec532e32.png" width = "30%">

The Dockerfile will install the .NET SDK, then copy the notebooks and Nuget.config to the notebooks folder.

```docker
# Copy notebooks

COPY ./notebooks/ ${HOME}/notebooks/

# Copy package sources

COPY ./NuGet.config ${HOME}/nuget.config

RUN chown -R ${NB_UID} ${HOME}
USER ${USER}
```

Now push your changes to [github](https://github.com/).

Open a browser to [Binder](https://mybinder.org/).

<img src ="https://user-images.githubusercontent.com/375556/67016428-16fce300-f0f0-11e9-98e7-d066ecb91049.png" width="70%">

Enter your repository URL and branch.

<img src = "https://user-images.githubusercontent.com/375556/67016633-66dbaa00-f0f0-11e9-8a6d-c7191de3142e.png" width="70%">

Press **launch** to test your Binder.

During development it is useful to use a commit hash so that you can test different commits at the same time.

When you're happy with the result, expand the section to reveal the badge code, which you can embed in your blogs and posts.

<img src = "https://user-images.githubusercontent.com/375556/67016821-bd48e880-f0f0-11e9-8c79-4fc97a06741a.png" width = "70%">

## Start in Jupyter Lab 

By default, Binder will start with the Jupyter Notebook frontend. If you prefer to use JupyterLab, just add the query parameter `?urlpath=lab` to the URL in your badge.

For example, change this:

```[![Binder](https://mybinder.org/badge_logo.svg)](https://mybinder.org/v2/gh/dotnet/try/master)```

into this:

```[![Binder](https://mybinder.org/badge_logo.svg)](https://mybinder.org/v2/gh/dotnet/try/master?urlpath=lab)```

Return to [README.md](README.md)
