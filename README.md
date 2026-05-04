# FnB Manager (.NET 10)

This repository contains a Food & Beverage management web application built with ASP.NET Core MVC and Entity Framework Core.

## Features

- Dashboard metrics (menu/inventory/active orders)
- Menu management
- Inventory management
- Order management with status workflow
- Azure App Service deployment-ready structure
- Configurable SQLite/Azure SQL persistence
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

### Optional Azure SQL free database

For a more production-like database, create an Azure SQL Database with the Azure SQL free offer in the Azure portal, then configure this app:

```powershell
az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $appName `
  --settings `
    Database__Provider=AzureSql `
    "ConnectionStrings__DefaultConnection=<azure-sql-connection-string>"

az webapp restart `
  --resource-group $resourceGroup `
  --name $appName
```

Keep the Azure SQL free database set to pause when the free monthly limit is reached unless you intentionally want billable usage.

## Notes

- The app uses SQLite (`fnbmanager.db`) for local development.
- Azure SQL uses EF Core SQL Server retry behavior to handle transient cloud database failures.
- Event persistence tables are included: `OutboxMessages`, `InboxMessages`, and `DeadLetterMessages` for event-driven workflows.
- Local `dotnet run` does not use Azure App Service Authentication; authentication is enforced only in Azure App Service.
