using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ToDo.Models;
using ToDo.Models.Dto;
using ToDo.Services;
using Xunit;

namespace ToDo.FunctionalTests
{
    public class TodoAppFunctionalTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly ApplicationDbContext _context;

        public TodoAppFunctionalTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                
                builder.ConfigureServices(services =>
                {
                    // Configure test-specific settings
                    services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
                    {
                        options.SerializerOptions.PropertyNameCaseInsensitive = true;
                    });
                });
            });

            _client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task ApplicationStartup_ShouldRespond()
        {
            // Act
            var response = await _client.GetAsync("/");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UserWorkflow_CompleteRegistrationAndLogin_ShouldWork()
        {
            // Arrange
            var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
            var userDto = new UserDto
            {
                UserName = "functionaluser",
                Password = "functionalpassword"
            };

            // Act & Assert - User Registration
            var registeredUser = await authService.RegisterAsync(userDto);
            Assert.NotNull(registeredUser);
            Assert.Equal("functionaluser", registeredUser.UserName);
            Assert.NotEmpty(registeredUser.PasswordHash);

            // Act & Assert - User Login
            var loginResult = await authService.LoginAsync(userDto);
            Assert.NotNull(loginResult);
            Assert.NotEmpty(loginResult.AccessToken);
            Assert.NotEmpty(loginResult.RefreshToken);

            // Verify user exists in database
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "functionaluser");
            Assert.NotNull(dbUser);
            Assert.Equal(registeredUser.Id, dbUser.Id);
        }

        [Fact]
        public async Task TodoManagement_CompleteWorkflow_ShouldWork()
        {
            // Arrange - Setup user
            var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
            var todoService = _scope.ServiceProvider.GetRequiredService<TodoService>();

            var userDto = new UserDto
            {
                UserName = "todouser",
                Password = "todopassword"
            };

            var user = await authService.RegisterAsync(userDto);
            Assert.NotNull(user);

            // Test Data
            var todos = new List<Todo>
            {
                new Todo
                {
                    Title = "Complete project documentation",
                    UserId = user.Id,
                    DueDate = DateTime.Today.AddDays(7),
                    IsComplete = false
                },
                new Todo
                {
                    Title = "Review code changes",
                    UserId = user.Id,
                    DueDate = DateTime.Today.AddDays(2),
                    IsComplete = false
                },
                new Todo
                {
                    Title = "Prepare presentation",
                    UserId = user.Id,
                    DueDate = DateTime.Today.AddDays(-1), // Overdue
                    IsComplete = false
                }
            };

            // Act & Assert - Create Todos
            var createdTodos = new List<Todo>();
            foreach (var todo in todos)
            {
                var createdTodo = await todoService.AddTodoAsync(todo);
                Assert.NotNull(createdTodo);
                Assert.Equal(todo.Title, createdTodo.Title);
                
                // Check correct status based on due date
                if (todo.DueDate.HasValue && todo.DueDate.Value < DateTime.Today)
                {
                    Assert.Equal("Gecikmiş", createdTodo.Status);
                }
                else
                {
                    Assert.Equal("Beklemede", createdTodo.Status);
                }
                
                createdTodos.Add(createdTodo);
            }

            // Act & Assert - Get All User Todos
            var userTodos = await todoService.GetUserTodosAsync(user.Id);
            Assert.Equal(3, userTodos.Count);

            // Act & Assert - Get Overdue Todos
            var overdueTodos = await todoService.GetUserOverdueTodosAsync(user.Id);
            Assert.Single(overdueTodos);
            Assert.Equal("Prepare presentation", overdueTodos[0].Title);

            // Act & Assert - Complete a Todo
            var todoToComplete = createdTodos.First();
            todoToComplete.IsComplete = true;
            var completedTodo = await todoService.UpdateTodoAsync(todoToComplete);
            Assert.NotNull(completedTodo);
            Assert.True(completedTodo.IsComplete);
            Assert.Equal("Tamamlandı", completedTodo.Status);
            Assert.NotNull(completedTodo.CompletedDate);

            // Act & Assert - Get Completed Todos
            var completedTodos = await todoService.GetUserCompletedTodosAsync(user.Id);
            Assert.Single(completedTodos);
            Assert.Equal(todoToComplete.Title, completedTodos[0].Title);

            // Act & Assert - Update Todo Title
            var todoToUpdate = createdTodos.Skip(1).First();
            todoToUpdate.Title = "Updated: Review code changes";
            var updatedTodo = await todoService.UpdateTodoAsync(todoToUpdate);
            Assert.NotNull(updatedTodo);
            Assert.Equal("Updated: Review code changes", updatedTodo.Title);

            // Act & Assert - Delete a Todo
            var todoToDelete = createdTodos.Last();
            var deleteResult = await todoService.DeleteTodoAsync(todoToDelete.Id);
            Assert.True(deleteResult);

            // Verify final state
            var finalTodos = await todoService.GetUserTodosAsync(user.Id);
            Assert.Equal(2, finalTodos.Count);
        }

        [Fact]
        public async Task MultiUserScenario_ShouldIsolateUserData()
        {
            // Arrange - Create multiple users
            var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
            var todoService = _scope.ServiceProvider.GetRequiredService<TodoService>();

            var users = new List<User>();
            for (int i = 1; i <= 3; i++)
            {
                var userDto = new UserDto
                {
                    UserName = $"user{i}",
                    Password = $"password{i}"
                };
                var user = await authService.RegisterAsync(userDto);
                Assert.NotNull(user);
                users.Add(user);
            }

            // Act - Create todos for each user
            var userTodoCounts = new Dictionary<Guid, int>();
            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                var todoCount = i + 2; // Create 2, 3, 4 todos respectively
                userTodoCounts[user.Id] = todoCount;

                for (int j = 1; j <= todoCount; j++)
                {
                    var todo = new Todo
                    {
                        Title = $"User {i + 1} Todo {j}",
                        UserId = user.Id,
                        IsComplete = j == 1 // First todo is completed
                    };
                    var createdTodo = await todoService.AddTodoAsync(todo);
                    Assert.NotNull(createdTodo);
                }
            }

            // Assert - Verify data isolation
            foreach (var user in users)
            {
                var userTodos = await todoService.GetUserTodosAsync(user.Id);
                var expectedCount = userTodoCounts[user.Id];
                Assert.Equal(expectedCount, userTodos.Count);

                // Verify all todos belong to the user
                Assert.All(userTodos, todo => Assert.Equal(user.Id, todo.UserId));

                // Verify completed todos
                var completedTodos = await todoService.GetUserCompletedTodosAsync(user.Id);
                Assert.Single(completedTodos);
            }
        }

        [Fact]
        public async Task AuthenticationFlow_TokenRefreshAndLogout_ShouldWork()
        {
            // Arrange
            var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
            var userDto = new UserDto
            {
                UserName = "authflowuser",
                Password = "authflowpassword"
            };

            // Act & Assert - Register and Login
            var user = await authService.RegisterAsync(userDto);
            Assert.NotNull(user);

            var loginResult = await authService.LoginAsync(userDto);
            Assert.NotNull(loginResult);
            var originalRefreshToken = loginResult.RefreshToken;

            // Act & Assert - Refresh Token
            var refreshRequest = new RefreshTokenRequestDto
            {
                UserId = user.Id,
                RefreshToken = originalRefreshToken
            };

            var refreshResult = await authService.RefreshTokenAsync(refreshRequest);
            Assert.NotNull(refreshResult);
            Assert.NotEmpty(refreshResult.AccessToken);
            Assert.NotEmpty(refreshResult.RefreshToken);
            Assert.NotEqual(originalRefreshToken, refreshResult.RefreshToken);

            // Act & Assert - Use old refresh token (should fail)
            var oldTokenRequest = new RefreshTokenRequestDto
            {
                UserId = user.Id,
                RefreshToken = originalRefreshToken
            };

            var oldTokenResult = await authService.RefreshTokenAsync(oldTokenRequest);
            Assert.Null(oldTokenResult);

            // Act & Assert - Logout
            await authService.LogoutAsync(user.Id);

            // Verify user is logged out (refresh token cleared)
            var userAfterLogout = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(userAfterLogout);
            Assert.Null(userAfterLogout.RefreshToken);

            // Try to use refresh token after logout (should fail)
            var postLogoutRequest = new RefreshTokenRequestDto
            {
                UserId = user.Id,
                RefreshToken = refreshResult.RefreshToken
            };

            var postLogoutResult = await authService.RefreshTokenAsync(postLogoutRequest);
            Assert.Null(postLogoutResult);
        }

        [Fact]
        public async Task TodoStatusManagement_ShouldMaintainConsistency()
        {
            // Arrange
            var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
            var todoService = _scope.ServiceProvider.GetRequiredService<TodoService>();

            var userDto = new UserDto
            {
                UserName = "statususer",
                Password = "statuspassword"
            };

            var user = await authService.RegisterAsync(userDto);
            Assert.NotNull(user);

            // Test scenarios for different status transitions
            var testCases = new[]
            {
                new { Description = "Normal todo", DueDate = (DateTime?)DateTime.Today.AddDays(1), ExpectedStatus = "Beklemede" },
                new { Description = "Overdue todo", DueDate = (DateTime?)DateTime.Today.AddDays(-1), ExpectedStatus = "Gecikmiş" }, // TodoService auto-sets overdue status
                new { Description = "No due date", DueDate = (DateTime?)null, ExpectedStatus = "Beklemede" }
            };

            // Act & Assert - Test different initial statuses
            foreach (var testCase in testCases)
            {
                var todo = new Todo
                {
                    Title = testCase.Description,
                    UserId = user.Id,
                    DueDate = testCase.DueDate,
                    IsComplete = false
                };

                var createdTodo = await todoService.AddTodoAsync(todo);
                Assert.NotNull(createdTodo);
                Assert.Equal(testCase.ExpectedStatus, createdTodo.Status);

                // Test completion
                createdTodo.IsComplete = true;
                var completedTodo = await todoService.UpdateTodoAsync(createdTodo);
                Assert.NotNull(completedTodo);
                Assert.Equal("Tamamlandı", completedTodo.Status);
                Assert.NotNull(completedTodo.CompletedDate);

                // Test reactivation
                completedTodo.IsComplete = false;
                var reactivatedTodo = await todoService.UpdateTodoAsync(completedTodo);
                Assert.NotNull(reactivatedTodo);
                
                // Check correct status after reactivation based on due date
                if (reactivatedTodo.DueDate.HasValue && reactivatedTodo.DueDate.Value < DateTime.Today)
                {
                    Assert.Equal("Gecikmiş", reactivatedTodo.Status);
                }
                else
                {
                    Assert.Equal("Beklemede", reactivatedTodo.Status);
                }
                
                Assert.Null(reactivatedTodo.CompletedDate);
            }
        }

        [Fact]
        public async Task DatabasePersistence_ShouldMaintainDataIntegrity()
        {
            // Arrange
            var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
            var todoService = _scope.ServiceProvider.GetRequiredService<TodoService>();

            // Create test data
            var userDto = new UserDto
            {
                UserName = "persistenceuser",
                Password = "persistencepassword"
            };

            var user = await authService.RegisterAsync(userDto);
            Assert.NotNull(user);

            var todo = new Todo
            {
                Title = "Persistence Test Todo",
                UserId = user.Id,
                DueDate = DateTime.Today.AddDays(5),
                IsComplete = false,
                DisplayOrder = 1
            };

            // Act
            var createdTodo = await todoService.AddTodoAsync(todo);
            Assert.NotNull(createdTodo);

            // Clear the context to ensure we're reading from the database
            _context.ChangeTracker.Clear();

            // Assert - Verify data persistence
            var retrievedUser = await _context.Users.FindAsync(user.Id);
            var retrievedTodo = await _context.Todos.FindAsync(createdTodo.Id);

            Assert.NotNull(retrievedUser);
            Assert.NotNull(retrievedTodo);

            // Verify user data
            Assert.Equal(user.UserName, retrievedUser.UserName);
            Assert.Equal(user.PasswordHash, retrievedUser.PasswordHash);

            // Verify todo data
            Assert.Equal(todo.Title, retrievedTodo.Title);
            Assert.Equal(user.Id, retrievedTodo.UserId);
            Assert.Equal(todo.DueDate?.Date, retrievedTodo.DueDate?.Date);
            Assert.Equal(todo.IsComplete, retrievedTodo.IsComplete);
            Assert.Equal(todo.DisplayOrder, retrievedTodo.DisplayOrder);

            // Verify relationships - User should exist
            var userExists = await _context.Users
                .AnyAsync(u => u.Id == user.Id);

            Assert.True(userExists);
        }

        [Fact]
        public async Task ErrorHandling_ShouldHandleInvalidOperations()
        {
            // Arrange
            var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
            var todoService = _scope.ServiceProvider.GetRequiredService<TodoService>();

            // Test invalid login
            var invalidUserDto = new UserDto
            {
                UserName = "nonexistent",
                Password = "wrongpassword"
            };

            // Act & Assert - Invalid login should return null
            var loginResult = await authService.LoginAsync(invalidUserDto);
            Assert.Null(loginResult);

            // Test duplicate registration
            var userDto = new UserDto
            {
                UserName = "duplicatetest",
                Password = "password"
            };

            var firstUser = await authService.RegisterAsync(userDto);
            Assert.NotNull(firstUser);

            var duplicateUser = await authService.RegisterAsync(userDto);
            Assert.Null(duplicateUser);

            // Test operations on non-existent todos
            var nonExistentTodo = await todoService.GetTodoByIdAsync(99999);
            Assert.Null(nonExistentTodo);

            var deleteResult = await todoService.DeleteTodoAsync(99999);
            Assert.False(deleteResult);

            var updateResult = await todoService.UpdateTodoAsync(new Todo { Id = 99999, Title = "Test" });
            Assert.Null(updateResult);
        }

        public void Dispose()
        {
            _scope?.Dispose();
            _client?.Dispose();
        }
    }
} 