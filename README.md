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

Use your IDE or run the Dnw.RateLimiter.Api project or on the commandline with:

```
cd ./Dnw.RateLimiter.Api
dotnet run
```

You can use the RATE_LIMITER_TYPE environment variable to set the rate limiter that is used. See the RateLimiterType
enum for the options.

The default is FixedWindow. To use the SlidingWindow rate limiter use:

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

# Gotchas

The integration tests in Dnw.RateLimiter.IntegrationTests do not mock the redis service and therefore they require a
redis instance running on the default port (6379). 