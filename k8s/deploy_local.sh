#!/bin/bash

# Define variables
RELEASE_NAME=dnw-rate-limiter-api

# Create the new cluster with a private container / image registry
echo "Create new KinD cluster"
. ./create_kind_cluster.sh

# Preload 3rd party images
docker pull mcr.microsoft.com/dotnet/sdk:6.0-alpine
kind load docker-image mcr.microsoft.com/dotnet/sdk:6.0-alpine

docker pull mcr.microsoft.com/dotnet/aspnet:6.0-alpine
kind load docker-image mcr.microsoft.com/dotnet/aspnet:6.0-alpine

docker pull arm64v8/redis:latest
kind load docker-image arm64v8/redis:latest

# Build local image, tag it and push it to the local registry
TAG="localhost:5001/$RELEASE_NAME:latest"
echo "TAG=$TAG"
docker build -t $TAG -f ../Dnw.RateLimiter.Api/Dockerfile ../Dnw.RateLimiter.Api
docker push $TAG

# Install app into k8s cluster
helm upgrade "$RELEASE_NAME" ./helm --install --namespace "$RELEASE_NAME" --create-namespace

# Restart the deployment
DEPLOYMENT="deployment/$RELEASE_NAME-deployment"
echo "DEPLOYMENT=$DEPLOYMENT"
kubectl rollout restart "deployment/$RELEASE_NAME-deployment" -n "$RELEASE_NAME"