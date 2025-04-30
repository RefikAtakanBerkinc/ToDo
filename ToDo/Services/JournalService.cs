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

        public async Task<bool> EntryExistsForDateAsync(DateTime date)
        {
            return await _context.Journals
                .AnyAsync(j => j.EntryDate.Date == date.Date);
        }

        public async Task<JournalEntry> AddEntryAsync(JournalEntry entry)
        {
            // Aynı tarih için giriş var mı kontrol et
            bool exists = await EntryExistsForDateAsync(entry.EntryDate);
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
                bool exists = await EntryExistsForDateAsync(entry.EntryDate);
                if (exists)
                {
                    throw new InvalidOperationException($"Hedef tarih için zaten bir günlük kaydı mevcut: {entry.EntryDate.ToShortDateString()}");
                }
            }

            existingEntry.EntryDate = entry.EntryDate;
            existingEntry.Content = entry.Content;
            existingEntry.Mood = entry.Mood;
            existingEntry.ModifiedDate = DateTime.Now;

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
        public async Task<List<JournalEntry>> GetEntriesByMonthAsync(DateTime month)
        {
            return await _context.Journals
                .Where(e => e.EntryDate.Year == month.Year && e.EntryDate.Month == month.Month)
                .OrderBy(e => e.EntryDate) // Optional: order entries by date
                .ToListAsync();
        }
    }
}