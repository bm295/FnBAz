# FnB Manager (.NET 10)

This repository contains a Food & Beverage management web application built with ASP.NET Core MVC and Entity Framework Core.

## Features

- Dashboard metrics (menu/inventory/active orders)
- Menu management
- Inventory management
- Order management with status workflow
- Azure App Service deployment-ready structure
- Configurable SQLite/Azure SQL persistence with migration-based Azure SQL startup
- Microsoft Entra authentication through Azure App Service

## Run locally

```bash
dotnet restore
dotnet run
```

Open `https://localhost:5001` or the URL shown in the console.

## Azure free account deployment

The cheapest Azure path for this repo is Azure App Service Free F1 with SQLite and Azure App Service Authentication. Authentication is handled by Azure before requests reach the app, so no local login code is required.

The fixed free domain for these instructions is:

```text
https://fnbmanager-77700.azurewebsites.net
```

This README uses `fnbmanager-77700` as the fixed app name. Your URL stays the same while the App Service exists.

Mapping your own domain, such as `www.example.com`, requires a paid App Service plan. To stay on the free tier, use the fixed `azurewebsites.net` domain.

Prerequisites:

- Azure free account with an active subscription
- Azure CLI installed locally, or Azure Cloud Shell
- .NET 10 SDK installed if you publish from your machine

### First-time deployment

From the repository root, run the following commands.

```powershell
az login

$resourceGroup = "rg-fnbmanager-free"
$location = "southeastasia"
$appName = "fnbmanager-77700"
$appUrl = "https://$appName.azurewebsites.net"

az group create `
  --name $resourceGroup `
  --location $location

az webapp up `
  --resource-group $resourceGroup `
  --location $location `
  --name $appName `
  --sku F1 `
  --os-type Windows
```

Configure the app to use SQLite in App Service's persistent home directory:

```powershell
az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $appName `
  --settings `
    ASPNETCORE_ENVIRONMENT=Production `
    Database__Provider=Sqlite `
    "ConnectionStrings__DefaultConnection=Data Source=D:\home\fnbmanager.db"

az webapp restart `
  --resource-group $resourceGroup `
  --name $appName
```

Enable Microsoft Entra authentication before sharing the URL:

1. Open the Azure portal.
2. Go to Resource groups > `rg-fnbmanager-free` > `fnbmanager-77700`.
3. Open Authentication.
4. Select Add identity provider.
5. For Identity provider, select Microsoft.
6. For Tenant type, select Workforce configuration, current tenant.
7. For App registration type, select Create new app registration.
8. For Supported account types, select Current tenant, single tenant.
9. In Additional checks, keep the recommended current-tenant/application checks.
10. In App Service authentication settings, set:
    - Authentication: Require authentication
    - Unauthenticated requests: HTTP 302 Found redirect
    - Token store: enabled
11. Select Add.

Open the fixed app URL:

```powershell
Start-Process $appUrl
```

Or open:

```text
https://fnbmanager-77700.azurewebsites.net
```

You should be redirected to Microsoft sign-in. Only users in your Microsoft Entra tenant can access the app.

### Deploy next times

For later code changes, keep the same `$resourceGroup` and `$appName`, then redeploy from the repository root:

```powershell
$resourceGroup = "rg-fnbmanager-free"
$appName = "fnbmanager-77700"

az webapp up `
  --resource-group $resourceGroup `
  --name $appName `
  --sku F1 `
  --os-type Windows
```

The URL remains:

```text
https://fnbmanager-77700.azurewebsites.net
```

Useful diagnostics:

```powershell
az webapp log tail `
  --resource-group $resourceGroup `
  --name $appName
```

### Delete when no longer needed

Deleting the resource group removes the App Service, App Service plan, and the SQLite database stored in App Service. The fixed `azurewebsites.net` domain is released after deletion, so another Azure user could later take the same app name.

```powershell
$resourceGroup = "rg-fnbmanager-free"

az group delete `
  --name $resourceGroup `
  --yes
