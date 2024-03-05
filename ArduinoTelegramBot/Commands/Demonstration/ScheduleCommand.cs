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
using ArduinoTelegramBot.Commands.Arduino;

namespace ArduinoTelegramBot.Commands.Demonstration
{
    public class ScheduleCommand : IAuthorizedCommand
    {
        private readonly ISchedulerService _schedulerService;
        private IServiceProvider _serviceProvider;
        public string Name { get; set; } = "/schedule";

        public ScheduleCommand(ISchedulerService schedulerService)
        {
            _schedulerService = schedulerService;
        }

        public static ScheduleCommand Create(IServiceProvider serviceProvider, string name)
        {
            var command = new ScheduleCommand(
                serviceProvider.GetRequiredService<ISchedulerService>())
            {
                _serviceProvider = serviceProvider,
                Name = name
            };
            return command;
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var parts = message.Text.Split(' ');
            if (parts.Length != 3)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Неверный формат команды. Используйте /schedule [имя_команды] [интервал_в_формате_ЧЧ:ММ]");
                return;
            }

            var commandName = parts[1];
            var intervalStr = parts[2];
            if (!TimeSpan.TryParse(intervalStr, out var interval))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Неверный формат интервала. Используйте формат ЧЧ:ММ");
                return;
            }

            var command = GetCommandByName(commandName);
            if (command == null)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Команда {commandName} не найдена.");
                return;
            }

            _schedulerService.ScheduleCommand(command, message.Chat.Id.ToString(), interval);
            await botClient.SendTextMessageAsync(message.Chat.Id, $"Команда {commandName} запланирована с интервалом {intervalStr}.");
        }

        private IAuthorizedCommand GetCommandByName(string commandName)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var commands = scope.ServiceProvider.GetServices<IAuthorizedCommand>();
                return commands.FirstOrDefault(c => c.Name == commandName);
            }
        }
    }
}
