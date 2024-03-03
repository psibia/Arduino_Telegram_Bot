namespace ArduinoTelegramBot.Models
{
    public class ActionStatusResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }
        public string[] Data { get; private set; }

        private ActionStatusResult(bool success, string message, string[] data = null)
        {
            Success = success;
            Message = message;
            Data = data;
        }

        public static ActionStatusResult Ok(string message, string[] data = null) => new ActionStatusResult(true, message, data);

        public static ActionStatusResult Error(string message) => new ActionStatusResult(false, message);
    }
}
