using FnBManager.Application.Ports;
using FnBManager.ViewModels;

namespace FnBManager.Application.Services;

public class DashboardService(
    IMenuService menuService,
    IInventoryService inventoryService,
    IOrderService orderService) : IDashboardService
{
    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        // These services share a scoped DbContext, which requires sequential queries.
        var menuCount = await menuService.CountAsync();
        var inventoryCount = await inventoryService.CountAsync();
        var activeOrders = await orderService.CountActiveAsync();
        var recentOrders = await orderService.GetRecentAsync(5);
        var (favorable, total) = await orderService.GetClosureCountsAsync();
        var bestSellingMenuItem = await orderService.GetBestSellingMenuItemAsync();
        var gcd = Gcd(favorable, total);

        return new DashboardViewModel
        {
            MenuCount = menuCount,
            InventoryCount = inventoryCount,
            ActiveOrders = activeOrders,
            RecentOrders = recentOrders,
            BestSellingMenuItem = bestSellingMenuItem,
            ClosedOrders = favorable,
            TotalOrders = total,
            ClosedOrderFraction = total == 0 ? string.Empty : $"{favorable / gcd}/{total / gcd}"
        };
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            int remainder = a % b;
            a = b;
            b = remainder;
        }

        return a;
    }
}
