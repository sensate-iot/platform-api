#
# Docker file for the Data API.
#
# @author Michel Megens
# @email  michel@michelmegens.net
#

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

WORKDIR /build

COPY . .
RUN dotnet restore -r linux-musl-x64 SensateIoT.API.DataApi/SensateIoT.API.DataApi.csproj
RUN dotnet publish -c Release -o /app/bin -r linux-musl-x64 --no-restore --self-contained true  SensateIoT.API.DataApi/SensateIoT.API.DataApi.csproj

FROM alpine:3.12

COPY --from=build-env /app /app
COPY SensateIoT.API.DataApi/appsettings.json /app/appsettings.json
WORKDIR /app
RUN apk add --no-cache libstdc++ libintl icu

EXPOSE 80
ENTRYPOINT ["bin/SensateIoT.API.DataApi", "--urls", "http://*:80"]
