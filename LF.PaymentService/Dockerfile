# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LF.PaymentService/LF.PaymentService.csproj LF.PaymentService/
COPY LF.Infrastructure/LF.Infrastructure.csproj LF.Infrastructure/
COPY LF.Application/LF.Application.csproj LF.Application/
COPY LF.AppDomain/LF.AppDomain.csproj LF.AppDomain/
COPY LeanForgeLMS.ServiceDefaults/LeanForgeLMS.ServiceDefaults.csproj LeanForgeLMS.ServiceDefaults/
RUN dotnet restore LF.PaymentService/LF.PaymentService.csproj

COPY LF.PaymentService/ LF.PaymentService/
COPY LF.Infrastructure/ LF.Infrastructure/
COPY LF.Application/ LF.Application/
COPY LF.AppDomain/ LF.AppDomain/
COPY LeanForgeLMS.ServiceDefaults/ LeanForgeLMS.ServiceDefaults/
# LF.Infrastructure.csproj references these .proto files directly as gRPC clients,
# even though LF.PaymentService has no ProjectReference to those services.
COPY LF.IdentityService/Protos/ LF.IdentityService/Protos/
COPY LF.CourseService/Protos/ LF.CourseService/Protos/

RUN dotnet publish LF.PaymentService/LF.PaymentService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "LF.PaymentService.dll"]
