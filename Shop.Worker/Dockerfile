# Build context: repository root
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shop.Contracts/Shop.Contracts.csproj Shop.Contracts/
COPY Shop.Data/Shop.Data.csproj Shop.Data/
COPY Shop.Worker/Shop.Worker.csproj Shop.Worker/
RUN dotnet restore Shop.Worker/Shop.Worker.csproj

COPY Shop.Contracts/ Shop.Contracts/
COPY Shop.Data/ Shop.Data/
COPY Shop.Worker/ Shop.Worker/
RUN dotnet publish Shop.Worker/Shop.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Shop.Worker.dll"]
