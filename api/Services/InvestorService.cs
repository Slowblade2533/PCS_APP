using PCS_API.Models;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class InvestorService(
    IInvestorRepository investorRepository,
    IInvestorBankAccountRepository bankAccountRepository) : IInvestorService
{
    public async Task<IEnumerable<InvestorModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await investorRepository.GetAllAsync(cancellationToken);
    }

    public async Task<InvestorModel?> GetByIdAsync(Guid investorId, CancellationToken cancellationToken = default)
    {
        return await investorRepository.GetByIdAsync(investorId, cancellationToken);
    }

    public async Task<Guid> CreateAsync(InvestorModel model, List<InvestorBankAccountModel> bankAccounts, CancellationToken cancellationToken = default)
    {
        model.InvestorId = model.InvestorId == Guid.Empty ? Guid.NewGuid() : model.InvestorId;
        var investorId = await investorRepository.CreateAsync(model, cancellationToken);

        if (bankAccounts != null)
        {
            foreach (var account in bankAccounts)
            {
                account.InvestorId = investorId;
                await bankAccountRepository.CreateAsync(account, cancellationToken);
            }
        }

        return investorId;
    }

    public async Task<bool> UpdateAsync(InvestorModel model, List<InvestorBankAccountModel> bankAccounts, CancellationToken cancellationToken = default)
    {
        var exists = await investorRepository.GetByIdAsync(model.InvestorId, cancellationToken);
        if (exists == null) return false;

        var updated = await investorRepository.UpdateAsync(model, cancellationToken);
        if (!updated) return false;

        var existingAccounts = await bankAccountRepository.GetByInvestorAsync(model.InvestorId, cancellationToken);
        var existingAccountDict = existingAccounts.ToDictionary(a => a.BankAccountId);

        // 1. Create or Update incoming bank accounts
        if (bankAccounts != null)
        {
            foreach (var account in bankAccounts)
            {
                account.InvestorId = model.InvestorId;
                if (existingAccountDict.ContainsKey(account.BankAccountId))
                {
                    await bankAccountRepository.UpdateAsync(account, cancellationToken);
                }
                else
                {
                    await bankAccountRepository.CreateAsync(account, cancellationToken);
                }
            }
        }

        // 2. Delete bank accounts that were removed in the request
        var incomingIds = bankAccounts?.Select(a => a.BankAccountId).ToHashSet() ?? [];
        foreach (var extAcc in existingAccounts)
        {
            if (!incomingIds.Contains(extAcc.BankAccountId))
            {
                try
                {
                    await bankAccountRepository.DeleteAsync(extAcc.BankAccountId, cancellationToken);
                }
                catch (Exception ex) when (ex.Message.Contains("FK_") || ex.Message.Contains("REFERENCE constraint") || ex.Message.Contains("conflict"))
                {
                    throw new ArgumentException($"ไม่สามารถลบบัญชีธนาคาร {extAcc.BankName} ({extAcc.AccountNumber}) ได้ เนื่องจากถูกอ้างอิงอยู่ในการร่วมลงทุน/เงินกู้ยืม");
                }
            }
        }

        return true;
    }

    public async Task<bool> DeleteAsync(Guid investorId, CancellationToken cancellationToken = default)
    {
        // Investor bank accounts will be deleted by ON DELETE CASCADE in DB foreign key,
        // but we can delete them explicitly or let cascade handle it. Let cascade handle it.
        return await investorRepository.DeleteAsync(investorId, cancellationToken);
    }

    public async Task<IEnumerable<InvestorBankAccountModel>> GetBankAccountsAsync(Guid investorId, CancellationToken cancellationToken = default)
    {
        return await bankAccountRepository.GetByInvestorAsync(investorId, cancellationToken);
    }
}
