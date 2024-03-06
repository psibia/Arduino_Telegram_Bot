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

        public ScheduleOperationResult ScheduleCommand(IAuthorizedCommand command, string chatId, TimeSpan interval, bool isStartupLoad = false, string taskId = null)
        {
            // Проверяем, не запланирована ли уже такая задача
            if (_timers.Any(t => t.Command.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase) && t.ChatId == chatId))
            {
                return ScheduleOperationResult.Error($"Циклическое выполнение команды {command.Name} уже запланировано для чата {chatId}.");
            }

            // Создаем информацию о таймере для циклической команды
            taskId = taskId ?? Guid.NewGuid().ToString();
            var schedulerTimerInfo = new SchedulerTimerInfo
            {
                TaskId = taskId,
                Command = command,
                ChatId = chatId,
                Interval = interval,
                DailyTime = null
            };

            // Создаем и запускаем таймер
            var timer = new Timer(Callback, schedulerTimerInfo, interval, interval);
            schedulerTimerInfo.Timer = timer;
            _timers.Add(schedulerTimerInfo);

            // Создаем данные о задаче для сохранения
            var taskData = new ScheduledTaskData
            {
                TaskId = taskId,
                CommandName = command.Name,
                ChatId = chatId,
                Interval = interval,
                DailyTime = null
            };

            // Сохраняем задачу, если это не загрузка при старте программы
            if (!isStartupLoad)
            {
                _permissionsDatabaseService.SaveScheduledTaskAsync(taskData).Wait();
            }

            Log.Information("Планировщик задач: Команда {CommandName} запланирована для чата {ChatId} с интервалом {Interval}", command.Name, chatId, interval);
            return ScheduleOperationResult.Ok($"Запланировано циклическое выполнение команды {command.Name} с интервалом {interval}.");
        }

        public ScheduleOperationResult ScheduleDailyTask(IAuthorizedCommand command, string chatId, TimeSpan dailyTime, bool isStartupLoad = false, string taskId = null)
        {
            if (_timers.Any(t => t.Command.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase) && t.ChatId == chatId && t.DailyTime == dailyTime))
            {
                return ScheduleOperationResult.Error($"Ежедневная задача {command.Name} уже запланирована для чата {chatId} на {dailyTime}.");
            }

            var now = DateTime.Now;
            var firstRunTime = now.TimeOfDay > dailyTime ? now.AddDays(1).Add(dailyTime) : now.Date.Add(dailyTime);
            var initialDelay = firstRunTime - now;

            taskId = taskId ?? Guid.NewGuid().ToString();
            var schedulerTimerInfo = new SchedulerTimerInfo
            {
                TaskId = taskId,
                Command = command,
                ChatId = chatId,
                DailyTime = dailyTime,
                Interval = TimeSpan.FromDays(1) // Установлен для обозначения повторения задачи каждые 24 часа
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
                Interval = TimeSpan.FromDays(1) //может быть использовано для уточнения, так как задача ежедневная
            };

            if (!isStartupLoad) // Чтобы задача не дублировалась при запуске программы, когда загружаются имеющиеся задачи
            {
                _permissionsDatabaseService.SaveScheduledTaskAsync(taskData).Wait();
            }

            Log.Information("Планировщик задач: Ежедневная задача {CommandName} запланирована для чата {ChatId} на {DailyTime}", command.Name, chatId, dailyTime);
            return ScheduleOperationResult.Ok($"Ежедневная задача {command.Name} успешно запланирована на {dailyTime}.");
        }

        public ScheduleOperationResult CancelScheduledCommand(string commandName, string chatId)
        {
            var taskIdsToRemove = _timers.Where(t => t.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) && t.ChatId == chatId)
                                         .Select(t => t.TaskId)
                                         .ToList();

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
                    Log.Information("Планировщик задач: Отмена циклической задачи {CommandName} для chatId {ChatId}", commandName, chatId);
                    _permissionsDatabaseService.DeleteScheduledTaskAsync(taskId).Wait();
                }
            }

            return ScheduleOperationResult.Ok($"Циклическое выполнение задачи {commandName} для chatId {chatId} успешно отменено.");
        }

        public ScheduleOperationResult CancelScheduledDailyTask(string commandName, string chatId, TimeSpan taskTime)
        {
            // Выборка задач для удаления учитывает как commandName, так и chatId для однозначной идентификации
            var tasksToRemove = _timers.Where(t => t.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase)
                                                    && t.ChatId == chatId
                                                    && t.DailyTime == taskTime).ToList();

            if (!tasksToRemove.Any())
            {
                return ScheduleOperationResult.Error($"Ежедневная задача {commandName} на {taskTime} не найдена для chatId {chatId}.");
            }

            foreach (var timerInfo in tasksToRemove)
            {
                timerInfo.Timer.Dispose();
                _timers.Remove(timerInfo);
                Log.Information("Планировщик задач: Отмена ежедневной задачи {CommandName} для chatId {ChatId} на {TaskTime}", commandName, chatId, taskTime);
                // Для удаления используется TaskId, достаточный для однозначного определения задачи
                _permissionsDatabaseService.DeleteScheduledTaskAsync(timerInfo.TaskId).Wait();
            }

            return ScheduleOperationResult.Ok($"Ежедневная задача {commandName} на {taskTime} для chatId {chatId} успешно отменена.");
        }

        public ScheduleOperationResult CancelAllScheduledTasks(string commandName, string chatId)
        {
            var taskIdsToRemove = _timers.Where(timerInfo => timerInfo.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) && timerInfo.ChatId == chatId)
                                         .Select(t => t.TaskId)
                                         .ToList();

            if (!taskIdsToRemove.Any())
            {
                return ScheduleOperationResult.Error($"Команда {commandName} не найдена или для неё не запланированы задачи в чате {chatId}.");
            }

            foreach (var taskId in taskIdsToRemove)
            {
                var timerInfo = _timers.FirstOrDefault(t => t.TaskId == taskId && t.ChatId == chatId);
                if (timerInfo != null)
                {
                    timerInfo.Timer.Dispose();
                    _timers.Remove(timerInfo);
                    Log.Information("Планировщик задач: Полная отмена задач {CommandName} для chatId {ChatId}", commandName, chatId);
                    _permissionsDatabaseService.DeleteScheduledTaskAsync(taskId).Wait();
                }
            }

            return ScheduleOperationResult.Ok($"Полная отмена задач для команды: {commandName} в чате {chatId}.");
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
                    // Проверяем, задано ли время выполнения для ежедневной задачи
                    if (taskData.DailyTime.HasValue)
                    {
                        // Передаем isStartupLoad и TaskId в ScheduleDailyTask
                        ScheduleDailyTask(command, taskData.ChatId, taskData.DailyTime.Value, true, taskData.TaskId);
                    }
                    else
                    {
                        // Передаем isStartupLoad и TaskId в ScheduleCommand для циклических задач
                        ScheduleCommand(command, taskData.ChatId, taskData.Interval, true, taskData.TaskId);
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
