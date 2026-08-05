FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG APPLICATION_VERSION=2026.07
ARG BUILD_NUMBER=0
ARG RELEASE_DATE=ChuaPhatHanh

COPY global.json ./
COPY src/Vnta.HRM2026/Directory.Packages.props src/Vnta.HRM2026/
COPY src/Vnta.HRM2026/Vnta.Hrm.Domain/Vnta.Hrm.Domain.csproj src/Vnta.HRM2026/Vnta.Hrm.Domain/
COPY src/Vnta.HRM2026/Vnta.Hrm.Application/Vnta.Hrm.Application.csproj src/Vnta.HRM2026/Vnta.Hrm.Application/
COPY src/Vnta.HRM2026/Vnta.Hrm.Infrastructure/Vnta.Hrm.Infrastructure.csproj src/Vnta.HRM2026/Vnta.Hrm.Infrastructure/
COPY src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Vnta.Hrm.Web.Client.csproj src/Vnta.HRM2026/Vnta.Hrm.Web.Client/
COPY src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj src/Vnta.HRM2026/Vnta.Hrm.Web/

RUN dotnet restore src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj

COPY src/Vnta.HRM2026/ src/Vnta.HRM2026/

RUN dotnet publish src/Vnta.HRM2026/Vnta.Hrm.Web/Vnta.Hrm.Web.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:ApplicationVersion=$APPLICATION_VERSION \
    /p:BuildNumber=$BUILD_NUMBER \
    /p:ReleaseDate=$RELEASE_DATE

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Vnta.Hrm.Web.dll"]
