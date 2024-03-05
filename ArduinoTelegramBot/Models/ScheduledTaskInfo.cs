using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Models
{
    public class ScheduledTaskInfo
    {
        public string CommandType { get; set; } //Циклическая или Ежедневная
        public string CommandName { get; set; }
        public string ExecutionTimeOrInterval { get; set; }
    }
}
