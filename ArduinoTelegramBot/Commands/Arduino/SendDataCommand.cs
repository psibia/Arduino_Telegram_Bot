using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.DependencyInjection;

namespace ArduinoTelegramBot.Commands.Arduino
{
    public class SendDataCommand : IAuthorizedCommand
    {
        public string Name { get; set; }
        private readonly ITelegramBotClient _botClient;
        private readonly ISerialPortService _serialPortService;

        public SendDataCommand(ITelegramBotClient botClient, ISerialPortService serialPortService)
        {
            _botClient = botClient;
            _serialPortService = serialPortService;
        }

        public static SendDataCommand Create(IServiceProvider serviceProvider, string name)
        {
            return new SendDataCommand(
                serviceProvider.GetRequiredService<ITelegramBotClient>(),
                serviceProvider.GetRequiredService<ISerialPortService>())
            {
                Name = name
            };
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var dataToSend = message.Text.Substring(Name.Length).Trim();
            long chatId = message.Chat.Id;

            if (string.IsNullOrEmpty(dataToSend))
            {
                await botClient.SendTextMessageAsync(chatId, "Пожалуйста, укажите данные для отправки после команды.");
                return;
            }

            var result = await _serialPortService.SendDataAsync(dataToSend, chatId);
            if (result.Success)
            {
                //await botClient.SendTextMessageAsync(chatId, "Запрос отправлен."); //удалить, это для тестирования
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, result.Message);
            }
        }
    }
}
