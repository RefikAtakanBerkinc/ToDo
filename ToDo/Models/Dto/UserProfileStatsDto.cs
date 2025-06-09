namespace ToDo.Models.Dto
{
    public class UserProfileStatsDto
    {
        public string UserName { get; set; } = string.Empty;
        public int TotalTodos { get; set; }
        public int CompletedTodos { get; set; }
        public int PendingTodos { get; set; }
        public int CancelledTodos { get; set; }
        public int TotalJournalEntries { get; set; }
        public List<DateTime> JournalDates { get; set; } = new List<DateTime>();
        public DateTime? LastJournalEntry { get; set; }
        public DateTime? LastTodoCompleted { get; set; }
    }
} 