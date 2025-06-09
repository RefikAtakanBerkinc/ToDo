namespace ToDo.Models.Dto
{
    public class RegisterResultDto
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
    }
    
    public static class RegisterErrorCodes
    {
        public const string EmptyFields = "EMPTY_FIELDS";
        public const string UserExists = "USER_EXISTS";
        public const string InvalidInput = "INVALID_INPUT";
    }
} 