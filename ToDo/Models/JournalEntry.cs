using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDo.Models
{
    public class JournalEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime EntryDate { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? Mood { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }
        
        // Foreign key for User
        public Guid? UserId { get; set; }
        
        // Navigation property
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}