﻿using ArduinoTelegramBot.Processors.Arduino.Interfaces;
using Serilog;
using System.Text.RegularExpressions;
using Telegram.Bot;

namespace ArduinoTelegramBot.Processors.Arduino
{
    public class DefaultDataProcessor : ISerialDataProcessor
    {
        public Regex Pattern => new Regex("^.*$", RegexOptions.Compiled); //регулярное выражение, соответствующее любым данным!!!!!

        public async Task ProcessAsync(string data, long chatId, ITelegramBotClient botClient)
        {
            await botClient.SendTextMessageAsync(chatId, "Не найден процессор для данных, полученных из последовательного порта (использован процессор по умолчанию):\n" + data);
            Log.Information("DefaultDataProcessor: Получен неизвестны тип данных в {chatId}", chatId);
        }
    }
}





// [prefix]:[identificator]:[body]

// "REQ:Ufdfs-43423-3442-fsddsfsdfsafds:hello, arduino" 

// REQ, RES, NOT, ERR