using Microsoft.EntityFrameworkCore;
using ToDo.Models;

namespace ToDo.Services
{
    public class JournalService
    {
        private readonly ApplicationDbContext _context;

        public JournalService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<JournalEntry>> GetAllEntriesAsync()
        {
            return await _context.Journals
                .OrderByDescending(j => j.EntryDate)
                .ToListAsync();
        }
        
        public async Task<List<JournalEntry>> GetUserEntriesAsync(Guid userId)
        {
            return await _context.Journals
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.EntryDate)
                .ToListAsync();
        }

        public async Task<JournalEntry?> GetEntryByIdAsync(int id)
        {
            return await _context.Journals.FindAsync(id);
        }

        public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
        {
            // Sadece tarihi karşılaştır, saati dikkate alma
            return await _context.Journals
                .FirstOrDefaultAsync(j => j.EntryDate.Date == date.Date);
        }
        
        public async Task<JournalEntry?> GetUserEntryByDateAsync(Guid userId, DateTime date)
        {
            // Compare only the date part, not the time
            return await _context.Journals
                .FirstOrDefaultAsync(j => j.UserId == userId && j.EntryDate.Date == date.Date);
        }

        public async Task<bool> EntryExistsForDateAsync(DateTime date)
        {
            return await _context.Journals
                .AnyAsync(j => j.EntryDate.Date == date.Date);
        }
        
        public async Task<bool> UserEntryExistsForDateAsync(Guid userId, DateTime date)
        {
            return await _context.Journals
                .AnyAsync(j => j.UserId == userId && j.EntryDate.Date == date.Date);
        }

        public async Task<JournalEntry> AddEntryAsync(JournalEntry entry)
        {
            // Aynı tarih için giriş var mı kontrol et
            bool exists = false;
            
            if (entry.UserId.HasValue)
            {
                exists = await UserEntryExistsForDateAsync(entry.UserId.Value, entry.EntryDate);
            }
            else
            {
                exists = await EntryExistsForDateAsync(entry.EntryDate);
            }
            
            if (exists)
            {
                throw new InvalidOperationException($"Bu tarih için zaten bir günlük kaydı mevcut: {entry.EntryDate.ToShortDateString()}");
            }

            entry.CreatedDate = DateTime.Now;
            _context.Journals.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<JournalEntry?> UpdateEntryAsync(JournalEntry entry)
        {
            var existingEntry = await _context.Journals.FindAsync(entry.Id);
            if (existingEntry == null)
                return null;

            // Tarih değiştiriliyorsa, hedef tarihte başka giriş olup olmadığını kontrol et
            if (existingEntry.EntryDate.Date != entry.EntryDate.Date)
            {
                bool exists = false;
                
                if (entry.UserId.HasValue)
                {
                    exists = await UserEntryExistsForDateAsync(entry.UserId.Value, entry.EntryDate);
                }
                else
                {
                    exists = await EntryExistsForDateAsync(entry.EntryDate);
                }
                
                if (exists)
                {
                    throw new InvalidOperationException($"Hedef tarih için zaten bir günlük kaydı mevcut: {entry.EntryDate.ToShortDateString()}");
                }
            }

            existingEntry.EntryDate = entry.EntryDate;
            existingEntry.Content = entry.Content;
            existingEntry.Mood = entry.Mood;
            existingEntry.ModifiedDate = DateTime.Now;
            existingEntry.UserId = entry.UserId;

            await _context.SaveChangesAsync();
            return existingEntry;
        }

        public async Task<bool> DeleteEntryAsync(int id)
        {
            var entry = await _context.Journals.FindAsync(id);
            if (entry == null)
                return false;

            _context.Journals.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DateTime>> GetDatesWithEntriesAsync(int year, int month)
        {
            // Belirli ay içinde hangi günlerde kayıt olduğunu dön
            // Takvim görünümü için kullanışlı
            return await _context.Journals
                .Where(j => j.EntryDate.Year == year && j.EntryDate.Month == month)
                .Select(j => j.EntryDate.Date)
                .ToListAsync();
        }
        
        public async Task<List<DateTime>> GetUserDatesWithEntriesAsync(Guid userId, int year, int month)
        {
            // Return dates with entries for a specific user in a given month
            return await _context.Journals
                .Where(j => j.UserId == userId && j.EntryDate.Year == year && j.EntryDate.Month == month)
                .Select(j => j.EntryDate.Date)
                .ToListAsync();
        }
        
        public async Task<List<JournalEntry>> GetEntriesByMonthAsync(DateTime month)
        {
            return await _context.Journals
                .Where(e => e.EntryDate.Year == month.Year && e.EntryDate.Month == month.Month)
                .OrderBy(e => e.EntryDate) // Optional: order entries by date
                .ToListAsync();
        }
        
        public async Task<List<JournalEntry>> GetUserEntriesByMonthAsync(Guid userId, DateTime month)
        {
            return await _context.Journals
                .Where(e => e.UserId == userId && e.EntryDate.Year == month.Year && e.EntryDate.Month == month.Month)
                .OrderBy(e => e.EntryDate)
                .ToListAsync();
        }
    }
}