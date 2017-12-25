using System.Threading;

namespace VMagik
{
    internal struct LiquidRescaleInfo
    {
        public string InputPath {
            get;
        }

        public string OutputPath {
            get;
        }

        public double Amount {
            get;
        }

        public ManualResetEvent ResetEvent { get; }

        public LiquidRescaleInfo(string inputPath, string outputPath, double amount, ManualResetEvent resetEvent)
        {
            InputPath = inputPath;
            OutputPath = outputPath;
            Amount = amount;
            ResetEvent = resetEvent;
        }
    }
}
