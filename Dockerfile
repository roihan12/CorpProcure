FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CorpProcure.csproj", "./"]
RUN dotnet restore "CorpProcure.csproj"
COPY . .
RUN dotnet build "CorpProcure.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CorpProcure.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create uploads directory with correct permissions
RUN mkdir -p /app/wwwroot/uploads/attachments && \
    chmod -R 755 /app/wwwroot/uploads

# Switch to non-root user for security
USER $APP_UID

ENTRYPOINT ["dotnet", "CorpProcure.dll"]

