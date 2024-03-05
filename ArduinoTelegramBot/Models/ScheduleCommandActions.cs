using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Models
{
    public static class ScheduleCommandActions
    {
        public const string Interval = "interval";
        public const string Daily = "daily";
        public const string DeleteInterval = "delete_interval";
        public const string DeleteDaily = "delete_daily";
        public const string Delete = "delete";
        public const string Help = "help";
    }
}
