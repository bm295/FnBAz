using FnBManager.Application.Ports;
using FnBManager.Application.Services;
using FnBManager.Data;
using FnBManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var databaseProvider = builder.Configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
var applyMigrationsOnStartup = builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (IsSqlServerProvider(databaseProvider))
    {
        options.UseSqlServer(connectionString, sqlServer =>
            sqlServer.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null));
        return;
    }

    if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
        return;
    }

    throw new InvalidOperationException(
        $"Unsupported database provider '{databaseProvider}'. Use 'Sqlite' or 'AzureSql'.");
});

builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();

builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

if (applyMigrationsOnStartup)
{
    await InitializeDatabaseAsync(app, databaseProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static bool IsSqlServerProvider(string databaseProvider) =>
    databaseProvider.Equals("AzureSql", StringComparison.OrdinalIgnoreCase) ||
    databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase);

static async Task InitializeDatabaseAsync(WebApplication app, string databaseProvider)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (IsSqlServerProvider(databaseProvider))
    {
        await db.Database.MigrateAsync();
        return;
    }

    await db.Database.EnsureCreatedAsync();
}
