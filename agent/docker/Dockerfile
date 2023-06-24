FROM ubuntu:20.04

# Debug
ENV VSTS_AGENT_HTTPTRACE=true

# See https://github.com/nvm-sh/nvm/releases
ARG NVM_VERSION=0.39.1

# See https://nodejs.org/download/release/latest-v14.x/
ARG NODE_VERSION=18.13.0

# See https://nodejs.org/download/release/latest-v14.x/
ARG DOTNET_VERSION=6.0

# See https://github.com/serverless/serverless/releases
ARG SERVERLESS_VERSION=latest

# see http://docs.docker.oeynet.com/release-notes/docker-ce/#stable-releases
ARG DOCKER_CE_VERSION=5:20.10.*

# see https://github.com/containerd/containerd/releases
ARG CONTAINERD_VERSION=1.6.*


# Make version available to Agent capabilities tab via env
ENV NVM_VERSION                         $NVM_VERSION
ENV NODE_VERSION                        $NODE_VERSION
ENV SERVERLESS_VERSION                  $SERVERLESS_VERSION
ENV DOCKER_CE_VERSION                   $DOCKER_CE_VERSION
ENV DOCKER_COMPOSE_VERSION              $DOCKER_COMPOSE_VERSION
ENV CONTAINERD_VERSION                  $CONTAINERD_VERSION
ENV DOTNET_VERSION                      $DOTNET_VERSION


ENV NVM_DIR   /usr/local/nvm
ENV NODE_PATH $NVM_DIR/versions/node/v$NODE_VERSION/lib/node_modules

ENV DEBIAN_FRONTEND noninteractive
RUN echo "APT::Get::Assume-Yes \"true\";" > /etc/apt/apt.conf.d/90assumeyes

# Update / Install base packages
RUN apt-get -y update \
    && apt-get -y install --no-install-recommends \
        apt-transport-https \
        build-essential \
        ca-certificates \
        curl \
        dos2unix \
        file \
        git \
        gnupg-agent \
        gnupg2 \
        iputils-ping \
        jq \
        libcurl4 \
        libssl1.0 \
        libunwind8 \
        moreutils \
        netcat \
        software-properties-common \
        ssh \
        sudo \
        tar \
        unzip \
        zip \
    && apt-get -q clean \
    && rm -rf /var/lib/apt/lists/*

# Add Node & NPM repos
RUN curl -sL https://deb.nodesource.com/setup_14.x | bash -

# Add hashicorp repo for Packer
RUN curl -fsSL https://apt.releases.hashicorp.com/gpg | apt-key add -
RUN apt-add-repository "deb [arch=amd64] https://apt.releases.hashicorp.com $(lsb_release -cs) main"

# Add docker repo
RUN curl -fsSL https://download.docker.com/linux/ubuntu/gpg | apt-key add -
RUN add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"

# Add the Microsoft repo
RUN curl "https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb" -o "/tmp/packages-microsoft-prod.deb" \
    && dpkg -i /tmp/packages-microsoft-prod.deb \
    && rm /tmp/packages-microsoft-prod.deb \
    && rm -rf /var/lib/apt/lists/*

# Add the azure-cli repo
RUN add-apt-repository "deb [arch=amd64] https://packages.microsoft.com/repos/azure-cli/ $(lsb_release -cs) main"

RUN apt-get -y update \
    && apt-get remove -y docker docker-engine docker.io containerd runc \
    && apt-get -y install --no-install-recommends \
        powershell \
        azure-cli \
        aspnetcore-runtime-$DOTNET_VERSION \
        docker-ce=$DOCKER_CE_VERSION \
        docker-ce-cli=$DOCKER_CE_VERSION \
        containerd.io \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# NodeJS and NPM via nvm
RUN mkdir -p /usr/local/nvm \
    && curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v$NVM_VERSION/install.sh | bash \
    && . $NVM_DIR/nvm.sh \
    && nvm install $NODE_VERSION \
    && nvm alias default $NODE_VERSION \
    && nvm use default

ENV PATH $NVM_DIR/versions/node/v$NODE_VERSION/bin:$PATH

# Install Serverless
RUN npm install -g serverless@$SERVERLESS_VERSION

# NodeJS and NPM via nvm
RUN mkdir -p /usr/local/nvm \
    && curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v$NVM_VERSION/install.sh | bash \
    && . $NVM_DIR/nvm.sh \
    && nvm install $NODE_VERSION \
    && nvm alias default $NODE_VERSION \
    && nvm use default

ENV PATH $NVM_DIR/versions/node/v$NODE_VERSION/bin:$PATH

# Install Serverless
RUN npm install -g serverless@$SERVERLESS_VERSION

# Configure Powershell: install az / PSDepend
RUN pwsh -c "Install-Module -Name PSDepend,az -Scope AllUsers -Force -Verbose"

# Configure Docker + Docker compose
RUN curl -L https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m) -o /usr/local/bin/docker-compose \
     && chmod +x /usr/local/bin/docker-compose \
     && usermod -a -G docker root \
     && systemctl enable docker

# Install AWS CLI
RUN curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "/tmp/awscliv2.zip" \
    && unzip /tmp/awscliv2.zip -d /tmp/awscli \
    && /tmp/awscli/aws/install -i /usr/local/aws-cli -b /usr/local/bin \
    && rm -rf /tmp/awscli*

# Install AWS Session Manager
RUN curl "https://s3.amazonaws.com/session-manager-downloads/plugin/latest/ubuntu_64bit/session-manager-plugin.deb" -o "/tmp/session-manager-plugin.deb" \
    && dpkg -i /tmp/session-manager-plugin.deb \
    && rm /tmp/session-manager-plugin.deb \
    && rm -rf /var/lib/apt/lists/*

# # Install dotnet-install.sh script to install dotnet sdk and runtime
# # https://unix.stackexchange.com/questions/321710/why-wont-curl-download-this-link-when-a-browser-will#:~:text=There%20is%20a%20redirect,the%20%2DL%20argument%3A
# RUN curl -L https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet/dotnet-install.sh \
#     && sudo chmod +x ./dotnet-install.sh \
#     && ./dotnet-install.sh --version 6.0

# Install dotnet runtime and sdk
RUN curl -L https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -o packages-microsoft-prod.deb \
    && sudo dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb

RUN sudo apt-get update && \
    sudo apt-get install -y dotnet-sdk-$DOTNET_VERSION

RUN sudo apt-get update && \
  sudo apt-get install -y dotnet-runtime-$DOTNET_VERSION

# NOTE: You will not need to do the aws config if you are hosting your agents in AWS. Instead use IAM roles. 
# This is only for running the agents in your local machine.
RUN aws configure set default.region eu-west-2 \
    && aws configure set aws_access_key_id '<aws_access_key_id>' \
    && aws configure set aws_secret_access_key '<aws_secret_access_key>'

WORKDIR /azp

COPY ./start.sh .
RUN chmod +x start.sh

ENTRYPOINT ["./start.sh"]
