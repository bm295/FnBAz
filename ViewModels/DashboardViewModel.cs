using FnBManager.Models;

namespace FnBManager.ViewModels;

public class DashboardViewModel
{
    public int MenuCount { get; set; }
    public int InventoryCount { get; set; }
    public int ActiveOrders { get; set; }
    public int ClosedOrders { get; set; }
    public int TotalOrders { get; set; }
    public string ClosedOrderFraction { get; set; } = string.Empty;
    public decimal ClosedOrderPercentage => TotalOrders == 0 ? 0 : 100m * ClosedOrders / TotalOrders;
    public List<Order> RecentOrders { get; set; } = [];
}
