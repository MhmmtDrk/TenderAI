FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["TenderAI.DataCollector/TenderAI.DataCollector.csproj", "TenderAI.DataCollector/"]
COPY ["TenderAI.Core/TenderAI.Core.csproj", "TenderAI.Core/"]
COPY ["TenderAI.Infrastructure/TenderAI.Infrastructure.csproj", "TenderAI.Infrastructure/"]
COPY ["TenderAI.Domain/TenderAI.Domain.csproj", "TenderAI.Domain/"]

# Restore dependencies
RUN dotnet restore "TenderAI.DataCollector/TenderAI.DataCollector.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR "/src/TenderAI.DataCollector"
RUN dotnet build "TenderAI.DataCollector.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TenderAI.DataCollector.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TenderAI.DataCollector.dll"]
