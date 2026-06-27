using PCS_API.DTOs;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class BranchService(IBranchRepository branchRepository) : IBranchService
{
    public async Task<IEnumerable<BranchDto>> GetAllActiveBranchesAsync()
    {
        return await branchRepository.GetAllActiveAsync();
    }

    public async Task<BranchDetailDto?> GetBranchByIdAsync(int id)
    {
        return await branchRepository.GetByIdAsync(id);
    }

    public async Task<bool> UpdateBranchAsync(int id, BranchUpdateDto dto)
    {
        return await branchRepository.UpdateAsync(id, dto);
    }
}
