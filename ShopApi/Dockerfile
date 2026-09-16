# Build context: repository root
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shop.Contracts/Shop.Contracts.csproj Shop.Contracts/
COPY Shop.Data/Shop.Data.csproj Shop.Data/
COPY ShopApi/ShopApi.csproj ShopApi/
RUN dotnet restore ShopApi/ShopApi.csproj

COPY Shop.Contracts/ Shop.Contracts/
COPY Shop.Data/ Shop.Data/
COPY ShopApi/ ShopApi/
RUN dotnet publish ShopApi/ShopApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ShopApi.dll"]
