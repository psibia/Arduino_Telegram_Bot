using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types;
using Telegram.Bot;
using Serilog;

namespace ArduinoTelegramBot.Services
{
    public class SchedulerService : ISchedulerService, IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private List<(Timer Timer, string ChatId, IAuthorizedCommand Command)> _timers = new();

        public SchedulerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Log.Information("Планировщик задач: Сервис инициализирован.");
        }

        public void ScheduleCommand(IAuthorizedCommand command, string chatId, TimeSpan interval)
        {
            var timer = new Timer(Callback, (command, chatId), interval, interval);
            _timers.Add((timer, chatId, command));
            Log.Information("Планировщик задач: Команда запланирована для чата {chatId} с интервалом {interval}", chatId, interval);
        }

        private void Callback(object state)
        {
            var (command, chatId) = ((IAuthorizedCommand, string))state;
            using (var scope = _serviceProvider.CreateScope())
            {
                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var message = new Message() { Chat = new Chat() { Id = long.Parse(chatId) } };
                try
                {
                    command.ExecuteAsync(botClient, message).Wait();
                    Log.Information("Планировщик задач: Команда выполнена для чата {chatId}", chatId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Планировщик задач: Ошибка при выполнении команды для чата {chatId}", chatId);
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
            foreach (var (Timer, _, _) in _timers)
            {
                Timer?.Dispose();
            }
            _timers.Clear();
            Log.Information("Планировщик задач: Сервис остановлен");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var (Timer, _, _) in _timers)
            {
                Timer?.Dispose();
            }
            Log.Information("Планировщик задач: Ресурсы освобождены");
        }
    }
}
