using FnBManager.ViewModels;

namespace FnBManager.Application.Ports;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync();
}
