using ArduinoTelegramBot.Models;

namespace ArduinoTelegramBot.Services.Interfaces;

public interface IUserAuthorizationService
{
    Task<bool> CheckUserAuthorization(long userId, string commandName);
    Task<AuthorizationResult> AttemptAuthorizationWithKey(string key, long userId);
}
