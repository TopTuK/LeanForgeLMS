# syntax=docker/dockerfile:1

########################################
# Stage 1: build the Vue SPA
########################################
FROM node:22-bookworm-slim AS node-build
WORKDIR /src/lf.webapp

COPY lf.webapp/package.json lf.webapp/package-lock.json ./
# package-lock.json was generated on Windows and doesn't pin the Linux-target
# native-binary variants for Tailwind v4's lightningcss/oxide engine, so `npm ci`
# rejects it here even though it's valid on the host. `npm install` resolves
# fresh against package.json instead of requiring an exact lockfile match.
RUN npm install

COPY lf.webapp/ ./
# vite.config.js: build.outDir = '../LF.WebApi/wwwroot' (relative to this WORKDIR)
RUN npm run build

########################################
# Stage 2: restore & publish the .NET app
########################################
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /src

COPY LF.WebApi/LF.WebApi.csproj LF.WebApi/
COPY LF.Infrastructure/LF.Infrastructure.csproj LF.Infrastructure/
COPY LF.Application/LF.Application.csproj LF.Application/
COPY LF.AppDomain/LF.AppDomain.csproj LF.AppDomain/
COPY LeanForgeLMS.ServiceDefaults/LeanForgeLMS.ServiceDefaults.csproj LeanForgeLMS.ServiceDefaults/
RUN dotnet restore LF.WebApi/LF.WebApi.csproj

COPY LF.WebApi/ LF.WebApi/
COPY LF.Infrastructure/ LF.Infrastructure/
COPY LF.Application/ LF.Application/
COPY LF.AppDomain/ LF.AppDomain/
COPY LeanForgeLMS.ServiceDefaults/ LeanForgeLMS.ServiceDefaults/
# LF.Infrastructure.csproj references these .proto files directly, even though LF.WebApi
# has no ProjectReference to LF.IdentityService or LF.CourseService.
COPY LF.IdentityService/Protos/ LF.IdentityService/Protos/
COPY LF.CourseService/Protos/ LF.CourseService/Protos/

# SPA output, so the SDK's default wwwroot/** content globbing picks it up on publish.
COPY --from=node-build /src/LF.WebApi/wwwroot/ LF.WebApi/wwwroot/

RUN dotnet publish LF.WebApi/LF.WebApi.csproj -c Release -o /app/publish --no-restore

########################################
# Stage 3: runtime image
########################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=dotnet-build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "LF.WebApi.dll"]
