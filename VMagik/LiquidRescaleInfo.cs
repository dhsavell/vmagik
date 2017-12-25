using System;
using System.Threading;

namespace VMagik
{
    internal struct LiquidRescaleInfo : IEquatable<LiquidRescaleInfo>
    {
        public string InputPath { get; }

        public string OutputPath { get; }

        public double Amount { get; }

        public ManualResetEvent ResetEvent { get; }

        public LiquidRescaleInfo(string inputPath, string outputPath, double amount, ManualResetEvent resetEvent)
        {
            InputPath = inputPath;
            OutputPath = outputPath;
            Amount = amount;
            ResetEvent = resetEvent;
        }

        public bool Equals(LiquidRescaleInfo other)
        {
            return string.Equals(InputPath, other.InputPath) && string.Equals(OutputPath, other.OutputPath) &&
                   Amount.Equals(other.Amount) && Equals(ResetEvent, other.ResetEvent);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is LiquidRescaleInfo info && Equals(info);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = InputPath != null ? InputPath.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (OutputPath != null ? OutputPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Amount.GetHashCode();
                hashCode = (hashCode * 397) ^ (ResetEvent != null ? ResetEvent.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}