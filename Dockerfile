# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/AuthGate.Auth/AuthGate.Auth.csproj", "src/AuthGate.Auth/"]
COPY ["src/AuthGate.Auth.Domain/AuthGate.Auth.Domain.csproj", "src/AuthGate.Auth.Domain/"]
COPY ["src/AuthGate.Auth.Application/AuthGate.Auth.Application.csproj", "src/AuthGate.Auth.Application/"]
COPY ["src/AuthGate.Auth.Infrastructure/AuthGate.Auth.Infrastructure.csproj", "src/AuthGate.Auth.Infrastructure/"]

RUN dotnet restore "src/AuthGate.Auth/AuthGate.Auth.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/AuthGate.Auth"
RUN dotnet build "AuthGate.Auth.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AuthGate.Auth.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthGate.Auth.dll"]
