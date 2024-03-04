using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace ArduinoTelegramBot.Commands.Arduino
{
    public class SendBinaryDataCommand : IAuthorizedCommand
    {
        public string Name { get; set; } = "/binary";
        private readonly ITelegramBotClient _botClient;
        private readonly ISerialPortService _serialPortService;

        public SendBinaryDataCommand(ITelegramBotClient botClient, ISerialPortService serialPortService)
        {
            _botClient = botClient;
            _serialPortService = serialPortService;
        }

        public static SendBinaryDataCommand Create(IServiceProvider serviceProvider, string name)
        {
            return new SendBinaryDataCommand(
                serviceProvider.GetRequiredService<ITelegramBotClient>(),
                serviceProvider.GetRequiredService<ISerialPortService>())
            {
                Name = name
            };
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var dataToSend = message.Text.Substring(Name.Length).Trim();
            if (string.IsNullOrEmpty(dataToSend))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, укажите данные для отправки после команды");
                return;
            }

            //конвертируем строку в байты
            var bytesToSend = Encoding.UTF8.GetBytes(dataToSend);
            var result = await _serialPortService.SendBinaryDataAsync(bytesToSend);
            if (result.Success)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Данные успешно отправлены.");//удалить после тестирования
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
            }
        }
    }
}
