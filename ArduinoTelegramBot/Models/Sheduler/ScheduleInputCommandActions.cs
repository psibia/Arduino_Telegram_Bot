using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Models.Sheduler
{
    public static class ScheduleInputCommandActions
    {
        public const string Interval = "cycle";
        public const string Daily = "daily";
        public const string Delete = "delete"; // Используем этот подход для всех операций удаления
        public const string DeleteAll = "delete_all";
        public const string Help = "help";
        public const string Get = "get";
    }
}
