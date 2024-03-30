using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace ArduinoTelegramBot.Dialogs.Interfaces
{
    public interface IDialog
    {
        string Name { get; set; }
        Task StartAsync(ITelegramBotClient botClient, Message message);
        Task<bool> NextAsync(ITelegramBotClient botClient, Message message);
        bool IsComplete { get; }
        Task HandleCallbackQueryAsync(CallbackQuery callbackQuery);
    }
}
