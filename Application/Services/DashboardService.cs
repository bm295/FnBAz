using FnBManager.Application.Ports;
using FnBManager.ViewModels;

namespace FnBManager.Application.Services;

public class DashboardService(
    IMenuService menuService,
    IInventoryService inventoryService,
    IOrderService orderService) : IDashboardService
{
    public async Task<DashboardViewModel> GetDashboardAsync() =>
        new()
        {
            MenuCount = await menuService.CountAsync(),
            InventoryCount = await inventoryService.CountAsync(),
            ActiveOrders = await orderService.CountActiveAsync(),
            RecentOrders = await orderService.GetRecentAsync(5)
        };
}
