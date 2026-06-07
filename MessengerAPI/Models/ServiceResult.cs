namespace MessengerAPI.Models
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public object? Data { get; set; }

        public static ServiceResult SuccessResult(string message = "Успех", object? data = null)
        {
            return new ServiceResult { Success = true, Message = message, Data = data };
        }

        public static ServiceResult ErrorResult(string message)
        {
            return new ServiceResult { Success = false, Message = message };
        }
    }
}
