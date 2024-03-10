using ArduinoTelegramBot.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ArduinoTelegramBot.Services
{
    public class Test1Command : IAuthorizedCommand
    {
        public string Name { get; set; }

        public Test1Command(ITelegramBotClient telegramBotClient, Message message)
        {
            
        }

        public Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            throw new NotImplementedException();
        }
    }
}
