using ArduinoTelegramBot.Models;

namespace ArduinoTelegramBot.Services.Interfaces;

public interface IUserAuthorizationService
{
    Task<bool> IsAuthorized(long userId, string commandName);
    Task AuthorizeUser(long userId, string key);
    bool IsUserAuthorized(long userId);
    Task<AuthorizationResult> ProcessAuthorizationAttempt(string key, long userId); // Изменено
}
