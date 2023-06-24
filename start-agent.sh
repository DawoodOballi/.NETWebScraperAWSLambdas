#!/bin/bash

PROJECT="AWS-SERVERLESS"

# This script will be used to spin up the docker image via 'docker build' and then run the image to start the agent service.
SUFFIX=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 6 | head -n 1)
export AZURE_URL="https://dev.azure.com/<organisation>/"
export AZURE_PAT="<ADO_PAT>"
export AZURE_AGENT_NAME="$RANDOM-$PROJECT-$SUFFIX-$HOSTNAME-AGENT"
export AZP_POOL=$PROJECT

current_wd=`pwd`

# # go into the directory where you have your agent files (Dockerfile and start script file)
cd agent/docker/

# see https://github.com/moby/moby/issues/31702#:~:text=Looking%20at%20the,the%20above%20helps regarding the command below
dos2unix start.sh

docker build -t dockeragent:latest .

IMAGE=$(docker images --format "{{.Repository}}:{{.Tag}}" | grep dockeragent) # This gets the name of the image and the tag in the following format ImageName:Tag

docker run -d --name $AZURE_AGENT_NAME -e AZP_URL=$AZURE_URL -e AZP_TOKEN=$AZURE_PAT -e AZP_AGENT_NAME=$AZURE_AGENT_NAME $IMAGE > ../../dockerrun.log 2>&1

cd $current_wd

