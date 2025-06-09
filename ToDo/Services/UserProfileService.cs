using Microsoft.EntityFrameworkCore;
using ToDo.Models.Dto;

namespace ToDo.Services
{
    public class UserProfileService
    {
        private readonly ApplicationDbContext _context;

        public UserProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfileStatsDto?> GetUserProfileStatsAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            // Todo istatistikleri
            var todos = await _context.Todos
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var completedTodos = todos.Where(t => t.IsComplete || t.Status == "Tamamlandı").Count();
            var pendingTodos = todos.Where(t => !t.IsComplete && t.Status == "Beklemede").Count();
            var cancelledTodos = todos.Where(t => t.Status == "İptal Edildi").Count();

            // Journal istatistikleri
            var journalEntries = await _context.Journals
                .Where(j => j.UserId == userId)
                .OrderBy(j => j.EntryDate)
                .ToListAsync();

            var journalDates = journalEntries
                .Select(j => j.EntryDate.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var lastJournalEntry = journalEntries
                .OrderByDescending(j => j.EntryDate)
                .FirstOrDefault()?.EntryDate;

            var lastTodoCompleted = todos
                .Where(t => t.IsComplete && t.CompletedDate.HasValue)
                .OrderByDescending(t => t.CompletedDate)
                .FirstOrDefault()?.CompletedDate;

            return new UserProfileStatsDto
            {
                UserName = user.UserName,
                TotalTodos = todos.Count,
                CompletedTodos = completedTodos,
                PendingTodos = pendingTodos,
                CancelledTodos = cancelledTodos,
                TotalJournalEntries = journalEntries.Count,
                JournalDates = journalDates,
                LastJournalEntry = lastJournalEntry,
                LastTodoCompleted = lastTodoCompleted
            };
        }
    }
} 