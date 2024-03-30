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
    public class ShowSubscriptionsCommand : IAuthorizedCommand
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ISerialPortService _serialPortService;

        public string Name { get; set; }

        public ShowSubscriptionsCommand(ITelegramBotClient botClient, ISerialPortService serialPortService)
        {
            _botClient = botClient;
            _serialPortService = serialPortService;
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var userSubscriptions = await _serialPortService.GetUserSubscriptionsAsync(message.Chat.Id);
            string responseMessage = userSubscriptions.Any() ? $"Ваши текущие подписки: \n{string.Join("\n", userSubscriptions)}" : "У вас нет активных подписок.";
            await botClient.SendTextMessageAsync(message.Chat.Id, responseMessage);
        }

        public static ShowSubscriptionsCommand Create(IServiceProvider serviceProvider, string name)
        {
            return new ShowSubscriptionsCommand(
                serviceProvider.GetRequiredService<ITelegramBotClient>(),
                serviceProvider.GetRequiredService<ISerialPortService>())
            {
                Name = name
            };
        }
    }
}
