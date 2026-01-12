# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies (layer-caching)
COPY ["src/AuthGate.Auth/AuthGate.Auth.csproj", "src/AuthGate.Auth/"]
COPY ["src/AuthGate.Auth.Domain/AuthGate.Auth.Domain.csproj", "src/AuthGate.Auth.Domain/"]
COPY ["src/AuthGate.Auth.Application/AuthGate.Auth.Application.csproj", "src/AuthGate.Auth.Application/"]
COPY ["src/AuthGate.Auth.Infrastructure/AuthGate.Auth.Infrastructure.csproj", "src/AuthGate.Auth.Infrastructure/"]

RUN dotnet restore "src/AuthGate.Auth/AuthGate.Auth.csproj"

# Copy everything else and build/publish
COPY . .
WORKDIR "/src/src/AuthGate.Auth"
RUN dotnet publish "AuthGate.Auth.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Security: run as non-root
RUN addgroup --system dotnetapp && adduser --system --ingroup dotnetapp dotnetapp
ENV AUTHGATE_HOME=/app/AuthGate
RUN mkdir -p /app/AuthGate /app/AuthGate/Data && chown -R dotnetapp:dotnetapp /app/AuthGate
USER dotnetapp

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build --chown=dotnetapp:dotnetapp /app/publish ./
ENTRYPOINT ["dotnet", "AuthGate.Auth.dll"]
