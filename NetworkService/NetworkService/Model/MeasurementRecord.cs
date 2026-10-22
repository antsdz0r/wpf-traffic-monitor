using System;

namespace NetworkService.Model
{
    
    public class MeasurementRecord
    {
        public double Value { get; }
        public DateTime Time { get; }
        public bool IsValid { get; }

        public MeasurementRecord(double value, DateTime time, bool isValid)
        {
            Value = value;
            Time = time;
            IsValid = isValid;
        }

        public string TimeLabel => Time.ToString("HH:mm:ss");
    }
}
