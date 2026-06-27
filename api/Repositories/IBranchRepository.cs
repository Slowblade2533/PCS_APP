using PCS_API.DTOs;

namespace PCS_API.Repositories;

public interface IBranchRepository
{
    Task<IEnumerable<BranchDto>> GetAllActiveAsync();
    Task<BranchDetailDto?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(int id, BranchUpdateDto dto);
}
