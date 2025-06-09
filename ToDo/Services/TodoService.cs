using ToDo.Models;
using Microsoft.EntityFrameworkCore;

namespace ToDo.Services
{
    public class TodoService
    {
        private readonly ApplicationDbContext _context;

        public TodoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Todo>> GetTodosAsync()
        {
            return await _context.Todos.ToListAsync();
        }
        
        public async Task<List<Todo>> GetUserTodosAsync(Guid userId)
        {
            return await _context.Todos
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }
        
        public async Task<List<Todo>> GetUserCompletedTodosAsync(Guid userId)
        {
            return await _context.Todos
                .Where(t => t.UserId == userId && t.IsComplete)
                .OrderByDescending(t => t.CompletedDate)
                .ToListAsync();
        }
        
        public async Task<List<Todo>> GetUserOverdueTodosAsync(Guid userId)
        {
            return await _context.Todos
                .Where(t => t.UserId == userId && !t.IsComplete && t.DueDate.HasValue && t.DueDate.Value < DateTime.Today)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<Todo?> GetTodoByIdAsync(int id)
        {
            return await _context.Todos.FindAsync(id);
        }

        public async Task<Todo> AddTodoAsync(Todo todo)
        {
            todo.CreatedDate = DateTime.Now;
            
            // Eğer varsayılan değerler ayarlanmamışsa burada ayarla
            if (string.IsNullOrEmpty(todo.Status))
            {
                todo.Status = "Beklemede";
            }
            
            // Son teslim tarihi status değişimlerine göre güncelle
            UpdateTodoStatusBasedOnCompletion(todo);
            
            _context.Todos.Add(todo);
            await _context.SaveChangesAsync();
            return todo;
        }

        public async Task<Todo?> UpdateTodoAsync(Todo todo)
        {
            var existingTodo = await _context.Todos.FindAsync(todo.Id);
            if (existingTodo == null)
                return null;

            existingTodo.Title = todo.Title;
            existingTodo.IsComplete = todo.IsComplete;
            existingTodo.CompletedDate = todo.CompletedDate;
            existingTodo.DisplayOrder = todo.DisplayOrder;
            existingTodo.UserId = todo.UserId;
            existingTodo.DueDate = todo.DueDate;
            existingTodo.Status = todo.Status;
            
            // Status ve IsComplete arasındaki tutarlılığı sağla
            UpdateTodoStatusBasedOnCompletion(existingTodo);

            await _context.SaveChangesAsync();
            return existingTodo;
        }

        public async Task<bool> DeleteTodoAsync(int id)
        {
            var todo = await _context.Todos.FindAsync(id);
            if (todo == null)
                return false;

            _context.Todos.Remove(todo);
            await _context.SaveChangesAsync();
            return true;
        }
        
        private void UpdateTodoStatusBasedOnCompletion(Todo todo)
        {
            // Varsayılan olarak tamamlanmayan görevler "Beklemede" olarak ayarlanır
            if (!todo.IsComplete && string.IsNullOrEmpty(todo.Status))
            {
                todo.Status = "Beklemede";
            }

            // IsComplete ve Status arasındaki tutarlılığı sağla
            if (todo.IsComplete)
            {
                if (todo.Status != "Tamamlandı")
                {
                    todo.Status = "Tamamlandı";
                }
                // Eğer CompletedDate boşsa, şimdi ata
                todo.CompletedDate ??= DateTime.Now;
            }
            else if (!todo.IsComplete && todo.Status == "Tamamlandı")
            {
                todo.Status = "Beklemede";
                todo.CompletedDate = null;
            }
            
            // Son teslim tarihi geçmiş ve tamamlanmamış görevleri "Gecikmiş" olarak işaretle
            if (!todo.IsComplete && todo.DueDate.HasValue && todo.DueDate.Value < DateTime.Today)
            {
                // Status "İptal Edildi" değilse güncelle
                if (todo.Status != "İptal Edildi")
                {
                    todo.Status = "Gecikmiş";
                }
            }
        }
    }
}