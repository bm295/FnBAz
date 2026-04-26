using FnBManager.Models;

namespace FnBManager.ViewModels;

public class DashboardViewModel
{
    public int MenuCount { get; set; }
    public int InventoryCount { get; set; }
    public int ActiveOrders { get; set; }
    public List<Order> RecentOrders { get; set; } = [];
}
