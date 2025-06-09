using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ToDo.Models;
using ToDo.Services;
using Xunit;

namespace ToDo.UnitTests
{
    public class TodoServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TodoService _todoService;

        public TodoServiceTests()
        {
            // Setup in-memory database for each test
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            _todoService = new TodoService(_context);
        }

        [Fact]
        public async Task GetTodosAsync_ShouldReturnAllTodos()
        {
            // Arrange
            var todos = new List<Todo>
            {
                new Todo { Id = 1, Title = "Test Todo 1", IsComplete = false },
                new Todo { Id = 2, Title = "Test Todo 2", IsComplete = true }
            };
            
            _context.Todos.AddRange(todos);
            await _context.SaveChangesAsync();

            // Act
            var result = await _todoService.GetTodosAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.Title == "Test Todo 1");
            Assert.Contains(result, t => t.Title == "Test Todo 2");
        }

        [Fact]
        public async Task GetUserTodosAsync_ShouldReturnUserSpecificTodos()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            
            var todos = new List<Todo>
            {
                new Todo { Id = 1, Title = "User Todo", UserId = userId },
                new Todo { Id = 2, Title = "Other User Todo", UserId = otherUserId },
                new Todo { Id = 3, Title = "Another User Todo", UserId = userId }
            };
            
            _context.Todos.AddRange(todos);
            await _context.SaveChangesAsync();

            // Act
            var result = await _todoService.GetUserTodosAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, t => Assert.Equal(userId, t.UserId));
        }

        [Fact]
        public async Task GetUserCompletedTodosAsync_ShouldReturnOnlyCompletedTodos()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var completedDate = DateTime.Now.AddDays(-1);
            
            var todos = new List<Todo>
            {
                new Todo { Id = 1, Title = "Completed Todo", UserId = userId, IsComplete = true, CompletedDate = completedDate },
                new Todo { Id = 2, Title = "Incomplete Todo", UserId = userId, IsComplete = false },
                new Todo { Id = 3, Title = "Another Completed", UserId = userId, IsComplete = true, CompletedDate = DateTime.Now }
            };
            
            _context.Todos.AddRange(todos);
            await _context.SaveChangesAsync();

            // Act
            var result = await _todoService.GetUserCompletedTodosAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, t => Assert.True(t.IsComplete));
            Assert.All(result, t => Assert.Equal(userId, t.UserId));
        }

        [Fact]
        public async Task GetUserOverdueTodosAsync_ShouldReturnOverdueTodos()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pastDate = DateTime.Today.AddDays(-1);
            var futureDate = DateTime.Today.AddDays(1);
            
            var todos = new List<Todo>
            {
                new Todo { Id = 1, Title = "Overdue Todo", UserId = userId, IsComplete = false, DueDate = pastDate },
                new Todo { Id = 2, Title = "Future Todo", UserId = userId, IsComplete = false, DueDate = futureDate },
                new Todo { Id = 3, Title = "Completed Overdue", UserId = userId, IsComplete = true, DueDate = pastDate }
            };
            
            _context.Todos.AddRange(todos);
            await _context.SaveChangesAsync();

            // Act
            var result = await _todoService.GetUserOverdueTodosAsync(userId);

            // Assert
            Assert.Single(result);
            Assert.Equal("Overdue Todo", result[0].Title);
            Assert.False(result[0].IsComplete);
            Assert.True(result[0].DueDate < DateTime.Today);
        }

        [Fact]
        public async Task GetTodoByIdAsync_ExistingId_ShouldReturnTodo()
        {
            // Arrange
            var todo = new Todo { Id = 1, Title = "Test Todo" };
            _context.Todos.Add(todo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _todoService.GetTodoByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Todo", result.Title);
        }

        [Fact]
        public async Task GetTodoByIdAsync_NonExistingId_ShouldReturnNull()
        {
            // Act
            var result = await _todoService.GetTodoByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddTodoAsync_ShouldCreateTodoWithDefaults()
        {
            // Arrange
            var todo = new Todo
            {
                Title = "New Todo",
                UserId = Guid.NewGuid()
            };

            // Act
            var result = await _todoService.AddTodoAsync(todo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Todo", result.Title);
            Assert.Equal("Beklemede", result.Status);
            Assert.True(result.CreatedDate <= DateTime.Now);
            Assert.True(result.CreatedDate >= DateTime.Now.AddMinutes(-1));
        }

        [Fact]
        public async Task AddTodoAsync_WithDueDateInPast_ShouldSetStatusToGecikmiş()
        {
            // Arrange
            var todo = new Todo
            {
                Title = "Overdue Todo",
                DueDate = DateTime.Today.AddDays(-1),
                IsComplete = false
            };

            // Act
            var result = await _todoService.AddTodoAsync(todo);

            // Assert
            Assert.Equal("Gecikmiş", result.Status);
        }

        [Fact]
        public async Task UpdateTodoAsync_ExistingTodo_ShouldUpdateSuccessfully()
        {
            // Arrange
            var originalTodo = new Todo { Id = 1, Title = "Original Title", Status = "Beklemede" };
            _context.Todos.Add(originalTodo);
            await _context.SaveChangesAsync();

            var updatedTodo = new Todo
            {
                Id = 1,
                Title = "Updated Title",
                IsComplete = true,
                Status = "Tamamlandı"
            };

            // Act
            var result = await _todoService.UpdateTodoAsync(updatedTodo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            Assert.True(result.IsComplete);
            Assert.Equal("Tamamlandı", result.Status);
            Assert.NotNull(result.CompletedDate);
        }

        [Fact]
        public async Task UpdateTodoAsync_NonExistingTodo_ShouldReturnNull()
        {
            // Arrange
            var todo = new Todo { Id = 999, Title = "Non-existing" };

            // Act
            var result = await _todoService.UpdateTodoAsync(todo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTodoAsync_MarkAsComplete_ShouldSetCompletedDate()
        {
            // Arrange
            var todo = new Todo { Id = 1, Title = "Test Todo", IsComplete = false };
            _context.Todos.Add(todo);
            await _context.SaveChangesAsync();

            var updateTodo = new Todo { Id = 1, Title = "Test Todo", IsComplete = true };

            // Act
            var result = await _todoService.UpdateTodoAsync(updateTodo);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsComplete);
            Assert.Equal("Tamamlandı", result.Status);
            Assert.NotNull(result.CompletedDate);
        }

        [Fact]
        public async Task UpdateTodoAsync_MarkAsIncomplete_ShouldClearCompletedDate()
        {
            // Arrange
            var todo = new Todo 
            { 
                Id = 1, 
                Title = "Test Todo", 
                IsComplete = true, 
                CompletedDate = DateTime.Now,
                Status = "Tamamlandı"
            };
            _context.Todos.Add(todo);
            await _context.SaveChangesAsync();

            var updateTodo = new Todo { Id = 1, Title = "Test Todo", IsComplete = false };

            // Act
            var result = await _todoService.UpdateTodoAsync(updateTodo);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsComplete);
            Assert.Equal("Beklemede", result.Status);
            Assert.Null(result.CompletedDate);
        }

        [Fact]
        public async Task DeleteTodoAsync_ExistingTodo_ShouldReturnTrue()
        {
            // Arrange
            var todo = new Todo { Id = 1, Title = "To Delete" };
            _context.Todos.Add(todo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _todoService.DeleteTodoAsync(1);

            // Assert
            Assert.True(result);
            var deletedTodo = await _context.Todos.FindAsync(1);
            Assert.Null(deletedTodo);
        }

        [Fact]
        public async Task DeleteTodoAsync_NonExistingTodo_ShouldReturnFalse()
        {
            // Act
            var result = await _todoService.DeleteTodoAsync(999);

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
} 