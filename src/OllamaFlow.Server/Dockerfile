#
#
# Run the docker build command from src, not from the project directory
#
#


#
#
# Build stage
#
#
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy source
COPY . .

# Restore dependencies
RUN dotnet restore "OllamaFlow.Server/OllamaFlow.Server.csproj"

# Build the application
WORKDIR /src/OllamaFlow.Server
RUN dotnet build "OllamaFlow.Server.csproj" -c Release -o /app/build /p:GeneratePackageOnBuild=false


#
#
# Publish stage
#
#
FROM build AS publish
RUN dotnet publish "OllamaFlow.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:GeneratePackageOnBuild=false /p:ErrorOnDuplicatePublishOutputFiles=false


#
#
# Runtime stage
#
#
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
RUN apt-get update && apt-get install -y iputils-ping traceroute net-tools curl wget dnsutils iproute2 vim file && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 43411
ENTRYPOINT ["dotnet", "OllamaFlow.Server.dll"]
