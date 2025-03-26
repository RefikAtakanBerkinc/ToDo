using ToDo.Models;
using ToDo.Data;
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

        public async Task<Todo?> GetTodoByIdAsync(int id)
        {
            return await _context.Todos.FindAsync(id);
        }

        public async Task<Todo> AddTodoAsync(Todo todo)
        {
            todo.CreatedDate = DateTime.Now;
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
    }
}