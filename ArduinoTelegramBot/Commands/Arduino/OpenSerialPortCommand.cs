using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ArduinoTelegramBot.Commands.Arduino
{
    public class OpenSerialPortCommand : IAuthorizedCommand
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ISerialPortService _serialPortService;
        public string Name { get; set; }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var result = await _serialPortService.TryOpenPortAsync();
            await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
        }
        public OpenSerialPortCommand(ITelegramBotClient botClient, ISerialPortService serialPortService)
        {
            _botClient = botClient;
            _serialPortService = serialPortService;
        }
        public static OpenSerialPortCommand Create(IServiceProvider serviceProvider, string name)
        {
            return new OpenSerialPortCommand(
                serviceProvider.GetRequiredService<ITelegramBotClient>(),
                serviceProvider.GetRequiredService<ISerialPortService>())
            {
                Name = name
            };
        }
    }
}
