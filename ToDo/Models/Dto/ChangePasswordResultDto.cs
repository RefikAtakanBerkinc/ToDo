namespace ToDo.Models.Dto
{
    public class ChangePasswordResultDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
} 