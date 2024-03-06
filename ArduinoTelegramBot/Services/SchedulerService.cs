using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types;
using Telegram.Bot;
using Serilog;
using ArduinoTelegramBot.Models.Sheduler;

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
            _commandFactory = commandFactory;
            Log.Information("Планировщик задач: Сервис инициализирован");
        }

        public async Task<ScheduleOperationResult> ScheduleCycleTaskAsync(IAuthorizedCommand command, long chatId, TimeSpan interval, bool isStartupLoad = false, string taskId = null)
        {
            if (_timers.Any(t => t.Command.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase) && t.ChatId == chatId))
            {
                return ScheduleOperationResult.Error($"Для команды {command.Name} уже запланировано выполнение задачи. Создание циклической задачи для этой команды невозможно.");
            }

            taskId = taskId ?? Guid.NewGuid().ToString();
            var schedulerTimerInfo = new SchedulerTimerInfo
            {
                TaskId = taskId,
                Command = command,
                ChatId = chatId,
                Interval = interval,
                DailyTime = null
            };

            var timer = new Timer(Callback, schedulerTimerInfo, interval, interval);
            schedulerTimerInfo.Timer = timer;
            _timers.Add(schedulerTimerInfo);

            var taskData = new ScheduledTaskData
            {
                TaskId = taskId,
                CommandName = command.Name,
                ChatId = chatId,
                Interval = interval,
                DailyTime = null
            };

            if (!isStartupLoad)
            {
                await _permissionsDatabaseService.SaveScheduledTaskAsync(taskData);
            }

            Log.Information("Планировщик задач: Команда {CommandName} запланирована для чата {ChatId} с интервалом {Interval}", command.Name, chatId, interval);
            return ScheduleOperationResult.Ok($"Запланировано циклическое выполнение команды {command.Name} с интервалом {interval}.");
        }

        public async Task<ScheduleOperationResult> ScheduleDailyTaskAsync(IAuthorizedCommand command, long chatId, TimeSpan dailyTime, bool isStartupLoad = false, string taskId = null)
        {
            if (_timers.Any(t => t.Command.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase) && t.ChatId == chatId && t.DailyTime == null))
            {
                Log.Information("Планировщик задач: Попытка создать ежедневную задачу для чата {id}. Для команды {name} уже запланировано циклическое выполнение задачи.", chatId, command.Name);
                return ScheduleOperationResult.Error($"Для команды {command.Name} уже запланировано циклическое выполнение задачи. Невозможно создать ежедневную задачу.");
            }

            if (_timers.Any(t => t.Command.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase) && t.ChatId == chatId && t.DailyTime == dailyTime))
            {
                Log.Information("Планировщик задач: Ежедневная задача {name} уже запланирована на {dailyTime}.", chatId, command.Name, dailyTime);
                return ScheduleOperationResult.Error($"Ежедневная задача {command.Name} уже запланирована на {dailyTime}.");
            }

            taskId = taskId ?? Guid.NewGuid().ToString();
            var now = DateTime.Now;
            var firstRunTime = now.TimeOfDay > dailyTime ? now.AddDays(1).Date.Add(dailyTime) : now.Date.Add(dailyTime);
            var initialDelay = firstRunTime - now;

            var schedulerTimerInfo = new SchedulerTimerInfo
            {
                TaskId = taskId,
                Command = command,
                ChatId = chatId,
                DailyTime = dailyTime,
                Interval = TimeSpan.FromDays(1)
            };

            var timer = new Timer(Callback, schedulerTimerInfo, initialDelay, TimeSpan.FromDays(1));
            schedulerTimerInfo.Timer = timer;
            _timers.Add(schedulerTimerInfo);

            var taskData = new ScheduledTaskData
            {
                TaskId = taskId,
                CommandName = command.Name,
                ChatId = chatId,
                DailyTime = dailyTime,
                Interval = TimeSpan.FromDays(1)
            };

            if (!isStartupLoad)
            {
                await _permissionsDatabaseService.SaveScheduledTaskAsync(taskData);
            }

            Log.Information("Планировщик задач: Ежедневная задача {CommandName} запланирована для чата {ChatId} на {DailyTime}", command.Name, chatId, dailyTime);
            return ScheduleOperationResult.Ok($"Ежедневная задача {command.Name} успешно запланирована на {dailyTime}.");
        }

        public async Task<ScheduleOperationResult> CancelScheduledCycleTaskAsync(string commandName, long chatId)
        {
            var taskIdsToRemove = _timers.Where(t => t.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) && t.ChatId == chatId && t.DailyTime == null).Select(t => t.TaskId).ToList();

            if (!taskIdsToRemove.Any())
            {
                return ScheduleOperationResult.Error($"Задача на циклическое выполнение команды {commandName} не найдена.");
            }

            foreach (var taskId in taskIdsToRemove)
            {
                var timerInfo = _timers.FirstOrDefault(t => t.TaskId == taskId);
                if (timerInfo != null)
                {
                    timerInfo.Timer.Dispose();
                    _timers.Remove(timerInfo);
                    await _permissionsDatabaseService.DeleteScheduledTaskAsync(taskId);
                    Log.Information("Планировщик задач: Отмена циклической задачи {CommandName} для chatId {ChatId}", commandName, chatId);
                }
            }

            return ScheduleOperationResult.Ok($"Циклическое выполнение задачи {commandName} успешно отменено.");
        }

        public async Task<ScheduleOperationResult> CancelScheduledDailyTaskAsync(string commandName, long chatId, TimeSpan taskTime)
        {
            var tasksToRemove = _timers.Where(t => t.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) && t.ChatId == chatId && t.DailyTime == taskTime).ToList();

            if (!tasksToRemove.Any())
            {
                return ScheduleOperationResult.Error($"Ежедневная задача {commandName} на {taskTime} не найдена.");
            }

            foreach (var timerInfo in tasksToRemove)
            {
                timerInfo.Timer.Dispose();
                _timers.Remove(timerInfo);
                await _permissionsDatabaseService.DeleteScheduledTaskAsync(timerInfo.TaskId);
                Log.Information("Планировщик задач: Отмена ежедневной задачи {CommandName} для chatId {ChatId} на {TaskTime}", commandName, chatId, taskTime);
            }

            return ScheduleOperationResult.Ok($"Ежедневная задача {commandName} на {taskTime} успешно отменена.");
        }


        public async Task<ScheduleOperationResult> CancelAllScheduledTasksAsync(string commandName, long chatId)
        {
            var taskIdsToRemove = _timers.Where(timerInfo => timerInfo.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) && timerInfo.ChatId == chatId).Select(t => t.TaskId).ToList();

            if (!taskIdsToRemove.Any())
            {
                return ScheduleOperationResult.Error($"Команда {commandName} не найдена или для неё не запланированы задачи.");
            }

            foreach (var taskId in taskIdsToRemove)
            {
                var timerInfo = _timers.FirstOrDefault(t => t.TaskId == taskId);
                if (timerInfo != null)
                {
                    timerInfo.Timer.Dispose();
                    _timers.Remove(timerInfo);
                    await _permissionsDatabaseService.DeleteScheduledTaskAsync(taskId);
                    Log.Information("Планировщик задач: Полная отмена задач {CommandName} для chatId {ChatId}", commandName, chatId);
                }
            }

            return ScheduleOperationResult.Ok($"Выполнена полная отмена задач для команды: {commandName}.");
        }

        public async Task<List<ScheduledTaskInfo>> GetScheduledTasksForChatAsync(long chatId)
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

            return await Task.FromResult(scheduledTasks);
        }


        private void Callback(object state)
        {
            var timerInfo = (SchedulerTimerInfo)state;
            using (var scope = _serviceProvider.CreateScope())
            {
                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var message = new Message() { Chat = new Chat() { Id = timerInfo.ChatId } };
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
                        await ScheduleDailyTaskAsync(command, taskData.ChatId, taskData.DailyTime.Value, isStartupLoad: true, taskId: taskData.TaskId);
                    }
                    else
                    {
                        await ScheduleCycleTaskAsync(command, taskData.ChatId, taskData.Interval, isStartupLoad: true, taskId: taskData.TaskId);
                    }
                }
                else
                {
                    Log.Warning($"Команда {taskData.CommandName} не найдена.");
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
