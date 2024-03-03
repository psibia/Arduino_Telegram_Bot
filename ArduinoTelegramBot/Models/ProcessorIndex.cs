using ArduinoTelegramBot.Processors.Arduino.Interfaces;
using System.Text.RegularExpressions;

namespace ArduinoTelegramBot.Models
{
    public class ProcessorIndex
    {
        public Regex Pattern { get; }
        public ISerialDataProcessor Processor { get; }

        public ProcessorIndex(Regex pattern, ISerialDataProcessor processor)
        {
            Pattern = pattern;
            Processor = processor;
        }
    }
}
