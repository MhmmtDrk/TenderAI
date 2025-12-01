FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["TenderAI.Web/TenderAI.Web.csproj", "TenderAI.Web/"]
COPY ["TenderAI.Core/TenderAI.Core.csproj", "TenderAI.Core/"]
COPY ["TenderAI.Infrastructure/TenderAI.Infrastructure.csproj", "TenderAI.Infrastructure/"]
COPY ["TenderAI.Domain/TenderAI.Domain.csproj", "TenderAI.Domain/"]

# Restore dependencies
RUN dotnet restore "TenderAI.Web/TenderAI.Web.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR "/src/TenderAI.Web"
RUN dotnet build "TenderAI.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TenderAI.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TenderAI.Web.dll"]
