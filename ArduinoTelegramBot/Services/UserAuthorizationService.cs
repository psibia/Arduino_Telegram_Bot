using Serilog;
using ArduinoTelegramBot.Services.Interfaces;
using ArduinoTelegramBot.Models;

namespace ArduinoTelegramBot.Services;

public class UserAuthorizationService : IUserAuthorizationService
{
    private Dictionary<long, string> _userKeys = new Dictionary<long, string>();
    private readonly Dictionary<long, DateTime> _lastChecked = new Dictionary<long, DateTime>();
    private static readonly Dictionary<string, List<string>> _permissions = new(); //словарь для кэширования разрешений пользователя
    private const int CacheDurationInMinutes = 1; //длительность кэширования проверки ключа, в минутх
    private readonly IPermissionsDatabaseService _permissionsDatabase;

    public UserAuthorizationService(IPermissionsDatabaseService permissionsDatabase)
    {
        _permissionsDatabase = permissionsDatabase;
        Initialize();
    }

    private async void Initialize()
    {
        try
        {
            Log.Information("Сервис авторизации: Начинается загрузка данных авторизации пользователей...");
            _userKeys = await _permissionsDatabase.LoadUserKeysAsync();
            Log.Information("Сервис авторизации: Данные авторизации пользователей успешно загружены. Загружено {Count} записей.", _userKeys.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Сервис авторизации: Произошла ошибка при загрузке данных авторизации пользователей.");
        }
    }

    public async Task<bool> IsAuthorized(long userId, string commandName)
    {
        if (_userKeys.TryGetValue(userId, out var key))
        {
            if (!_lastChecked.ContainsKey(userId) || DateTime.UtcNow.Subtract(_lastChecked[userId]).TotalMinutes > CacheDurationInMinutes || !_permissions.ContainsKey(key))
            {
                try
                {
                    var permissions = await _permissionsDatabase.GetPermissionsAsync(key);
                    _lastChecked[userId] = DateTime.UtcNow; // Обновляем время последней успешной проверки.
                    _permissions[key] = permissions ?? new List<string>(); // Обновляем кэшированные разрешения для мастер-ключа.

                    return permissions == null || permissions.Contains(commandName, StringComparer.InvariantCultureIgnoreCase);
                }
                catch (KeyNotFoundException)
                {
                    return false;
                }
            }
            else
            {
                return _permissions[key] == null || _permissions[key].Contains(commandName, StringComparer.InvariantCultureIgnoreCase);
            }
        }

        return false;
    }



    public async Task AuthorizeUser(long userId, string key)
    {
        try
        {
            var permissions = await _permissionsDatabase.GetPermissionsAsync(key);
            _userKeys[userId] = key;
            _lastChecked[userId] = DateTime.UtcNow;

            _permissions[key] = permissions;
            //сохраняем обновленные данные авторизации
            await _permissionsDatabase.SaveUserKeysAsync(_userKeys);
        }
        catch (KeyNotFoundException)
        {
            //логируем на уровне выше, просто пробрасываем исключение, иначе задублируется
            throw;
        }
    }

    public bool IsUserAuthorized(long userId)
    {
        return _userKeys.ContainsKey(userId) && _lastChecked.ContainsKey(userId) &&
               DateTime.UtcNow.Subtract(_lastChecked[userId]).TotalMinutes <= CacheDurationInMinutes;
    }

    public async Task<AuthorizationResult> ProcessAuthorizationAttempt(string key, long userId)
    {
        try
        {
            await AuthorizeUser(userId, key);
            var message = "Вы успешно авторизованы.";
            Log.Information("Сервис авторизации: Пользователь {UserId} успешно авторизован с ключом {Key}", userId, key);
            return new AuthorizationResult { Success = true, Message = message };
        }
        catch (KeyNotFoundException)
        {
            var message = "Данный ключ не найден или неверен.";
            Log.Warning("Сервис авторизации: Пользователь {UserId} пытался авторизоваться с неверным ключом {Key}.", userId, key);
            return new AuthorizationResult { Success = false, Message = message };
        }
    }
}