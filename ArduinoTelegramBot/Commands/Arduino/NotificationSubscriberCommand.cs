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

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var result = await _serialPortService.Subscribe(message.Chat.Id);
            await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
        }

        public NotificationSubscriberCommand(ITelegramBotClient botClient, ISerialPortService serialPortService)
        {
            _botClient = botClient;
            _serialPortService = serialPortService;
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
