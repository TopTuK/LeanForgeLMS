# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LF.IdentityService/Lf.IdentityService.csproj LF.IdentityService/
COPY LF.Infrastructure/LF.Infrastructure.csproj LF.Infrastructure/
COPY LF.Application/LF.Application.csproj LF.Application/
COPY LF.AppDomain/LF.AppDomain.csproj LF.AppDomain/
COPY LeanForgeLMS.ServiceDefaults/LeanForgeLMS.ServiceDefaults.csproj LeanForgeLMS.ServiceDefaults/
RUN dotnet restore LF.IdentityService/Lf.IdentityService.csproj

COPY LF.IdentityService/ LF.IdentityService/
COPY LF.Infrastructure/ LF.Infrastructure/
COPY LF.Application/ LF.Application/
COPY LF.AppDomain/ LF.AppDomain/
COPY LeanForgeLMS.ServiceDefaults/ LeanForgeLMS.ServiceDefaults/
# LF.Infrastructure.csproj references this .proto directly, even though LF.IdentityService
# has no ProjectReference to LF.CourseService.
COPY LF.CourseService/Protos/ LF.CourseService/Protos/

RUN dotnet publish LF.IdentityService/Lf.IdentityService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
# Assembly is Lf.IdentityService.dll (lowercase "f") — matches the csproj filename exactly.
ENTRYPOINT ["dotnet", "Lf.IdentityService.dll"]
