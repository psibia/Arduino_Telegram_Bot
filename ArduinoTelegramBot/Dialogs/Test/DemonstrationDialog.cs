using ArduinoTelegramBot.Dialogs.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace ArduinoTelegramBot.Dialogs.Test;

public class DemonstrationDialog : IDialog
{
    public ITelegramBotClient BotClient { get; private set; }
    private IDialogState _currentState;
    public string Name { get; set; }
    public bool IsComplete { get; set; } = false;
    public Dictionary<string, string> UserData { get; private set; } = new Dictionary<string, string>();

    public DemonstrationDialog(ITelegramBotClient botClient, string name)
    {
        BotClient = botClient;
        Name = name;
        SetState(new InitialBranchQuestionState());
    }

    public async Task StartAsync(ITelegramBotClient botClient, Message message)
    {
        await _currentState.ProcessAsync(this, message, botClient);
    }

    public async Task<bool> NextAsync(ITelegramBotClient botClient, Message message)
    {
        if (!IsComplete)
        {
            var response = message.Text ?? message.Text; 
            var nextState = _currentState.DetermineNextState(response, this);
            _currentState = nextState;
            await _currentState.ProcessAsync(this, message, botClient);
        }
        return !IsComplete;
    }

    public void SetState(IDialogState state)
    {
        _currentState = state;
    }

    public void AddUserData(string key, string value)
    {
        UserData[key] = value;
    }

    public static DemonstrationDialog Create(IServiceProvider serviceProvider, string name)
    {
        return new DemonstrationDialog(serviceProvider.GetRequiredService<ITelegramBotClient>(), name);
    }

    public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        if (!IsComplete)
        {
            var nextState = _currentState.DetermineNextState(callbackQuery.Data, this);
            _currentState = nextState;
            var fakeMessage = new Message
            {
                Chat = new Chat { Id = callbackQuery.Message.Chat.Id },
                Text = callbackQuery.Data,
                From = callbackQuery.From
            };
            await _currentState.ProcessAsync(this, fakeMessage, BotClient);
            //удаление клавиатуры после ответа
            await BotClient.EditMessageReplyMarkupAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[] { }));
        }
    }
}






public abstract class DialogStateBase : IDialogState
{
    public abstract Task ProcessAsync(IDialog dialog, Message message, ITelegramBotClient botClient);
    public abstract IDialogState DetermineNextState(string response, IDialog dialog);

    protected async Task SendMessageAsync(ITelegramBotClient botClient, long chatId, string message, IReplyMarkup replyMarkup = null)
    {
        await botClient.SendTextMessageAsync(chatId, message, replyMarkup: replyMarkup);
    }
}

public class InitialBranchQuestionState : DialogStateBase
{
    public override async Task ProcessAsync(IDialog dialog, Message message, ITelegramBotClient botClient)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Ветвь 1", "1"),
            InlineKeyboardButton.WithCallbackData("Ветвь 2", "2"),
            InlineKeyboardButton.WithCallbackData("Ветвь 3", "3"),
            InlineKeyboardButton.WithCallbackData("Завершить диалог", "end_dialog")
        });

        await SendMessageAsync(botClient, message.Chat.Id, "Выберите ветвь: 1, 2 или 3?", inlineKeyboard);
    }

    public override IDialogState DetermineNextState(string response, IDialog dialog)
    {
        switch (response)
        {
            case "1":
                return new BranchAQuestion1State();
            case "2":
                return new BranchBQuestion1State();
            case "3":
                return new BranchCQuestion1State();
            case "end_dialog":
                return new CompletionState();
            default:
                return new InvalidInputState(this, "Некорректный формат ответа. Пожалуйста, выберите 1, 2 или 3.");
        }
    }
}

public class InvalidInputState : DialogStateBase
{
    private readonly IDialogState _returnToState;
    private readonly string _message;

    public InvalidInputState(IDialogState returnToState, string message)
    {
        _returnToState = returnToState;
        _message = message;
    }

    public override async Task ProcessAsync(IDialog dialog, Message message, ITelegramBotClient botClient)
    {
        await SendMessageAsync(botClient, message.Chat.Id, _message); //в этом случае клавиатура не нужна
    }

    public override IDialogState DetermineNextState(string response, IDialog dialog)
    {
        return response == "end_dialog" ? new CompletionState() : _returnToState;
    }
}

public class BranchAQuestion1State : DialogStateBase
{
    public override async Task ProcessAsync(IDialog dialog, Message message, ITelegramBotClient botClient)
    {
        if (dialog is DemonstrationDialog userDialog)
        {
            await SendMessageAsync(botClient, message.Chat.Id, "Ветвь A: Какой ваш любимый цвет?");
        }
    }

    public override IDialogState DetermineNextState(string response, IDialog dialog)
    {
        if (dialog is DemonstrationDialog userDialog)
        {
            userDialog.AddUserData("Любимый цвет", response);
            return new CompletionState();
        }
        return null;
    }
}

public class BranchBQuestion1State : DialogStateBase
{
    public override async Task ProcessAsync(IDialog dialog, Message message, ITelegramBotClient botClient)
    {
        if (dialog is DemonstrationDialog userDialog)
        {
            await SendMessageAsync(botClient, message.Chat.Id, "Ветвь B: Какая ваша любимая книга?");
        }
    }

    public override IDialogState DetermineNextState(string response, IDialog dialog)
    {
        if (dialog is DemonstrationDialog userDialog)
        {
            userDialog.AddUserData("Любимая книга", response);
            return new CompletionState();
        }
        return null;
    }
}

public class BranchCQuestion1State : DialogStateBase
{
    public override async Task ProcessAsync(IDialog dialog, Message message, ITelegramBotClient botClient)
    {
        if (dialog is DemonstrationDialog userDialog)
        {
            await SendMessageAsync(botClient, message.Chat.Id, "Ветвь C: Какой ваш любимый фильм?");
        }
    }

    public override IDialogState DetermineNextState(string response, IDialog dialog)
    {
        if (dialog is DemonstrationDialog userDialog)
        {
            userDialog.AddUserData("Любимый фильм", response);
            return new CompletionState();
        }
        return null;
    }
}

public class CompletionState : DialogStateBase
{
    public override async Task ProcessAsync(IDialog dialog, Message message, ITelegramBotClient botClient)
    {
        if (dialog is DemonstrationDialog userDialog)
        {
            string userDataSummary = userDialog.UserData.Any()
                ? userDialog.UserData.Select(kvp => $"{kvp.Key}: {kvp.Value}").Aggregate((a, b) => a + "\n" + b)
                : "Похоже, вы не предоставили никаких данных.";
            await SendMessageAsync(botClient, message.Chat.Id, $"Ваш опрос завершен! Ваши ответы:\n{userDataSummary}");
            userDialog.IsComplete = true;
        }
    }

    public override IDialogState DetermineNextState(string response, IDialog dialog)
    {
        return this;
    }
}

