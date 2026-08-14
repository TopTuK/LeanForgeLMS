# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LF.CourseService/LF.CourseService.csproj LF.CourseService/
COPY LF.Infrastructure/LF.Infrastructure.csproj LF.Infrastructure/
COPY LF.Application/LF.Application.csproj LF.Application/
COPY LF.AppDomain/LF.AppDomain.csproj LF.AppDomain/
COPY LeanForgeLMS.ServiceDefaults/LeanForgeLMS.ServiceDefaults.csproj LeanForgeLMS.ServiceDefaults/
RUN dotnet restore LF.CourseService/LF.CourseService.csproj

COPY LF.CourseService/ LF.CourseService/
COPY LF.Infrastructure/ LF.Infrastructure/
COPY LF.Application/ LF.Application/
COPY LF.AppDomain/ LF.AppDomain/
COPY LeanForgeLMS.ServiceDefaults/ LeanForgeLMS.ServiceDefaults/
# LF.Infrastructure.csproj references this .proto directly, even though LF.CourseService
# has no ProjectReference to LF.IdentityService.
COPY LF.IdentityService/Protos/ LF.IdentityService/Protos/

RUN dotnet publish LF.CourseService/LF.CourseService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "LF.CourseService.dll"]
