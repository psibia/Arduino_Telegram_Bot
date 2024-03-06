using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Models.Sheduler
{
    public class SchedulerTask
    {
        public long ChatId { get; set; }
        public string CommandName { get; set; }
        public TimeSpan Interval { get; set; }
        public TimeSpan? DailyTime { get; set; }
    }
}
