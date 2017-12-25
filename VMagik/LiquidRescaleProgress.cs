using System.Drawing;

namespace VMagik
{
    internal struct LiquidRescaleProgress
    {
        public Bitmap Image {
            get;
        }

        public int CurrentFrame {
            get;
        }

        public int TotalFrames {
            get;
        }

        public LiquidRescaleProgress(Bitmap image, int currentFrame, int totalFrames)
        {
            Image = image;
            CurrentFrame = currentFrame;
            TotalFrames = totalFrames;
        }
    }
}