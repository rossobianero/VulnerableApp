FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY VulnerableApp/VulnerableApp.csproj VulnerableApp/
RUN dotnet restore VulnerableApp/VulnerableApp.csproj
COPY . .
WORKDIR /src/VulnerableApp
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER appuser
ENTRYPOINT ["dotnet", "VulnerableApp.dll"]
USER appuser
