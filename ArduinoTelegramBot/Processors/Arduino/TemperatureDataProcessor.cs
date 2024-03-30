using ArduinoTelegramBot.Processors.Arduino.Interfaces;
using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ArduinoTelegramBot.Processors.Arduino
{
    public class TemperatureDataProcessor : ISerialDataProcessor
    {
        public Regex Pattern => new Regex(@"^temperature\s+(-?\d+)\s+(\d+)$", RegexOptions.Compiled);

        public async Task ProcessAsync(string data, long chatId, ITelegramBotClient botClient)
        {
            var match = Pattern.Match(data);
            if (match.Success)
            {
                var temperature = match.Groups[1].Value; // Извлечение значения температуры
                var humidity = match.Groups[2].Value; // Извлечение значения влажности
                await botClient.SendTextMessageAsync(chatId, $"Температура: {temperature}°C, Влажность: {humidity}%");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Некорректные данные о температуре и влажности");
            }
        }
    }
}