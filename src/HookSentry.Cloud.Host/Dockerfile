FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files first for layer caching
COPY HookSentry.Cloud.slnx ./
COPY src/HookSentry.Cloud.Host/HookSentry.Cloud.Host.csproj src/HookSentry.Cloud.Host/
COPY src/HookSentry.Billing/HookSentry.Billing.csproj src/HookSentry.Billing/
COPY src/HookSentry.Subscriptions/HookSentry.Subscriptions.csproj src/HookSentry.Subscriptions/

# Copy OSS submodule project files
COPY hooksentry/hooksentry-api/src/HookSentry.Api/HookSentry.Api.csproj hooksentry/hooksentry-api/src/HookSentry.Api/
COPY hooksentry/hooksentry-api/src/HookSentry.Domain/HookSentry.Domain.csproj hooksentry/hooksentry-api/src/HookSentry.Domain/
COPY hooksentry/hooksentry-api/src/HookSentry.Infrastructure/HookSentry.Infrastructure.csproj hooksentry/hooksentry-api/src/HookSentry.Infrastructure/
COPY hooksentry/hooksentry-api/src/HookSentry.Worker/HookSentry.Worker.csproj hooksentry/hooksentry-api/src/HookSentry.Worker/

RUN dotnet restore src/HookSentry.Cloud.Host/HookSentry.Cloud.Host.csproj

# Copy all source
COPY src/ src/
COPY hooksentry/hooksentry-api/src/ hooksentry/hooksentry-api/src/

RUN dotnet publish src/HookSentry.Cloud.Host/HookSentry.Cloud.Host.csproj \
    -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "HookSentry.Cloud.Host.dll"]
