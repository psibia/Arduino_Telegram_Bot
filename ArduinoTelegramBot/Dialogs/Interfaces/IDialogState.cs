using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using ArduinoTelegramBot.Dialogs.Test;

namespace ArduinoTelegramBot.Dialogs.Interfaces
{
    public interface IDialogState
    {
        Task ProcessAsync(IDialog dialog, Message message, ITelegramBotClient botClient);
        IDialogState DetermineNextState(string response, IDialog dialog);
    }
}
