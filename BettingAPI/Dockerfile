# ============================================
# Build stage: compile the project
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the csproj first so restore is cached when only source changes
COPY BettingAPI/BettingAPI.csproj BettingAPI/
RUN dotnet restore BettingAPI/BettingAPI.csproj

# Copy the rest of the source and publish
COPY BettingAPI/ BettingAPI/
RUN dotnet publish BettingAPI/BettingAPI.csproj -c Release -o /app/publish --no-restore

# ============================================
# Runtime stage: slim image that only runs the app
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy compiled output from build stage
COPY --from=build /app/publish .

# Kestrel will listen on this port inside the container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BettingAPI.dll"]
