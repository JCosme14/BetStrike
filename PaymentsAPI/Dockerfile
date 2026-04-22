# ============================================
# Build stage: compile the project
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PaymentsAPI/PaymentsAPI.csproj PaymentsAPI/
RUN dotnet restore PaymentsAPI/PaymentsAPI.csproj

COPY PaymentsAPI/ PaymentsAPI/
RUN dotnet publish PaymentsAPI/PaymentsAPI.csproj -c Release -o /app/publish --no-restore

# ============================================
# Runtime stage: slim image that only runs the app
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PaymentsAPI.dll"]
