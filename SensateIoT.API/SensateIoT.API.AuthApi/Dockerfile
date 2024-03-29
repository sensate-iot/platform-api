#
# Docker file for Authorization API.
#
# @author Michel Megens
# @email  michel@michelmegens.net
#

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

WORKDIR /build

COPY . .
RUN dotnet restore -r linux-musl-x64 SensateIoT.API.AuthApi/SensateIoT.API.AuthApi.csproj
RUN dotnet publish -c Release -o /app/bin -r linux-musl-x64 --no-restore --self-contained true SensateIoT.API.AuthApi/SensateIoT.API.AuthApi.csproj

FROM alpine:3.12

COPY --from=build-env /app /app
COPY SensateIoT.API.AuthApi/appsettings.json /app/appsettings.json
COPY SensateIoT.API.AuthApi/Templates /app/Templates
WORKDIR /app
RUN apk add --no-cache libstdc++ libintl icu

EXPOSE 80
ENTRYPOINT ["bin/SensateIoT.API.AuthApi", "--urls", "http://*:80"]
