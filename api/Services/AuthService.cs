using Microsoft.AspNetCore.Identity;
using PCS_API.Models;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class AuthService(IUserRepository userRepository, IPasswordHasher<UserTableModel> passwordHasher) : IAuthService
{
    public async Task<UserTableModel?> AuthenticateAsync(string email, string password)
    {
        var user = await userRepository.GetActiveUserByEmailAsync(email);
        if (user == null) return null;

        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verificationResult == PasswordVerificationResult.Failed) return null;

        return user;
    }

    public async Task<IEnumerable<UserPermissionInfoModel>> GetUserPermissionsAsync(int userId)
    {
        return await userRepository.GetUserPermissionsAsync(userId);
    }
}
