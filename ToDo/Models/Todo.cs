using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDo.Models
{
    public class Todo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public bool IsComplete { get; set; }

        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CompletedDate { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        public int DisplayOrder { get; set; }
        
        // Todo durumu için enum yerine stringe çevirelim
        public string Status { get; set; } = "Beklemede"; // Beklemede, Tamamlandı, İptal Edildi, vs.
        
        // Foreign key for User
        public Guid? UserId { get; set; }
        
        // Navigation property
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}