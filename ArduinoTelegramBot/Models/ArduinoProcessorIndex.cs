using ArduinoTelegramBot.Processors.Arduino.Interfaces;
using System.Text.RegularExpressions;

namespace ArduinoTelegramBot.Models
{
    public class ArduinoProcessorIndex
    {
        public Regex Pattern { get; }
        public ISerialDataProcessor Processor { get; }

        public ArduinoProcessorIndex(Regex pattern, ISerialDataProcessor processor)
        {
            Pattern = pattern;
            Processor = processor;
        }
    }
}
