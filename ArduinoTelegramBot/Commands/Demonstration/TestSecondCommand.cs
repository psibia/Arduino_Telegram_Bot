using ArduinoTelegramBot.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace ArduinoTelegramBot.Commands.Demonstration
{
    public class TestSecondCommand : IAuthorizedCommand
    {
        public string Name { get; set; }
        public static TestSecondCommand Create(IServiceProvider serviceProvider, string name)
        {
            return new TestSecondCommand()
            {
                Name = name,
            };
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var arg = message.Text.Substring(Name.Length).Trim();
            await botClient.SendTextMessageAsync(message.Chat.Id, $"Привет! Это вторая команда, аргументы: {arg}");
        }
    }
}
