using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types;
using Telegram.Bot;
using Serilog;
using ArduinoTelegramBot.Models;

namespace ArduinoTelegramBot.Services
{
    public class SchedulerService : ISchedulerService, IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPermissionsDatabaseService _permissionsDatabaseService;
        private readonly ICommandFactory _commandFactory;
        private List<SchedulerTimerInfo> _timers = new List<SchedulerTimerInfo>();

        public SchedulerService(IServiceProvider serviceProvider, IPermissionsDatabaseService permissionsDatabaseService, ICommandFactory commandFactory)
        {
            _serviceProvider = serviceProvider;
            _permissionsDatabaseService = permissionsDatabaseService;
            Log.Information("Планировщик задач: Сервис инициализирован");
            _commandFactory = commandFactory;
        }

        public ScheduleOperationResult ScheduleCommand(IAuthorizedCommand command, string chatId, TimeSpan interval)
        {
            if (_timers.Any(t => t.Command.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase) && t.ChatId == chatId))
            {
                return ScheduleOperationResult.Error($"Циклическое выполнение команды {command.Name} уже запланировано");
            }

            var schedulerTimerInfo = new SchedulerTimerInfo
            {
                Command = command,
                ChatId = chatId,
                Interval = interval,
                DailyTime = null
            };

            //var timer = new Timer(Callback, schedulerTimerInfo, TimeSpan.Zero, interval); //эта реализация позволит запустить команду сразу в момент постановки задачи
            var timer = new Timer(Callback, schedulerTimerInfo, interval, interval); 
            schedulerTimerInfo.Timer = timer;
            _timers.Add(schedulerTimerInfo);

            var taskData = new ScheduledTaskData
            {
                CommandName = command.Name,
                ChatId = chatId,
                Interval = interval,
                DailyTime = null
            };

            _permissionsDatabaseService.SaveScheduledTaskAsync(taskData).Wait();

