using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ToDo.Models;
using ToDo.Models.Dto;
using ToDo.Services;
using Xunit;

namespace ToDo.UnitTests
{
    public class AuthServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthServiceTests()
        {
            // Setup in-memory database for each test
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"ConnectionStrings:Database", "Data Source=:memory:"},
                    {"AppSettings:Token", "your-super-secret-key-that-is-at-least-256-bits-long-for-testing-purposes"},
                    {"AppSettings:Issuer", "ToDo-App"},
                    {"AppSettings:Audience", "ToDo-Users"}
                })
                .Build();

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            _authService = new AuthService(_context, _configuration);
        }

        [Fact]
        public async Task RegisterAsync_NewUser_ShouldCreateUser()
        {
            // Arrange
            var userDto = new UserDto
            {
                UserName = "testuser",
                Password = "testpassword"
            };

            // Act
            var result = await _authService.RegisterAsync(userDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.UserName);
            Assert.NotEmpty(result.PasswordHash);
            Assert.NotEqual("testpassword", result.PasswordHash);

            // Verify user was saved to database
            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "testuser");
            Assert.NotNull(savedUser);
        }

        [Fact]
        public async Task RegisterAsync_ExistingUser_ShouldReturnNull()
        {
            // Arrange
            var existingUser = new User
            {
                UserName = "existinguser",
                PasswordHash = "hashedpassword"
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                UserName = "existinguser",
                Password = "testpassword"
            };

            // Act
            var result = await _authService.RegisterAsync(userDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ShouldReturnTokenResponse()
        {
            // Arrange
            var userDto = new UserDto
            {
                UserName = "testuser",
                Password = "testpassword"
            };

            // Register user first
            await _authService.RegisterAsync(userDto);

            // Act
            var result = await _authService.LoginAsync(userDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.AccessToken);
            Assert.NotEmpty(result.RefreshToken);
        }

        [Fact]
        public async Task LoginAsync_InvalidUserName_ShouldReturnNull()
        {
            // Arrange
            var userDto = new UserDto
            {
                UserName = "nonexistentuser",
                Password = "testpassword"
            };

            // Act
            var result = await _authService.LoginAsync(userDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ShouldReturnNull()
        {
            // Arrange
            var registerDto = new UserDto
            {
                UserName = "testuser",
                Password = "correctpassword"
            };
            await _authService.RegisterAsync(registerDto);

            var loginDto = new UserDto
            {
                UserName = "testuser",
                Password = "wrongpassword"
            };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidRefreshToken_ShouldReturnNewTokenResponse()
        {
            // Arrange
            var userDto = new UserDto
            {
                UserName = "testuser",
                Password = "testpassword"
            };

            // Register and login user first
            var user = await _authService.RegisterAsync(userDto);
            Assert.NotNull(user);

            var loginResult = await _authService.LoginAsync(userDto);
            Assert.NotNull(loginResult);

            var refreshRequest = new RefreshTokenRequestDto
            {
                UserId = user.Id,
                RefreshToken = loginResult.RefreshToken
            };

            // Act
            var result = await _authService.RefreshTokenAsync(refreshRequest);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.AccessToken);
            Assert.NotEmpty(result.RefreshToken);
        }

        [Fact]
        public async Task RefreshTokenAsync_InvalidRefreshToken_ShouldReturnNull()
        {
            // Arrange
            var userDto = new UserDto
            {
                UserName = "testuser",
                Password = "testpassword"
            };

            var user = await _authService.RegisterAsync(userDto);
            Assert.NotNull(user);

            var refreshRequest = new RefreshTokenRequestDto
            {
                UserId = user.Id,
                RefreshToken = "invalid-refresh-token"
            };

            // Act
            var result = await _authService.RefreshTokenAsync(refreshRequest);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshTokenAsync_NonExistentUser_ShouldReturnNull()
        {
            // Arrange
            var refreshRequest = new RefreshTokenRequestDto
            {
                UserId = Guid.NewGuid(),
                RefreshToken = "some-token"
            };

            // Act
            var result = await _authService.RefreshTokenAsync(refreshRequest);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LogoutAsync_ValidUser_ShouldClearRefreshToken()
        {
            // Arrange
            var userDto = new UserDto
            {
                UserName = "testuser",
                Password = "testpassword"
            };

            var user = await _authService.RegisterAsync(userDto);
            Assert.NotNull(user);

            var loginResult = await _authService.LoginAsync(userDto);
            Assert.NotNull(loginResult);

            // Verify user has refresh token
            var userBefore = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(userBefore?.RefreshToken);

            // Act
            await _authService.LogoutAsync(user.Id);

            // Assert
            var userAfter = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(userAfter);
            Assert.Null(userAfter.RefreshToken);
            Assert.True(userAfter.RefreshTokenExpiryTime <= DateTime.UtcNow);
        }

        [Fact]
        public async Task LogoutAsync_NonExistentUser_ShouldNotThrow()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();

            // Act & Assert - Should not throw any exception
            await _authService.LogoutAsync(nonExistentUserId);
        }

        [Fact]
        public async Task Login_ShouldGenerateValidJwtToken()
        {
            // Arrange
            var userDto = new UserDto
            {
                UserName = "testuser",
                Password = "testpassword"
            };

            await _authService.RegisterAsync(userDto);

            // Act
            var result = await _authService.LoginAsync(userDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.AccessToken);

            // Basic JWT token structure validation (should have 3 parts separated by dots)
            var tokenParts = result.AccessToken.Split('.');
            Assert.Equal(3, tokenParts.Length);
        }

        [Fact]
        public async Task Login_ShouldSetRefreshTokenExpiry()
        {
            // Arrange
            var userDto = new UserDto
            {
                UserName = "testuser",
                Password = "testpassword"
            };

            var user = await _authService.RegisterAsync(userDto);
            Assert.NotNull(user);

            // Act
            await _authService.LoginAsync(userDto);

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(updatedUser);
            Assert.NotNull(updatedUser.RefreshTokenExpiryTime);
            Assert.True(updatedUser.RefreshTokenExpiryTime > DateTime.UtcNow);
            Assert.True(updatedUser.RefreshTokenExpiryTime <= DateTime.UtcNow.AddDays(7));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
} 