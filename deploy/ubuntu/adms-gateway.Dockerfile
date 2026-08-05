FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/zkteco-adms-gateway/Vnta.AttendanceGateway.csproj src/zkteco-adms-gateway/

RUN dotnet restore src/zkteco-adms-gateway/Vnta.AttendanceGateway.csproj

COPY src/zkteco-adms-gateway/ src/zkteco-adms-gateway/

RUN dotnet publish src/zkteco-adms-gateway/Vnta.AttendanceGateway.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5005

EXPOSE 5005 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Vnta.AttendanceGateway.dll"]
