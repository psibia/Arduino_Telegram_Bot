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
using System.Globalization;
using Serilog;
using ArduinoTelegramBot.Models.Sheduler;

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
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Неверный формат команды. Используйте {this.Name} {ScheduleInputCommandActions.Help} для справки.");
                return;
            }

            var action = parts[1].ToLower();
            switch (action)
            {
                case ScheduleInputCommandActions.Help:
                    await SendHelpMessageAsync(botClient, message.Chat.Id);
                    break;
                case ScheduleInputCommandActions.Get:
                    await HandleGetScheduledTasksAsync(botClient, message.Chat.Id);
                    break;
                default:
                    await HandleActionAsync(botClient, message, action, parts.Skip(2).ToArray());
                    break;
            }
        }

        private async Task SendHelpMessageAsync(ITelegramBotClient botClient, long chatId)
        {
            var helpMessage = $"Команда {this.Name} позволяет планировать выполнение других команд с определенной периодичностью или в конкретное время. Вот доступные форматы команды:\n" +
                $"{this.Name} {ScheduleInputCommandActions.Interval} [имя_команды] [интервал] - Запланировать выполнение команды с указанным интервалом. Интервал задается в форматах: [ЧЧ], [ЧЧ:ММ], [ЧЧ:ММ:СС] (например, 00:30 для 30 минут).\n" +
                $"{this.Name} {ScheduleInputCommandActions.Daily} [имя_команды] [время] - Запланировать ежедневное выполнение команды в указанное время. Время задается в форматах: [ЧЧ], [ЧЧ:ММ], [ЧЧ:ММ:СС] (например, 08:00 для запуска в 8 утра).\n" +
                $"{this.Name} {ScheduleInputCommandActions.Delete} [имя_команды] - Отменить циклическое выполнение запланированной команды.\n" +
                $"{this.Name} {ScheduleInputCommandActions.Delete} [имя_команды] [время] - Отменить ежедневное выполнение запланированной команды, запланированное на указанное время.\n" +
                $"{this.Name} {ScheduleInputCommandActions.DeleteAll} [имя_команды] - Отменить все запланированные задачи для указанной команды.\n" +
                $"{this.Name} {ScheduleInputCommandActions.Help} - Показать это справочное сообщение.";
            await botClient.SendTextMessageAsync(chatId, helpMessage);
        }

        private async Task HandleActionAsync(ITelegramBotClient botClient, Message message, string action, string[] parameters)
        {
            try
            {
                switch (action)
                {
                    case ScheduleInputCommandActions.Interval:
                        await HandleCycleAsync(botClient, message, parameters);
                        break;
                    case ScheduleInputCommandActions.Daily:
                        await HandleDailyAsync(botClient, message, parameters);
                        break;
                    case ScheduleInputCommandActions.Delete:
                        await HandleDeleteAsync(botClient, message, parameters);
                        break;
                    case ScheduleInputCommandActions.DeleteAll:
                        await HandleDeleteAllAsync(botClient, message, parameters);
                        break;
                    default:
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Введен неверный аргумент, для вызова справки введите {this.Name} {ScheduleInputCommandActions.Help}");
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, ex.Message);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Произошла непредвиденная ошибка при выполнении команды.");
            }
        }


        private async Task HandleCycleAsync(ITelegramBotClient botClient, Message message, string[] parameters)
        {
            if (parameters.Length != 2)
            {
                throw new ArgumentException("Неверное количество аргументов для интервала.");
            }

            var commandName = parameters[0];
            var interval = parameters[1];
            var command = GetCommandByName(commandName);
            if (command == null)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Команда {commandName} не найдена.");
                return;
            }

            if (!TryParseTimeWithStrictFormat(interval, out var intervalTimeSpan))
            {
                throw new ArgumentException("Неверный формат интервала.");
            }

            var result = await _schedulerService.ScheduleCycleTaskAsync(command, message.Chat.Id, intervalTimeSpan);
            await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
        }

        private async Task HandleDailyAsync(ITelegramBotClient botClient, Message message, string[] parameters)
        {
            if (parameters.Length != 2)
            {
                throw new ArgumentException("Неверное количество аргументов для ежедневной задачи.");
            }

            var commandName = parameters[0];
            var time = parameters[1];
            var command = GetCommandByName(commandName);
            if (command == null)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Команда {commandName} не найдена.");
                return;
            }

            if (!TryParseTimeWithStrictFormat(time, out var dailyTimeSpan))
            {
                throw new ArgumentException("Неверный формат времени.");
            }

            var result = await _schedulerService.ScheduleDailyTaskAsync(command, message.Chat.Id, dailyTimeSpan);
            await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
        }

        private async Task HandleDeleteAsync(ITelegramBotClient botClient, Message message, string[] parameters)
        {
            if (parameters.Length == 1)
            {
                // Удаление циклической задачи
                await HandleDeleteCycleAsync(botClient, message, parameters);
            }
            else if (parameters.Length == 2)
            {// Удаление ежедневной задачи на конкретное время
                await HandleDeleteDailyAsync(botClient, message, parameters);
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Неверный формат команды. Используйте {this.Name} {ScheduleInputCommandActions.Help} для справки.");
            }
        }

        private async Task HandleDeleteCycleAsync(ITelegramBotClient botClient, Message message, string[] parameters)
        {
            if (parameters.Length != 1)
            {
                throw new ArgumentException("Неверное количество аргументов для отмены интервала.");
            }

            var commandName = parameters[0];
            var result = await _schedulerService.CancelScheduledCycleTaskAsync(commandName, message.Chat.Id);
            await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
        }

        private async Task HandleDeleteDailyAsync(ITelegramBotClient botClient, Message message, string[] parameters)
        {
            if (parameters.Length != 2)
            {
                throw new ArgumentException("Неверное количество аргументов для отмены ежедневной задачи.");
            }

            var commandName = parameters[0];
            var time = parameters[1];
            if (!TryParseTimeWithStrictFormat(time, out var timeToDelete))
            {
                throw new ArgumentException("Неверный формат времени.");
            }

            var result = await _schedulerService.CancelScheduledDailyTaskAsync(commandName, message.Chat.Id, timeToDelete);
            await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
        }

        private async Task HandleDeleteAllAsync(ITelegramBotClient botClient, Message message, string[] parameters)
        {
            if (parameters.Length != 1)
            {
                throw new ArgumentException("Неверное количество аргументов для удаления.");
            }

            var commandName = parameters[0];
            var result = await _schedulerService.CancelAllScheduledTasksAsync(commandName, message.Chat.Id);
            await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
        }

        private async Task HandleGetScheduledTasksAsync(ITelegramBotClient botClient, long chatId)
        {
            //получение списка задач должно быть асинхронным
            var scheduledTasks = await _schedulerService.GetScheduledTasksForChatAsync(chatId);

            if (scheduledTasks.Count == 0)
            {
                await botClient.SendTextMessageAsync(chatId, "Нет запланированных задач.");
                return;
            }

            var messageBuilder = new StringBuilder("Запланированные задачи:\n");
            foreach (var task in scheduledTasks)
            {
                string taskType = task.CommandType == "Ежедневная" ? "Время" : "Интервал";
                messageBuilder.AppendLine($"- {task.CommandType}: {task.CommandName}, {taskType}: {task.ExecutionTimeOrInterval}");
            }

            await botClient.SendTextMessageAsync(chatId, messageBuilder.ToString());
        }

        private bool TryParseTimeWithStrictFormat(string timeString, out TimeSpan timeSpan)
        {
            timeSpan = default(TimeSpan);
            var formats = new[] { "hh", "hh\\:mm", "hh\\:mm\\:ss" };

            Log.Debug($"Попытка разбора строки времени: '{timeString}'");

            foreach (var format in formats)
            {
                if (TimeSpan.TryParseExact(timeString.Trim(), format, CultureInfo.InvariantCulture, out timeSpan))
                {
                    Log.Debug($"Успешный разбор времени: '{timeSpan}'");
                    return true;
                }
            }

            Log.Debug("Не удалось разобрать строку времени в соответствии с ожидаемыми форматами.");
            return false;
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
