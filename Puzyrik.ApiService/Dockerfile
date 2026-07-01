# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Puzyrik.ApiService/Puzyrik.ApiService.csproj", "Puzyrik.ApiService/"]
COPY ["Puzyrik.ServiceDefaults/Puzyrik.ServiceDefaults.csproj", "Puzyrik.ServiceDefaults/"]
RUN dotnet restore "Puzyrik.ApiService/Puzyrik.ApiService.csproj"
COPY . .
RUN dotnet publish "Puzyrik.ApiService/Puzyrik.ApiService.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Cloud Run passes the port via $PORT (default 8080); tell ASP.NET to listen there.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Puzyrik.ApiService.dll"]