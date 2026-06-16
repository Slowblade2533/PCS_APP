using PCS_API.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCS_API.Services;

public interface IBranchService
{
    Task<IEnumerable<BranchDto>> GetAllActiveBranchesAsync();
    Task<BranchDetailDto?> GetBranchByIdAsync(int id);
    Task<bool> UpdateBranchAsync(int id, BranchUpdateDto dto);
}
