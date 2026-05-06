# Use the official .NET 10 SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["RMS.Web/RMS.Web.csproj", "RMS.Web/"]
RUN dotnet restore "RMS.Web/RMS.Web.csproj"

# Copy the entire source and build
COPY . .
WORKDIR "/src/RMS.Web"
RUN dotnet build "RMS.Web.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "RMS.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Expose the default ASP.NET Core port
EXPOSE 8080

# Environment variables
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Admin__Email=admin@rms.com
ENV Admin__Password=Admin@123

# Copy the published output from the publish stage
COPY --from=publish /app/publish .

# Create a data directory for SQLite persistence (optional but recommended for volume mounting)
RUN mkdir -p /app/data
# Ensure the application points to the data directory for the database
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/rms.db"

# Set the entry point
ENTRYPOINT ["dotnet", "RMS.Web.dll"]
