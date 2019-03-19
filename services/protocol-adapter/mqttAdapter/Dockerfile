FROM microsoft/dotnet:2.2-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY protocol-adapter/mqttAdapter/mqttAdapter.csproj protocol-adapter/mqttAdapter/
RUN dotnet restore protocol-adapter/mqttAdapter/mqttAdapter.csproj
COPY . .
WORKDIR /src/protocol-adapter/mqttAdapter
RUN dotnet build mqttAdapter.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish mqttAdapter.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "mqttAdapter.dll"]
