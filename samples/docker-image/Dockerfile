FROM mcr.microsoft.com/dotnet/sdk:5.0.100-focal

ARG HTTP_PORT_RANGE=1100-1200

# Opt out of telemetry until after we install jupyter when building the image, this prevents caching of machine id
ENV DOTNET_INTERACTIVE_CLI_TELEMETRY_OPTOUT=true

# Install all OS dependencies for notebook server that starts but lacks all
# features (e.g., download as all possible file formats)

ENV DEBIAN_FRONTEND noninteractive
RUN apt-get update \
 && apt-get install -yq --no-install-recommends \
    wget \
    bzip2 \
    ca-certificates \
    sudo \
    locales \
    fonts-liberation \
    run-one \
    python3.8 \
    python3-pip \
 && apt-get clean && rm -rf /var/lib/apt/lists/*

RUN echo "en_US.UTF-8 UTF-8" > /etc/locale.gen && \
    locale-gen

RUN python3 -m pip install setuptools
RUN python3 -m pip install jupyter
RUN python3 -m pip install jupyterlab

# Install lastest build from master branch of Microsoft.DotNet.Interactive
RUN dotnet tool install --tool-path /usr/share/dotnet-interactive Microsoft.dotnet-interactive --add-source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json"
RUN ln -s /usr/share/dotnet-interactive/dotnet-interactive /usr/bin/dotnet-interactive
RUN dotnet interactive jupyter install --http-port-range ${HTTP_PORT_RANGE}

# Enable telemetry once we install jupyter for the image
ENV DOTNET_INTERACTIVE_CLI_TELEMETRY_OPTOUT=false


EXPOSE 8888
EXPOSE ${HTTP_PORT_RANGE}

RUN mkdir notebooks

WORKDIR notebooks

ENTRYPOINT jupyter lab --ip=0.0.0.0  --allow-root  --notebook-dir=/notebooks/