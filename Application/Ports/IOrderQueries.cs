using FnBManager.Models;

namespace FnBManager.Application.Ports;

public interface IOrderQueries
{
    Task<List<Order>> GetAllWithMenuAsync();
    Task<List<Order>> GetRecentWithMenuAsync(int take);
    Task<int> CountActiveAsync();
    Task<(int Closed, int Total)> GetClosureCountsAsync();
    Task<BestSellingMenuItem?> GetBestSellingMenuItemAsync();
}
