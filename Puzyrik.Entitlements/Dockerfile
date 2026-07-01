FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Puzyrik.Entitlements/Puzyrik.Entitlements.csproj", "Puzyrik.Entitlements/"]
COPY ["Puzyrik.ServiceDefaults/Puzyrik.ServiceDefaults.csproj", "Puzyrik.ServiceDefaults/"]
RUN dotnet restore "Puzyrik.Entitlements/Puzyrik.Entitlements.csproj"
COPY . .
RUN dotnet publish "Puzyrik.Entitlements/Puzyrik.Entitlements.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Puzyrik.Entitlements.dll"]