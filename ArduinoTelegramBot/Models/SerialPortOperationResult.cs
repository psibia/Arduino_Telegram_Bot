namespace ArduinoTelegramBot.Models
{
    public class SerialPortOperationResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }
        public string[] Data { get; private set; }

        private SerialPortOperationResult(bool success, string message, string[] data = null)
        {
            Success = success;
            Message = message;
            Data = data;
        }

        public static SerialPortOperationResult Ok(string message, string[] data = null) => new SerialPortOperationResult(true, message, data);

        public static SerialPortOperationResult Error(string message) => new SerialPortOperationResult(false, message);
    }
}