```

The Microsoft Entra app registration created by App Service Authentication is separate from the resource group. To remove it:

1. Open the Microsoft Entra admin center.
2. Go to Identity > Applications > App registrations.
3. Search for `fnbmanager-77700`.
4. Open the app registration and select Delete.

### Azure SQL with managed identity

For Azure SQL, use Microsoft Entra authentication from the App Service managed identity. This avoids storing a SQL username/password in App Service settings.

Create or select an Azure SQL Database first. If you want to stay within a free-account experiment, use the Azure SQL free database offer in the Azure portal. If you create a database by CLI, confirm the selected SKU and cost before running the command.

```powershell
$resourceGroup = "rg-fnbmanager-free"
$location = "southeastasia"
$appName = "fnbmanager-77700"
$sqlServerName = "fnbmanager-sql-77700"
$databaseName = "fnbmanager"
```

Enable a system-assigned managed identity on the App Service:

```powershell
$principalId = az webapp identity assign `
  --resource-group $resourceGroup `
  --name $appName `
  --query principalId `
  --output tsv

Write-Host "App Service managed identity principal id: $principalId"
```

Allow the App Service outbound addresses through the Azure SQL firewall. Re-run this after changing the App Service plan or region because outbound addresses can change.

```powershell
$outboundIps = (az webapp show `
  --resource-group $resourceGroup `
  --name $appName `
  --query outboundIpAddresses `
  --output tsv) -split ","

