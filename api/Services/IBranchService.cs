using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IBranchService
{
    Task<IEnumerable<BranchDto>> GetAllActiveBranchesAsync();
    Task<BranchDetailDto?> GetBranchByIdAsync(int id);
    Task<bool> UpdateBranchAsync(int id, BranchUpdateDto dto);
}