            Log.Information("Планировщик задач: Команда {CommandName} запланирована для чата {ChatId} с интервалом {Interval}", command.Name, chatId, interval);
            return ScheduleOperationResult.Ok($"Запланировано циклическое выполнение команды {command.Name} с интервалом {interval}");
        }

        public ScheduleOperationResult ScheduleDailyTask(IAuthorizedCommand command, string chatId, TimeSpan dailyTime)
        {
            if (_timers.Any(t => t.Command.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase) && t.ChatId == chatId && t.DailyTime == dailyTime))
            {
                return ScheduleOperationResult.Error($"Ежедневная задача {command.Name} уже запланирована для чата {chatId} на {dailyTime}.");
            }
            var now = DateTime.Now;
            var firstRunTime = now.TimeOfDay > dailyTime ? now.Date.AddDays(1).Add(dailyTime) : now.Date.Add(dailyTime);
            var initialDelay = firstRunTime - now;
            //var timer = new Timer(Callback, new SchedulerTimerInfo { Command = command, ChatId = chatId, DailyTime = dailyTime }, initialDelay, TimeSpan.FromDays(1));
            var timer = new Timer(Callback, new SchedulerTimerInfo { Command = command, ChatId = chatId, DailyTime = dailyTime, Interval = TimeSpan.FromDays(1) }, initialDelay, TimeSpan.FromDays(1));
            _timers.Add(new SchedulerTimerInfo { Timer = timer, ChatId = chatId, Command = command, DailyTime = dailyTime });
            Log.Information("Планировщик задач: Ежедневная задача {CommandName} запланирована для чата {ChatId} на {DailyTime}", command.Name, chatId, dailyTime);
            return ScheduleOperationResult.Ok($"Ежедневная задача {command.Name} успешно запланирована на {dailyTime}.");
        }

        public ScheduleOperationResult CancelScheduledCommand(string commandName, string chatId)
        {
            var timersToRemove = _timers.Where(t => t.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) && t.ChatId == chatId).ToList();
            if (!timersToRemove.Any())
            {
                return ScheduleOperationResult.Error($"Задача на циклическое выполнение команды {commandName} не найдена.");
            }

            foreach (var timerInfo in timersToRemove)
            {
                timerInfo.Timer.Dispose();
                _timers.Remove(timerInfo);
                Log.Information("Планировщик задач: Отмена циклической задачи {CommandName} для chatId {ChatId}", commandName, chatId);
                //удаление задачи из файла базы данных
                Task.Run(async () => await _permissionsDatabaseService.DeleteScheduledTaskAsync(commandName, chatId)).Wait();
            }

            return ScheduleOperationResult.Ok($"Циклическое выполнение задачи {commandName} для успешно отменено.");
        }

        public ScheduleOperationResult CancelScheduledDailyTask(string commandName, TimeSpan taskTime)
        {
            var timersToRemove = _timers.Where(t => t.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) && t.DailyTime == taskTime).ToList();
            if (!timersToRemove.Any())
            {
                return ScheduleOperationResult.Error($"Ежедневная задача {commandName} на {taskTime} не найдена.");
            }
            _timers.RemoveAll(timerInfo =>
            {
                if (timerInfo.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) && timerInfo.DailyTime == taskTime)
                {
                    timerInfo.Timer.Dispose();
                    Log.Information("Планировщик задач: Отмена ежедневной задачи {CommandName} на {TaskTime}", commandName, taskTime);
                    return true;
                }
                return false;
            });
            return ScheduleOperationResult.Ok($"Ежедневная задача {commandName} на {taskTime} успешно отменена.");
        }

        public ScheduleOperationResult CancelAllScheduledTasks(string commandName)
        {
            var timersToRemove = _timers.Where(timerInfo => timerInfo.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!timersToRemove.Any())
            {
                return ScheduleOperationResult.Error($"Команда {commandName} не найдена или для неё не запланированы задачи.");
            }

            foreach (var timerInfo in timersToRemove)
            {
                timerInfo.Timer.Dispose();
                _timers.Remove(timerInfo);
                Log.Information("Планировщик задач: Полная отмена задач {CommandName}", commandName);
            }

            return ScheduleOperationResult.Ok($"Полная отмена задач для команды: {commandName}");
        }

        public List<ScheduledTaskInfo> GetScheduledTasksForChat(string chatId)
        {
            var scheduledTasks = new List<ScheduledTaskInfo>();

            foreach (var timerInfo in _timers.Where(t => t.ChatId == chatId))
            {
                var scheduledTaskInfo = new ScheduledTaskInfo
                {
                    CommandName = timerInfo.Command.Name,
                    CommandType = timerInfo.DailyTime.HasValue ? "Ежедневная" : "Циклическая",
                    ExecutionTimeOrInterval = timerInfo.DailyTime.HasValue ? timerInfo.DailyTime.Value.ToString(@"hh\:mm\:ss") : timerInfo.Interval.ToString(@"hh\:mm\:ss")
                };

                scheduledTasks.Add(scheduledTaskInfo);
            }

            return scheduledTasks;
        }


        private void Callback(object state)
        {
            var timerInfo = (SchedulerTimerInfo)state;
            using (var scope = _serviceProvider.CreateScope())
            {
                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var message = new Message() { Chat = new Chat() { Id = long.Parse(timerInfo.ChatId) } };
                try
                {
                    timerInfo.Command.ExecuteAsync(botClient, message).Wait();
                    Log.Information("Планировщик задач: Команда {CommandName} выполнена для чата {ChatId}", timerInfo.Command.Name, timerInfo.ChatId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Планировщик задач: Ошибка при выполнении команды {CommandName} для чата {ChatId}", timerInfo.Command.Name, timerInfo.ChatId);
                }
            }
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var scheduledTasksData = await _permissionsDatabaseService.LoadScheduledTasksAsync();
            foreach (var taskData in scheduledTasksData)
            {
                var command = _commandFactory.CreateCommand(taskData.CommandName);
                if (command != null)
                {
                    if (taskData.DailyTime.HasValue)
                    {
                        ScheduleDailyTask(command, taskData.ChatId, taskData.DailyTime.Value);
                    }
                    else
                    {
                        ScheduleCommand(command, taskData.ChatId, taskData.Interval);
                    }
                }
                else
                {
                    Log.Warning($"Command {taskData.CommandName} not found.");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timers.ForEach(timerInfo => timerInfo.Timer.Dispose());
            _timers.Clear();
            Log.Information("Планировщик задач: Сервис остановлен");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timers.ForEach(timerInfo => timerInfo.Timer.Dispose());
            Log.Information("Планировщик задач: Ресурсы освобождены");
        }
    }
}
