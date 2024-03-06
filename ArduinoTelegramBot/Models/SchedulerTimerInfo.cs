using ArduinoTelegramBot.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Models
{
    public class SchedulerTimerInfo
    {
        public string TaskId { get; set; } = Guid.NewGuid().ToString();
        public Timer Timer { get; set; }
        public string ChatId { get; set; }
        public IAuthorizedCommand Command { get; set; }
        public TimeSpan? DailyTime { get; set; }
        public TimeSpan Interval { get; set; }
    }
}
