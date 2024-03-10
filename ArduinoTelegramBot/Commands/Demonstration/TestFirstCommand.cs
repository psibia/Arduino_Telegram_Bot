using ArduinoTelegramBot.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using ArduinoTelegramBot.Services;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ArduinoTelegramBot.Commands.Demonstration
{
    public class TestFirstCommand : IAuthorizedCommand
    {
        private readonly ISchedulerService _schedulerService;
        public string Name { get; set; }
        public static TestFirstCommand Create(IServiceProvider serviceProvider, string name)
        {
            return new TestFirstCommand(
                serviceProvider.GetRequiredService<ISchedulerService>())
            {
                Name = name,
            };
        }
        public TestFirstCommand(ISchedulerService schedulerService)
        {
            _schedulerService = schedulerService;
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var arg = message.Text.Substring(Name.Length).Trim();
            await botClient.SendTextMessageAsync(message.Chat.Id, $"Это первая тестовая команда, аргументы: {arg}");
        }
    }
}
