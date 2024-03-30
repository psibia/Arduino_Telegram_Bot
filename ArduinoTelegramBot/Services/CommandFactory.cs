using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

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
            return _serviceProvider.GetServices<IAuthorizedCommand>().FirstOrDefault(cmd => cmd.Name == commandName);
        }
    }
}
