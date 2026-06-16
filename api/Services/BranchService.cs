using PCS_API.DTOs;
using PCS_API.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCS_API.Services;

public class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepository;

    public BranchService(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<IEnumerable<BranchDto>> GetAllActiveBranchesAsync()
    {
        return await _branchRepository.GetAllActiveAsync();
    }

    public async Task<BranchDetailDto?> GetBranchByIdAsync(int id)
    {
        return await _branchRepository.GetByIdAsync(id);
    }

    public async Task<bool> UpdateBranchAsync(int id, BranchUpdateDto dto)
    {
        return await _branchRepository.UpdateAsync(id, dto);
    }
}
