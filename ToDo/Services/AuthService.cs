﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ToDo.Models;
using ToDo.Models.Dto;

namespace ToDo.Services
{
    public class AuthService(ApplicationDbContext context, IConfiguration configuration) : IAuthService
    {
        public async Task<TokenResponseDto?> LoginAsync(UserDto request)
        {


            var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
            if (user is null)
            {
                return null;
            }
            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return null;
            }

            return await CreateTokenResponse(user);
        }

        private async Task<TokenResponseDto> CreateTokenResponse(User user)
        {
            return new TokenResponseDto
            {
                AccessToken = CreateToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
            };
        }

        private async Task<User?> ValidateRefreshtokenAsync(Guid userId, string refreshToken)
        {
            var user = await context.Users.FindAsync(userId);
            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }

            return user;
        }

        public async Task<User?> RegisterAsync(UserDto request)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return null;
            }

            if (await context.Users.AnyAsync(u => u.UserName == request.UserName))
            {
                return null;
            }
            var user = new User();

            var hashedPassword = new PasswordHasher<User>().HashPassword(user, request.Password);

            user.UserName = request.UserName;
            user.PasswordHash = hashedPassword;

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user;
        }

        public async Task<RegisterResultDto> RegisterWithResultAsync(UserDto request)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new RegisterResultDto
                {
                    Success = false,
                    ErrorCode = RegisterErrorCodes.EmptyFields,
                    ErrorMessage = "Kullanıcı adı ve şifre boş bırakılamaz."
                };
            }

            // Username length validation
            if (request.UserName.Length < 3)
            {
                return new RegisterResultDto
                {
                    Success = false,
                    ErrorCode = RegisterErrorCodes.InvalidInput,
                    ErrorMessage = "Kullanıcı adı en az 3 karakter olmalıdır."
                };
            }

            // Password length validation
            if (request.Password.Length < 6)
            {
                return new RegisterResultDto
                {
                    Success = false,
                    ErrorCode = RegisterErrorCodes.InvalidInput,
                    ErrorMessage = "Şifre en az 6 karakter olmalıdır."
                };
            }

            // Check if user already exists
            if (await context.Users.AnyAsync(u => u.UserName == request.UserName))
            {
                return new RegisterResultDto
                {
                    Success = false,
                    ErrorCode = RegisterErrorCodes.UserExists,
                    ErrorMessage = "Kullanıcı adı zaten mevcut. Lütfen farklı bir kullanıcı adı seçin."
                };
            }

            // Create new user
            var user = new User();
            var hashedPassword = new PasswordHasher<User>().HashPassword(user, request.Password);

            user.UserName = request.UserName;
            user.PasswordHash = hashedPassword;

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return new RegisterResultDto
            {
                Success = true,
                User = user
            };
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await context.SaveChangesAsync();
            return refreshToken;
        }


        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,user.UserName),
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Role,user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:token")!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: configuration.GetValue<string>("AppSettings:Issuer"),
                audience: configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        public async Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var user = await ValidateRefreshtokenAsync(request.UserId, request.RefreshToken);
            if (user is null)
            {
                return null;
            }
            return await CreateTokenResponse(user);
        }

        public async Task LogoutAsync(Guid userId)
        {
            var user = await context.Users.FindAsync(userId);
            if (user is not null)
            {
                // Clear the refresh token and its expiry time
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = DateTime.UtcNow;

                await context.SaveChangesAsync();
            }
        }

        public async Task<ChangePasswordResultDto> ChangePasswordAsync(Guid userId, ChangePasswordDto request)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || 
                string.IsNullOrWhiteSpace(request.NewPassword) || 
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    ErrorMessage = "Tüm alanları doldurun."
                };
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    ErrorMessage = "Yeni şifre ve şifre tekrarı uyuşmuyor."
                };
            }

            if (request.NewPassword.Length < 6)
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    ErrorMessage = "Yeni şifre en az 6 karakter olmalıdır."
                };
            }

            // Get user
            var user = await context.Users.FindAsync(userId);
            if (user is null)
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    ErrorMessage = "Kullanıcı bulunamadı."
                };
            }

            // Verify current password
            var passwordHasher = new PasswordHasher<User>();
            if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    ErrorMessage = "Mevcut şifre yanlış."
                };
            }

            // Hash new password and update user
            user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
            await context.SaveChangesAsync();

            return new ChangePasswordResultDto
            {
                Success = true
            };
        }
    }
}
