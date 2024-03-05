using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Services
{
    public class CommandFactory : ICommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CommandFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IAuthorizedCommand CreateCommand(string commandName)
        {
            // Здесь можно использовать switch или любую другую логику для выбора команды.
            // Пример с использованием имени команды для получения из DI-контейнера:
            return _serviceProvider.GetServices<IAuthorizedCommand>().FirstOrDefault(cmd => cmd.Name == commandName);
        }
    }
}
