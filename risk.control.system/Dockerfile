# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

ARG APP_UID=1000

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    libkrb5-3 \
    libicu-dev \
    fontconfig \
    && rm -rf /var/lib/apt/lists/*

# Install Microsoft TrueType Fonts (Arial, Times New Roman, etc.)
RUN apt-get update && \
    echo "ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true" | debconf-set-selections && \
    apt-get install -y --no-install-recommends ttf-mscorefonts-installer && \
    fc-cache -f -v && \
    rm -rf /var/lib/apt/lists/*
# Use root to prepare filesystem
USER root
WORKDIR /app

# Create Document folder and fix permissions
RUN mkdir -p /app/Document /app/DataProtection-Keys \
    && chown -R ${APP_UID}:${APP_UID} /app/Document /app/DataProtection-Keys \
    && chmod -R 775 /app/Document /app/DataProtection-Keys

# Switch back to non-root user
USER ${APP_UID}

EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["risk.control.system/risk.control.system.csproj", "risk.control.system/"]
RUN dotnet restore "./risk.control.system/risk.control.system.csproj"
COPY . .
WORKDIR "/src/risk.control.system"
RUN dotnet build "./risk.control.system.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./risk.control.system.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "risk.control.system.dll"]