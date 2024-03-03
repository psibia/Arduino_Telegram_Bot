using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ArduinoTelegramBot.Processors.Arduino.Interfaces
{
    public interface ISerialDataProcessor
    {
        Regex Pattern { get; }
        Task ProcessAsync(string data, long chatId, ITelegramBotClient botClient);
    }
}
