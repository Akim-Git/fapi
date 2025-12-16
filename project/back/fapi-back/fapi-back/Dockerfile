# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# zkopíruju csproj a obnovím závislosti
COPY fapi-back.csproj ./
RUN dotnet restore

# zkopíruju zbytek kódu a sestavím aplikaci
COPY . ./
RUN dotnet publish -c Release -o /out

# -------- RUNTIME STAGE --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# zkopíruju výstup z build fáze
COPY --from=build /out ./

# Render nasazuje aplikaci na portu 10000
ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

# spustím aplikaci
ENTRYPOINT ["dotnet", "fapi-back.dll"]
