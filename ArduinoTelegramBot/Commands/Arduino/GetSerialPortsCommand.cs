using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.DependencyInjection;

namespace ArduinoTelegramBot.Commands.Arduino
{
    public class GetSerialPortsCommand : IAuthorizedCommand
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ISerialPortService _serialPortService;

        public string Name { get; set; }

        public GetSerialPortsCommand(ITelegramBotClient botClient, ISerialPortService serialPortService)
        {
            _botClient = botClient;
            _serialPortService = serialPortService;
        }

        public static GetSerialPortsCommand Create(IServiceProvider serviceProvider, string name)
        {
            var command = new GetSerialPortsCommand(
                serviceProvider.GetRequiredService<ITelegramBotClient>(),
                serviceProvider.GetRequiredService<ISerialPortService>())
            {
                Name = name
            };
            return command;
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var result = await _serialPortService.ListAvailablePortsAsync();
            if (result.Success)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Доступные порты: {string.Join(", ", result.Data)}");
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
            }
        }
    }
}
