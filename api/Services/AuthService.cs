using Microsoft.AspNetCore.Identity;
using PCS_API.Models;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<UserTable> _passwordHasher;

    public AuthService(IUserRepository userRepository, IPasswordHasher<UserTable> passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserTable?> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.GetActiveUserByEmailAsync(email);

        if (user == null)
            return null;

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (verificationResult == PasswordVerificationResult.Failed)
            return null;

        return user;
    }
}
