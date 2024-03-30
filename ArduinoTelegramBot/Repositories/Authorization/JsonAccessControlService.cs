using ArduinoTelegramBot.Models.Authorization;
using ArduinoTelegramBot.Repositories.Authorization.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Repositories.Authorization
{
    public class JsonAccessControlService : IAccessControlService
    {
        private readonly string _directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "JsonDb");
        private readonly string _accessKeysFilePath;
        private readonly string _userKeysFilePath;

        public JsonAccessControlService()
        {
            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }

            _accessKeysFilePath = Path.Combine(_directoryPath, "accessKeys.json");
            _userKeysFilePath = Path.Combine(_directoryPath, "userKeys.json");

            InitializeAccessKeysFile();
            InitializeUserKeysFile();
        }

        private void InitializeAccessKeysFile()
        {
            if (!File.Exists(_accessKeysFilePath))
            {
                var initialAccessKeys = new List<AccessKey>
            {
                new AccessKey
                {
                    Key = "admin",
                    AvailableCommandsJson = JsonConvert.SerializeObject(new List<string>()),
                    IsActive = true,
                    IsMasterKey = true
                }
            };
                File.WriteAllText(_accessKeysFilePath, JsonConvert.SerializeObject(initialAccessKeys, Formatting.Indented));
            }
        }

        private void InitializeUserKeysFile()
        {
            if (!File.Exists(_userKeysFilePath))
            {
                var initialUserKeys = new List<UserKey>();
                File.WriteAllText(_userKeysFilePath, JsonConvert.SerializeObject(initialUserKeys, Formatting.Indented));
            }
        }

        public async Task<AccessKey> GetPermissionsAsync(string key)
        {
            var accessKeys = JsonConvert.DeserializeObject<List<AccessKey>>(await File.ReadAllTextAsync(_accessKeysFilePath)) ?? new List<AccessKey>();
            var accessKey = accessKeys.FirstOrDefault(ak => ak.Key == key && ak.IsActive);
            if (accessKey == null) throw new KeyNotFoundException($"Ключ '{key}' не найден или был заблокирован.");
            return accessKey;
        }

        public async Task<Dictionary<long, string>> LoadUserKeysAsync()
        {
            var userKeys = JsonConvert.DeserializeObject<List<UserKey>>(await File.ReadAllTextAsync(_userKeysFilePath)) ?? new List<UserKey>();
            return userKeys.ToDictionary(uk => uk.UserId, uk => uk.Key);
        }

        public async Task SaveUserKeysAsync(Dictionary<long, string> userKeys)
        {
            var userKeysList = userKeys.Select(uk => new UserKey { UserId = uk.Key, Key = uk.Value }).ToList();
            await File.WriteAllTextAsync(_userKeysFilePath, JsonConvert.SerializeObject(userKeysList, Formatting.Indented));
        }

        public async Task CreateAccessKeyAsync(AccessKey accessKey)
        {
            var accessKeys = JsonConvert.DeserializeObject<List<AccessKey>>(await File.ReadAllTextAsync(_accessKeysFilePath)) ?? new List<AccessKey>();
            accessKeys.Add(accessKey);
            await File.WriteAllTextAsync(_accessKeysFilePath, JsonConvert.SerializeObject(accessKeys, Formatting.Indented));
        }

        public async Task DeleteAccessKeyAsync(string key)
        {
            var accessKeys = JsonConvert.DeserializeObject<List<AccessKey>>(await File.ReadAllTextAsync(_accessKeysFilePath)) ?? new List<AccessKey>();
            var accessKey = accessKeys.FirstOrDefault(ak => ak.Key == key);
            if (accessKey == null) throw new KeyNotFoundException($"Ключ '{key}' не найден.");

            accessKeys.Remove(accessKey);
            await File.WriteAllTextAsync(_accessKeysFilePath, JsonConvert.SerializeObject(accessKeys, Formatting.Indented));

            // Удаление всех UserKeys, связанных с удаленным AccessKey
            var userKeys = JsonConvert.DeserializeObject<List<UserKey>>(await File.ReadAllTextAsync(_userKeysFilePath)) ?? new List<UserKey>();
            userKeys = userKeys.Where(uk => uk.Key != key).ToList();
            await File.WriteAllTextAsync(_userKeysFilePath, JsonConvert.SerializeObject(userKeys, Formatting.Indented));
        }

        public async Task BlockAccessKeyAsync(string key)
        {
            var accessKeys = JsonConvert.DeserializeObject<List<AccessKey>>(await File.ReadAllTextAsync(_accessKeysFilePath)) ?? new List<AccessKey>();
            var accessKey = accessKeys.FirstOrDefault(ak => ak.Key == key);
            if (accessKey == null) throw new KeyNotFoundException($"Ключ '{key}' не найден.");

            accessKey.IsActive = false;
            await File.WriteAllTextAsync(_accessKeysFilePath, JsonConvert.SerializeObject(accessKeys, Formatting.Indented));
        }

        public async Task UnblockAccessKeyAsync(string key)
        {
            var accessKeys = JsonConvert.DeserializeObject<List<AccessKey>>(await File.ReadAllTextAsync(_accessKeysFilePath)) ?? new List<AccessKey>();
            var accessKey = accessKeys.FirstOrDefault(ak => ak.Key == key);
            if (accessKey == null) throw new KeyNotFoundException($"Ключ '{key}' не найден.");

            accessKey.IsActive = true;
            await File.WriteAllTextAsync(_accessKeysFilePath, JsonConvert.SerializeObject(accessKeys, Formatting.Indented));
        }
    }
}
