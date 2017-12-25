using System;
using System.Drawing;

namespace VMagik
{
    internal struct LiquidRescaleProgress : IEquatable<LiquidRescaleProgress>
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

        public bool Equals(LiquidRescaleProgress other)
        {
            return Equals(Image, other.Image) && CurrentFrame == other.CurrentFrame && TotalFrames == other.TotalFrames;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is LiquidRescaleProgress progress && Equals(progress);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Image != null ? Image.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CurrentFrame;
                hashCode = (hashCode * 397) ^ TotalFrames;
                return hashCode;
            }
        }
    }
}