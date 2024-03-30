using ArduinoTelegramBot.Models.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Repositories.Authorization.Interfaces
{
    public interface IAccessControlService
    {
        Task<AccessKey> GetPermissionsAsync(string key);
        Task<Dictionary<long, string>> LoadUserKeysAsync();
        Task SaveUserKeysAsync(Dictionary<long, string> userKeys);
        Task CreateAccessKeyAsync(AccessKey accessKey);
        Task DeleteAccessKeyAsync(string key);
        Task BlockAccessKeyAsync(string key);
        Task UnblockAccessKeyAsync(string key);
    }
}
