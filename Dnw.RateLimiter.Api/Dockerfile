# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY ./*.csproj .
RUN dotnet restore -r linux-musl-arm64

# copy everything else and build app
COPY . .
RUN dotnet publish -c release -o /app -r linux-musl-arm64 --self-contained false --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine-arm64v8
WORKDIR /app
COPY --from=build /app ./

# configure kestrel listening port
ENV ASPNETCORE_URLS=http://+:5050
ENV ASPNETCORE_ENVIRONMENT=Production
ENV CLEAR_REDIS_DATABASE=True

# this should be the dll
ENTRYPOINT ["./Dnw.RateLimiter.Api"]