using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using System.Text.Json;
using ToDo.Models;
using ToDo.Models.Dto;
using ToDo.Services;
using Xunit;

namespace ToDo.IntegrationTests
{
    public class TodoApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly ApplicationDbContext _context;

        public TodoApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
            });

            _client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task TodoService_Integration_ShouldHandleCompleteWorkflow()
        {
            // Arrange
            var todoService = _scope.ServiceProvider.GetRequiredService<TodoService>();
            var userId = Guid.NewGuid();

            var todo = new Todo
            {
                Title = "Integration Test Todo",
                UserId = userId,
                DueDate = DateTime.Today.AddDays(7)
            };

            // Act & Assert - Add Todo
            var addedTodo = await todoService.AddTodoAsync(todo);
            Assert.NotNull(addedTodo);
            Assert.Equal("Integration Test Todo", addedTodo.Title);
            Assert.Equal("Beklemede", addedTodo.Status);

            // Act & Assert - Get User Todos
            var userTodos = await todoService.GetUserTodosAsync(userId);
            Assert.Single(userTodos);
            Assert.Equal(addedTodo.Id, userTodos[0].Id);

            // Act & Assert - Update Todo
            addedTodo.IsComplete = true;
            var updatedTodo = await todoService.UpdateTodoAsync(addedTodo);
            Assert.NotNull(updatedTodo);
            Assert.True(updatedTodo.IsComplete);
            Assert.Equal("Tamamlandı", updatedTodo.Status);

            // Act & Assert - Get Completed Todos
            var completedTodos = await todoService.GetUserCompletedTodosAsync(userId);
            Assert.Single(completedTodos);
            Assert.True(completedTodos[0].IsComplete);

            // Act & Assert - Delete Todo
            var deleteResult = await todoService.DeleteTodoAsync(addedTodo.Id);
            Assert.True(deleteResult);

            // Verify deletion
            var deletedTodo = await todoService.GetTodoByIdAsync(addedTodo.Id);
            Assert.Null(deletedTodo);
        }

        [Fact]
        public async Task AuthService_Integration_ShouldHandleUserLifecycle()
        {
            // Arrange
            var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
            var userDto = new UserDto
            {
                UserName = "integrationuser",
                Password = "integrationpassword"
            };

            // Act & Assert - Register User
            var registeredUser = await authService.RegisterAsync(userDto);
            Assert.NotNull(registeredUser);
            Assert.Equal("integrationuser", registeredUser.UserName);

            // Act & Assert - Login User
            var loginResult = await authService.LoginAsync(userDto);
            Assert.NotNull(loginResult);
            Assert.NotEmpty(loginResult.AccessToken);
            Assert.NotEmpty(loginResult.RefreshToken);

            // Act & Assert - Refresh Token
            var refreshRequest = new RefreshTokenRequestDto
            {
                UserId = registeredUser.Id,
                RefreshToken = loginResult.RefreshToken
            };

            var refreshResult = await authService.RefreshTokenAsync(refreshRequest);
            Assert.NotNull(refreshResult);
            Assert.NotEmpty(refreshResult.AccessToken);
            Assert.NotEmpty(refreshResult.RefreshToken);

            // Act & Assert - Logout User
            await authService.LogoutAsync(registeredUser.Id);

            // Verify refresh token is cleared
            var userAfterLogout = await _context.Users.FindAsync(registeredUser.Id);
            Assert.NotNull(userAfterLogout);
            Assert.Null(userAfterLogout.RefreshToken);
        }

        [Fact]
        public async Task TodoService_WithMultipleUsers_ShouldIsolateData()
        {
            // Arrange
            var todoService = _scope.ServiceProvider.GetRequiredService<TodoService>();
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();

            var user1Todo = new Todo
            {
                Title = "User 1 Todo",
                UserId = user1Id
            };

            var user2Todo = new Todo
            {
                Title = "User 2 Todo",
                UserId = user2Id
            };

            // Act
            await todoService.AddTodoAsync(user1Todo);
            await todoService.AddTodoAsync(user2Todo);

            var user1Todos = await todoService.GetUserTodosAsync(user1Id);
            var user2Todos = await todoService.GetUserTodosAsync(user2Id);

            // Assert
            Assert.Single(user1Todos);
            Assert.Single(user2Todos);
            Assert.Equal("User 1 Todo", user1Todos[0].Title);
            Assert.Equal("User 2 Todo", user2Todos[0].Title);
            Assert.Equal(user1Id, user1Todos[0].UserId);
            Assert.Equal(user2Id, user2Todos[0].UserId);
        }

        [Fact]
        public async Task TodoService_OverdueTodos_ShouldBeCorrectlyIdentified()
        {
            // Arrange
            var todoService = _scope.ServiceProvider.GetRequiredService<TodoService>();
            var userId = Guid.NewGuid();

            var overdueTodo = new Todo
            {
                Title = "Overdue Todo",
                UserId = userId,
                DueDate = DateTime.Today.AddDays(-1),
                IsComplete = false
            };

            var futureTodo = new Todo
            {
                Title = "Future Todo",
                UserId = userId,
                DueDate = DateTime.Today.AddDays(1),
                IsComplete = false
            };

            var completedOverdueTodo = new Todo
            {
                Title = "Completed Overdue Todo",
                UserId = userId,
                DueDate = DateTime.Today.AddDays(-1),
                IsComplete = true
            };

            // Act
            await todoService.AddTodoAsync(overdueTodo);
            await todoService.AddTodoAsync(futureTodo);
            await todoService.AddTodoAsync(completedOverdueTodo);

            var overdueTodos = await todoService.GetUserOverdueTodosAsync(userId);

            // Assert
            Assert.Single(overdueTodos);
            Assert.Equal("Overdue Todo", overdueTodos[0].Title);
            Assert.False(overdueTodos[0].IsComplete);
            Assert.True(overdueTodos[0].DueDate < DateTime.Today);
        }

        [Fact]
        public async Task AuthService_DuplicateRegistration_ShouldFail()
        {
            // Arrange
            var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
            var userDto = new UserDto
            {
                UserName = "duplicateuser",
                Password = "password123"
            };

            // Act - Register user first time
            var firstRegistration = await authService.RegisterAsync(userDto);
            Assert.NotNull(firstRegistration);

            // Act - Try to register same user again
            var secondRegistration = await authService.RegisterAsync(userDto);

            // Assert
            Assert.Null(secondRegistration);
        }

        [Fact]
        public async Task TodoService_StatusTransitions_ShouldBeConsistent()
        {
            // Arrange
            var todoService = _scope.ServiceProvider.GetRequiredService<TodoService>();
            var userId = Guid.NewGuid();

            var todo = new Todo
            {
                Title = "Status Test Todo",
                UserId = userId,
                IsComplete = false
            };

            // Act & Assert - Initial state
            var addedTodo = await todoService.AddTodoAsync(todo);
            Assert.Equal("Beklemede", addedTodo.Status);
            Assert.False(addedTodo.IsComplete);
            Assert.Null(addedTodo.CompletedDate);

            // Act & Assert - Mark as complete
            addedTodo.IsComplete = true;
            var completedTodo = await todoService.UpdateTodoAsync(addedTodo);
            Assert.NotNull(completedTodo);
            Assert.Equal("Tamamlandı", completedTodo.Status);
            Assert.True(completedTodo.IsComplete);
            Assert.NotNull(completedTodo.CompletedDate);

            // Act & Assert - Mark as incomplete again
            completedTodo.IsComplete = false;
            var incompleteTodo = await todoService.UpdateTodoAsync(completedTodo);
            Assert.NotNull(incompleteTodo);
            Assert.Equal("Beklemede", incompleteTodo.Status);
            Assert.False(incompleteTodo.IsComplete);
            Assert.Null(incompleteTodo.CompletedDate);
        }

        [Fact]
        public async Task DatabaseContext_ShouldPersistDataCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                UserName = "persistencetest",
                PasswordHash = "hashedpassword",
                Role = "User"
            };

            var todo = new Todo
            {
                Title = "Persistence Test Todo",
                UserId = userId,
                IsComplete = false,
                Status = "Beklemede"
            };

            // Act
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _context.Todos.Add(todo);
            await _context.SaveChangesAsync();

            // Clear context to ensure data is read from database
            _context.ChangeTracker.Clear();

            // Assert
            var retrievedUser = await _context.Users.FindAsync(userId);
            var retrievedTodo = await _context.Todos.FindAsync(todo.Id);

            Assert.NotNull(retrievedUser);
            Assert.NotNull(retrievedTodo);
            Assert.Equal(user.UserName, retrievedUser.UserName);
            Assert.Equal(todo.Title, retrievedTodo.Title);
            Assert.Equal(userId, retrievedTodo.UserId);
        }

        public void Dispose()
        {
            _scope?.Dispose();
            _client?.Dispose();
        }
    }
} 