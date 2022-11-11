# Introduction

[![Build and Test](https://github.com/jbijlsma/Dnw.RateLimiter/actions/workflows/build.yml/badge.svg)](https://github.com/jbijlsma/Dnw.RateLimiter/actions/workflows/build.yml)

Asp.net core example of a rate limiter with either a fixed- or sliding window. The code is loosely based on the examples
here:

https://developer.redis.com/develop/dotnet/aspnetcore/rate-limiting/fixed-window  
https://developer.redis.com/develop/dotnet/aspnetcore/rate-limiting/sliding-window  
https://developer.redis.com/develop/dotnet/aspnetcore/rate-limiting/middleware

# Testing locally

Start a redis instance locally (in docker):

```
docker run -dp 6379:6379 redis
```

Use your IDE to run the Dnw.RateLimiter.Api project or use the command line:

```
cd ./Dnw.RateLimiter.Api
dotnet run
```

You can use the RATE_LIMITER_TYPE environment variable to set the rate limiter that is used. See the RateLimiterType
enum for the options.

The default is the FixedWindow rate limiter. To use the SlidingWindow rate limiter use:

```
RATE_LIMITER_TYPE=SlidingWindow
dotnet run
```

If you are using Jetbrains Rider you can also set the RATE_LIMITER_TYPE environment variable in the run configuration
under environment variables.

To test the rate limiter using curl:

```
for n in {1..40}; do echo $(curl -s -w " HTTP %{http_code}, %{time_total} s" -X GET -H "Content-Length: 0" --user "foobar:password" https://localhost:5001/api/ratelimited/apikey); sleep 1; done
```

# Deploy to local k8s cluster (KinD)

The instructions below apply to running natively on apple silicon (M1 pro). To run on x64 you need to:

- Update the ./k8s/redis.yml file and use a x64 image. The current image used is: arm64v8/redis:latest
- Update the ./Dnw.RateLimiter.Api/Dockerfile. In the build phase you should not run 'dotnet restore' and 'dotnet publish' with the -r linux-musl-arm64 flag. Also the publish base image should not be an arm64 image. The current image is: aspnet:6.0-alpine-arm64v8.   

The ./k8s/deploy_local.sh script does everything:

- It creates a KinD cluster with a local image registry
- It builds & publishes the image for the api and pushes it to the local image registry
- It pre-loads images that are used in the k8s yml files 
- It deploys a redis instance and an api instance to the KinD cluster using helm

```
kind delete cluster (if a kind cluster already exists)

cd ./k8s
./deploy_local.sh

kubectl get po --all-namespaces (verify everything is running)

kubectl port-forward service/dnw-rate-limiter-api-service -n dnw-rate-limiter-api 5050:5050 (setup port forwarding to test the api)
```

The swagger is available under:

```
http://localhost:5050/swagger/index.html
```

To test the rate limiter using curl:

```
for n in {1..40}; do echo $(curl -s -w " HTTP %{http_code}, %{time_total} s" -X GET -H "Content-Length: 0" --user "foobar:password" http://localhost:5050/api/ratelimited/apikey); sleep 1; done
```

# Running the (integration) tests

The example contains both unit- and integration tests. Run them either from your favorite IDE or from the command line with:

```
dotnet test
```

A few notes on the integration tests:

- the TestContainers nuget package is used to spin up a separate redis docker container for each test
- TestContainers makes it easy to create a random host port mapping which allows running multiple containers that internally use the same port
- because the redis port is different for each test, in RateLimiterApiFactory.ConfigureWebHost the redis connection string is updated  
- alternatively you can spin up one container per integration test class using ClassFixture, but in this example this is not done 
- in xunit, tests within the same class never run in parallel, but tests in different classes can run in parallel
- because the redis connection string differs per redis container, the redis IConnectionMultiplexer singleton needs to be replaced. Without using a lazy singleton (one that is only created when it is first used) the incorrect connection string is used when starting up the web api and makes it crash.