for ($i = 0; $i -lt $outboundIps.Length; $i++) {
  az sql server firewall-rule create `
    --resource-group $resourceGroup `
    --server $sqlServerName `
    --name "appservice-outbound-$i" `
    --start-ip-address $outboundIps[$i] `
    --end-ip-address $outboundIps[$i]
}
```

Set a Microsoft Entra admin on the Azure SQL logical server. Use either your user or an Entra group as the admin.

```powershell
$entraAdminDisplayName = "<entra-user-or-group-display-name>"
$entraAdminObjectId = "<entra-user-or-group-object-id>"

az sql server ad-admin create `
  --resource-group $resourceGroup `
  --server $sqlServerName `
  --display-name $entraAdminDisplayName `
  --object-id $entraAdminObjectId
```

Grant the App Service identity access to the database. `db_ddladmin` is included because this app applies EF Core migrations on startup when `Database__Provider=AzureSql`. If migrations are later handled by CI/CD, remove `db_ddladmin` and set `Database__ApplyMigrationsOnStartup=false`.

```powershell
$sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$appName')
BEGIN
    CREATE USER [$appName] FROM EXTERNAL PROVIDER;
END

IF IS_ROLEMEMBER('db_datareader', '$appName') = 0
    ALTER ROLE db_datareader ADD MEMBER [$appName];

IF IS_ROLEMEMBER('db_datawriter', '$appName') = 0
    ALTER ROLE db_datawriter ADD MEMBER [$appName];

IF IS_ROLEMEMBER('db_ddladmin', '$appName') = 0
    ALTER ROLE db_ddladmin ADD MEMBER [$appName];
"@

sqlcmd `
  -S "$sqlServerName.database.windows.net" `
  -d $databaseName `
  -G `
  -Q $sql
```

Configure App Service to use Azure SQL without a SQL password:

```powershell
$connectionString = "Server=tcp:$sqlServerName.database.windows.net,1433;Database=$databaseName;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $appName `
  --settings `
    Database__Provider=AzureSql `
    Database__ApplyMigrationsOnStartup=true `
    "ConnectionStrings__DefaultConnection=$connectionString"

az webapp restart `
  --resource-group $resourceGroup `
  --name $appName
```

When the app starts with `Database__Provider=AzureSql`, it runs EF Core migrations from `Data/Migrations`. To add future schema changes:

```powershell
dotnet ef migrations add <MigrationName> --context AppDbContext --output-dir Data\Migrations
dotnet build FnBAz.sln
```

For a user-assigned managed identity, add `User Id=<managed-identity-client-id>;` to the Azure SQL connection string.

## Azure API Management (APIM)

This repo now exposes OpenAPI at:

```text
https://fnbmanager-77700.azurewebsites.net/swagger/v1/swagger.json
```

You can import that document into Azure API Management so clients call APIM instead of calling App Service directly.

Prerequisites:

- Existing deployed App Service for this repo
- Azure API Management instance (Consumption is lowest-cost to start)

### Create APIM and import this API

Keep authentication enabled for Swagger and import from a browser-authenticated production download.

```powershell
$ErrorActionPreference = "Stop"

$resourceGroup = "rg-fnbmanager-free"
$location = "southeastasia"
$apimName = "fnbmanager-apim-77700"
$publisherEmail = "baominh.nguyen.295@gmail.com"
$publisherName = "FnB Manager"
$appName = "fnbmanager-77700"
$apiId = "fnb-manager-api"
$swaggerPath = ".\swagger.production.json"

# 1) Open this URL in a browser, sign in, and save the response as swagger.production.json
Start-Process "https://$appName.azurewebsites.net/swagger/v1/swagger.json"
throw "After saving swagger.production.json in repo root, re-run from validation step below."
```

Then run:

```powershell
$ErrorActionPreference = "Stop"
$resourceGroup = "rg-fnbmanager-free"
$location = "southeastasia"
$apimName = "fnbmanager-apim-77700"
$publisherEmail = "baominh.nguyen.295@gmail.com"
$publisherName = "FnB Manager"
$apiId = "fnb-manager-api"
$swaggerPath = ".\swagger.production.json"

# Validate downloaded file before APIM import
$raw = Get-Content $swaggerPath -Raw
if ($raw.TrimStart().StartsWith("<")) {
  throw "File contains HTML, not OpenAPI JSON. Re-download from browser after successful sign-in."
}

$doc = $raw | ConvertFrom-Json
if (-not $doc.openapi) {
  throw "Downloaded file is JSON but does not contain an OpenAPI document."
}

Write-Host "Swagger download validated: OpenAPI version $($doc.openapi)"

# Create APIM (first run) and import API definition
az apim create `
  --name $apimName `
  --resource-group $resourceGroup `
  --location $location `
  --publisher-email $publisherEmail `
  --publisher-name $publisherName `
  --sku-name Consumption

az apim api import `
  --resource-group $resourceGroup `
  --service-name $apimName `
  --api-id $apiId `
  --path fnb `
  --specification-format OpenApiJson `
  --specification-path $swaggerPath
```

This avoids `consent_required` issues from Azure CLI token flow while still sourcing Swagger from the production URL.

Why this works:
- Swagger remains protected by App Service Authentication.
- You fetch Swagger from the production URL with a valid Entra bearer token.
- APIM imports from a local file, so no unauthenticated server-side fetch is required.

After import, gateway base URL is:

```text
https://fnbmanager-apim-77700.azure-api.net/fnb
```

Examples:

- `GET https://fnbmanager-apim-77700.azure-api.net/fnb/api/menu`
- `GET https://fnbmanager-apim-77700.azure-api.net/fnb/api/inventory`
- `GET https://fnbmanager-apim-77700.azure-api.net/fnb/api/orders`

### Require a subscription key (recommended)

In Azure Portal:

1. Open API Management > APIs > `fnb-manager-api`.
2. Open Settings.
3. Enable `Subscription required`.
4. Save.
5. Create or use an existing product and subscription.
6. Call APIM with `Ocp-Apim-Subscription-Key: <key>`.

### Add common inbound policies

In Azure Portal:

1. Open API Management > APIs > `fnb-manager-api` > Design > Inbound processing.
2. Add policy snippets such as:
   - Rate limit
   - Quota
   - Validate JWT (if using Entra tokens)
3. Save and test from the Test tab.

Sample policy XML:

```xml
<policies>
  <inbound>
    <base />
    <rate-limit-by-key calls="60" renewal-period="60" counter-key="@(context.Subscription?.Key ?? context.Request.IpAddress)" />
  </inbound>
  <backend>
    <base />
  </backend>
  <outbound>
    <base />
  </outbound>
  <on-error>
    <base />
  </on-error>
</policies>
```

### Authentication model options

- Option 1: Keep App Service Authentication enabled and let APIM forward requests to App Service.
- Option 2: Move auth checks to APIM (`validate-jwt`) and keep App Service open only to APIM.

For production, prefer a single auth layer with clear ownership and avoid duplicate token enforcement unless required.

## Notes

- The app uses SQLite (`fnbmanager.db`) for local development.
- Azure SQL uses EF Core SQL Server retry behavior to handle transient cloud database failures.
- Event persistence tables are included: `OutboxMessages`, `InboxMessages`, and `DeadLetterMessages` for event-driven workflows.
- Local `dotnet run` does not use Azure App Service Authentication; authentication is enforced only in Azure App Service.
