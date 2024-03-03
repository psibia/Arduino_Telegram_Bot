namespace ArduinoTelegramBot.Handlers.Interfaces
{
    public interface ISerialDataHandler
    {
        Task HandleReceivedDataAsync(string data, long chatId);
    }
}
