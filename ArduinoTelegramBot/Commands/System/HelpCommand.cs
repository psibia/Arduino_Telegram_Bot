using Telegram.Bot.Types;
using Telegram.Bot;
using ArduinoTelegramBot.Commands.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ArduinoTelegramBot.Commands.System
{
    public class HelpCommand : ICommand
    {
        public string Name { get; set; } = "/упс";
        IServiceProvider _serviceProvider;

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var commandNames = new List<string>();

            //получаем все ICommand
            var commands = _serviceProvider.GetServices<ICommand>();
            foreach (var command in commands)
            {
                commandNames.Add(command.Name);
            }

            //Получаем все IAuthorizedCommand
            var authorizedCommands = _serviceProvider.GetServices<IAuthorizedCommand>();
            foreach (var command in authorizedCommands)
            {
                //проверяем, чтобы не добавлять дубликаты
                if (!commandNames.Contains(command.Name))
                {
                    commandNames.Add(command.Name);
                }
            }

            string commandsNameConcat = "Cписок доступных команд:\n";
            foreach (var name in commandNames)
            {
                commandsNameConcat += name + "\n";
            }
            await botClient.SendTextMessageAsync(message.Chat.Id, commandsNameConcat);
        }

        public static HelpCommand Create(IServiceProvider serviceProvider, string name)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            return new HelpCommand()
            {
                Name = name,
                _serviceProvider = serviceProvider
            };
        }
    }
}
