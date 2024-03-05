using ArduinoTelegramBot.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface ICommandFactory
    {
        IAuthorizedCommand CreateCommand(string commandName);
    }
}
