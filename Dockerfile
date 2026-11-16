# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Спочатку лише *.csproj — щоб кешувати шар restore
COPY Splitwise.Api/Splitwise.Api.csproj Splitwise.Api/
COPY Splitwise.BusinessLogic/Splitwise.BusinessLogic.csproj Splitwise.BusinessLogic/
COPY Splitwise.Domain/Splitwise.Domain.csproj Splitwise.Domain/
COPY Splitwise.Infrastructure/Splitwise.Infrastructure.csproj Splitwise.Infrastructure/
RUN dotnet restore Splitwise.Api/Splitwise.Api.csproj

COPY . .
RUN dotnet publish Splitwise.Api/Splitwise.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    PORT=8080
EXPOSE 8080

# Не запускаємо від root
USER $APP_UID
ENTRYPOINT ["dotnet", "Splitwise.Api.dll"]
