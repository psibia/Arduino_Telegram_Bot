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
    public class NotificationSubscriberCommand : IAuthorizedCommand
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ISerialPortService _serialPortService;

        public string Name { get; set; }

        public NotificationSubscriberCommand(ITelegramBotClient botClient, ISerialPortService serialPortService)
        {
            _botClient = botClient;
            _serialPortService = serialPortService;
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            // Извлекаем название команды из текста сообщения
            string commandToSubscribe = message.Text.Substring(Name.Length).Trim();

            if (string.IsNullOrEmpty(commandToSubscribe))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, укажите название команды для подписки после имени команды.");
                return;
            }

            var result = await _serialPortService.Subscribe(message.Chat.Id, commandToSubscribe);
            await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
        }

        public static NotificationSubscriberCommand Create(IServiceProvider serviceProvider, string name)
        {
            return new NotificationSubscriberCommand(
                serviceProvider.GetRequiredService<ITelegramBotClient>(),
                serviceProvider.GetRequiredService<ISerialPortService>())
            {
                Name = name
            };
        }
    }
}
