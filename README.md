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
- adding to IConnectionMultiplexer singleton previously failed because of an incorrect initial redis connection string. It was fixed by using a lazy singleton. 