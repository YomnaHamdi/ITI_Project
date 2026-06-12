using Domain.Entities;

namespace Application.Interfaces
{
    public interface IPlacementRepository
    {
        Task<List<PlacementQuestion>> GetAllAsync();
        Task<List<PlacementQuestion>> GetByPartAsync(int part);
        Task SeedAsync();
    }
}
