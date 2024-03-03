using ArduinoTelegramBot.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ArduinoTelegramBot.Commands.Arduino
{
    public class CloseSerialPortCommand : IAuthorizedCommand
    {
        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            throw new NotImplementedException();
        }

        public CloseSerialPortCommand Create() 
        {
            return new CloseSerialPortCommand()
            {
                Name = Name
            };
        }
    }
}
