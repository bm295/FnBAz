# FnB Manager (.NET 14 + C# 10)

This repository contains a Food & Beverage management web application built with ASP.NET Core MVC, C# 10, and Entity Framework Core (SQLite).

## Features

- Dashboard metrics (menu/inventory/active orders)
- Menu management
- Inventory management
- Order management with status workflow
- Azure App Service deployment-ready structure

## Run locally

```bash
dotnet restore
dotnet run
```

Open `https://localhost:5001` or the URL shown in the console.

## Azure deployment (App Service)

1. Create an Azure App Service for .NET.
2. Configure startup command (if needed):
   ```bash
   dotnet FnBManager.dll
   ```
3. Set environment variable for production:
   - `ASPNETCORE_ENVIRONMENT=Production`
4. Deploy via GitHub Actions, Azure DevOps, or Zip Deploy.

## Notes

- The app uses SQLite (`fnbmanager.db`) for simplicity.
- Event persistence tables are included: `OutboxMessages`, `InboxMessages`, and `DeadLetterMessages` for event-driven workflows.
- For production workloads, prefer Azure SQL or PostgreSQL.
