using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArduinoTelegramBot.Processors.Arduino.Interfaces;
using Telegram.Bot;

namespace ArduinoTelegramBot.Processors.Arduino
{
    public class ErrorDataProcessor : ISerialDataProcessor
    {
        //сообщения об ошибке всегда СТРОГО!!!! должно соответствовать формату "ERR: [код_ошибки] [описание]", задается в сервисе последовательного порта.
        //например, "Error: 001 Ошибка чтения данных с датчика"
        public Regex Pattern => new Regex(@"^ERR:\s+(\d{3})\s+(.*)$", RegexOptions.Compiled);

        public async Task ProcessAsync(string data, long chatId, ITelegramBotClient botClient)
        {
            var match = Pattern.Match(data);
            if (match.Success)
            {
                var errorCode = match.Groups[1].Value; //извлечение кода ошибки
                var errorDescription = match.Groups[2].Value; //извлечение описания ошибки

                //логика перевода кода ошибки в понятное описание ошибки
                var errorMessage = $"Обнаружена ошибка на устройстве, подключенном к SerialPort: {errorDescription} (Код: {errorCode})";

                await botClient.SendTextMessageAsync(chatId, errorMessage);
            }
            else
            {
                //в случае, если сообщение не соответствует ожидаемому формату
                await botClient.SendTextMessageAsync(chatId, "Получено некорректное сообщение об ошибке");
            }
        }
    }
}
