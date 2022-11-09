# Introduction

Asp.net core example of a FixedRateLimiter, loosely based on the examples here:

https://developer.redis.com/develop/dotnet/aspnetcore/rate-limiting/fixed-window

# Testing locally

Run the asp.net core project with:

```
dotnet run
```

To switch between the fixed- and sliding window rate limiters update Program.cs:

```csharp
// app.UseMiddleware<FixedWindowRateLimiter>();
app.UseMiddleware<SlidingWindowRateLimiter>();
```

To test the rate limiter using curl:
```
for n in {1..40}; do echo $(curl -s -w " HTTP %{http_code}, %{time_total} s" -X GET -H "Content-Length: 0" --user "foobar:password" https://localhost:5001/api/ratelimited/apikey); sleep 1; done
```