###########
# Runtime #
###########
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8080

#########
# Build #
#########
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files for better restore caching
COPY ["./AuthGate.sln", "./"]
COPY ["src/AuthGate.Auth/AuthGate.Auth.csproj", "src/AuthGate.Auth/"]
COPY ["src/AuthGate.Auth.Application/AuthGate.Auth.Application.csproj", "src/AuthGate.Auth.Application/"]
COPY ["src/AuthGate.Auth.Domain/AuthGate.Auth.Domain.csproj", "src/AuthGate.Auth.Domain/"]
COPY ["src/AuthGate.Auth.Infrastructure/AuthGate.Auth.Infrastructure.csproj", "src/AuthGate.Auth.Infrastructure/"]

RUN dotnet restore "./AuthGate.sln"

# Copy the rest of the source
COPY . .

WORKDIR "/src/src/AuthGate.Auth"
RUN dotnet build "./AuthGate.Auth.csproj" -c ${BUILD_CONFIGURATION} -o /app/build

###########
# Publish #
###########
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AuthGate.Auth.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

###########
# Final   #
###########
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthGate.Auth.dll"]