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

namespace ArduinoTelegramBot.Commands.System
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
            var command = new ScheduleCommand(serviceProvider.GetRequiredService<ISchedulerService>())
            {
                _serviceProvider = serviceProvider,
                Name = name
            };
            return command;
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var parts = message.Text.Split(' ');
            if (parts.Length < 2)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Неверный формат команды. Используйте /schedule help для справки.");
                return;
            }

            var action = parts[1].ToLower();
            switch (action)
            {
                case "help":
                    var helpMessage = "Команда /schedule позволяет планировать выполнение других команд с определенной периодичностью или в конкретное время. Вот доступные форматы команды:\n" +           
                        "/schedule interval [имя_команды] [интервал] - Запланировать выполнение команды с указанным интервалом. Интервал задается в формате ЧЧ:ММ (например, 00:30 для 30 минут).\n" +         
                        "/schedule daily [имя_команды] [время] - Запланировать ежедневное выполнение команды в указанное время. Время задается в формате ЧЧ:ММ (например, 08:00 для запуска в 8 утра).\n" +          
                        "/schedule delete_interval [имя_команды] - Отменить циклическое выполнение запланированной команды.\n" +      
                        "/schedule delete_daily [имя_команды] [время] - Отменить ежедневное выполнение запланированной команды, запланированное на указанное время.\n" +         
                        "/schedule delete [имя_команды] - Отменить все запланированные задачи для указанной команды.\n" +           
                        "/schedule help - Показать это справочное сообщение.";
                    await botClient.SendTextMessageAsync(message.Chat.Id, helpMessage);
                    break;
                default:
                    await ExecuteCommandAsync(botClient, message, parts);
                    break;
            }
        }

        private async Task ExecuteCommandAsync(ITelegramBotClient botClient, Message message, string[] parts)
        {
            var action = parts[1].ToLower();
            if (parts.Length < 3)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Недостаточно аргументов для выполнения команды.");
                return;
            }

            var commandName = parts[2];
            IAuthorizedCommand command = null;

            // Получаем команду по имени для действий, кроме удаления
            if (!action.StartsWith("delete"))
            {
                command = GetCommandByName(commandName);
                if (command == null)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Команда {commandName} не найдена.");
                    return;
                }
            }

            try
            {
                switch (action)
                {
                    case "interval":
                        if (parts.Length != 4) throw new ArgumentException("Неверное количество аргументов для интервала.");
                        if (!TimeSpan.TryParse(parts[3], out var interval)) throw new ArgumentException("Неверный формат интервала.");
                        _schedulerService.ScheduleCommand(command, message.Chat.Id.ToString(), interval);
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Команда {commandName} запланирована с интервалом {parts[3]}.");
                        break;
                    case "daily":
                        if (parts.Length != 4) throw new ArgumentException("Неверное количество аргументов для ежедневной задачи.");
                        if (!TimeSpan.TryParse(parts[3], out var dailyTime)) throw new ArgumentException("Неверный формат времени.");
                        _schedulerService.ScheduleDailyTask(command, message.Chat.Id.ToString(), dailyTime);
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Ежедневная команда {commandName} запланирована на {parts[3]}.");
                        break;
                    case "delete_interval":
                        _schedulerService.CancelScheduledCommand(commandName);
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Циклическая задача {commandName} отменена.");
                        break;
                    case "delete_daily":
                        if (parts.Length != 4) throw new ArgumentException("Неверное количество аргументов для отмены ежедневной задачи.");
                        if (!TimeSpan.TryParse(parts[3], out var timeToDelete)) throw new ArgumentException("Неверный формат времени.");
                        _schedulerService.CancelScheduledDailyTask(commandName, timeToDelete);
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Ежедневная задача {commandName} на {parts[3]} отменена.");
                        break;
                    case "delete":
                        _schedulerService.CancelAllScheduledTasks(commandName);
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Все задачи для команды {commandName} отменены.");
                        break;
                    case "help":
                        var helpMessage = "Команда /schedule позволяет планировать выполнение других команд с определенной периодичностью или в конкретное время. Вот доступные форматы команды:\n" +
                            "/schedule interval [имя_команды] [интервал] - Запланировать выполнение команды с указанным интервалом. Интервал задается в формате ЧЧ:ММ (например, 00:30 для 30 минут).\n" +
                            "/schedule daily [имя_команды] [время] - Запланировать ежедневное выполнение команды в указанное время. Время задается в формате ЧЧ:ММ (например, 08:00 для запуска в 8 утра).\n" +
                            "/schedule delete_interval [имя_команды] - Отменить циклическое выполнение запланированной команды.\n" +
                            "/schedule delete_daily [имя_команды] [время] - Отменить ежедневное выполнение запланированной команды, запланированное на указанное время.\n" +
                            "/schedule delete [имя_команды] - Отменить все запланированные задачи для указанной команды.\n" +
                            "/schedule help - Показать это справочное сообщение.";
                        await botClient.SendTextMessageAsync(message.Chat.Id, helpMessage);
                        break;
                    default:
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Неизвестное действие.");
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, ex.Message);
            }
        }

        private IAuthorizedCommand GetCommandByName(string commandName)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var commands = scope.ServiceProvider.GetServices<IAuthorizedCommand>();
                return commands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
