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
        var menuCountTask = menuService.CountAsync();
        var inventoryCountTask = inventoryService.CountAsync();
        var activeOrdersTask = orderService.CountActiveAsync();
        var recentOrdersTask = orderService.GetRecentAsync(5);

        await Task.WhenAll(menuCountTask, inventoryCountTask, activeOrdersTask, recentOrdersTask);

        return new DashboardViewModel
        {
            MenuCount = await menuCountTask,
            InventoryCount = await inventoryCountTask,
            ActiveOrders = await activeOrdersTask,
            RecentOrders = await recentOrdersTask
        };
    }
}
