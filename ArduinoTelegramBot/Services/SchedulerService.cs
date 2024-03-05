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
        private List<TimerInfo> _timers = new List<TimerInfo>();

        public SchedulerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Log.Information("Планировщик задач: Сервис инициализирован.");
        }

        public void ScheduleCommand(IAuthorizedCommand command, string chatId, TimeSpan interval)
        {
            var timer = new Timer(Callback, new TimerInfo { Command = command, ChatId = chatId }, interval, interval);
            _timers.Add(new TimerInfo { Timer = timer, ChatId = chatId, Command = command });
            Log.Information("Планировщик задач: Команда {CommandName} запланирована для чата {ChatId} с интервалом {Interval}", command.Name, chatId, interval);
        }

        public void ScheduleDailyTask(IAuthorizedCommand command, string chatId, TimeSpan dailyTime)
        {
            var now = DateTime.Now;
            var firstRunTime = now.TimeOfDay > dailyTime ? now.Date.AddDays(1).Add(dailyTime) : now.Date.Add(dailyTime);
            var initialDelay = firstRunTime - now;
            var timer = new Timer(Callback, new TimerInfo { Command = command, ChatId = chatId, DailyTime = dailyTime }, initialDelay, TimeSpan.FromDays(1));
            _timers.Add(new TimerInfo { Timer = timer, ChatId = chatId, Command = command, DailyTime = dailyTime });
            Log.Information("Планировщик задач: Ежедневная задача {CommandName} запланирована для чата {ChatId} на {DailyTime}", command.Name, chatId, dailyTime);
        }

        public void CancelScheduledCommand(string commandName)
        {
            _timers.RemoveAll(timerInfo =>
            {
                if (timerInfo.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase))
                {
                    timerInfo.Timer.Dispose();
                    Log.Information("Планировщик задач: Отмена задачи {CommandName}", commandName);
                    return true;
                }
                return false;
            });
        }

        public void CancelScheduledDailyTask(string commandName, TimeSpan taskTime)
        {
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
        }

        public void CancelAllScheduledTasks(string commandName)
        {
            _timers.RemoveAll(timerInfo =>
            {
                if (timerInfo.Command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase))
                {
                    timerInfo.Timer.Dispose();
                    Log.Information("Планировщик задач: Полная отмена задач {CommandName}", commandName);
                    return true;
                }
                return false;
            });
        }

        private void Callback(object state)
        {
            var timerInfo = (TimerInfo)state;
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Information("Планировщик задач: Сервис запущен");
            return Task.CompletedTask;
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
