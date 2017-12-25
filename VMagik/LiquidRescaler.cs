using Emgu.CV;
using ImageMagick;
using System;
using System.Drawing;
using System.IO;

namespace VMagik
{
    internal class LiquidRescaler
    {
        public delegate bool FrameAction(Bitmap frame, int frameNumber);

        public VideoCapture VideoSource {
            get;
        }

        public int TotalFrames => (int) VideoSource.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount);
        public double Fps => VideoSource.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
        public Size FrameSize => new Size(VideoSource.Width, VideoSource.Height);

        public LiquidRescaler(string inputFilePath)
        {
            if (!File.Exists(inputFilePath))
            {
                throw new ArgumentException("Input file does not exist.");
            }

            VideoSource = new VideoCapture(inputFilePath);
        }

        public void Process(string outputFilePath, double reductionCoefficient, FrameAction frameAction)
        {
            // TODO: Multithreaded option

            var resultingSize = new Size((int)(FrameSize.Width * reductionCoefficient), (int)(FrameSize.Height * reductionCoefficient));

            var frameNumber = 0;
            var tempFramePath = Path.Combine(Path.GetTempPath(), "tempframe.png");

            using (var videoWriter = new VideoWriter(outputFilePath, -1, (int)Fps, resultingSize, true))
            {
                var continueWorking = true;
                while (continueWorking)
                {
                    frameNumber++;

                    var frame = new Mat();
                    VideoSource.Read(frame);

                    if (frame.IsEmpty)
                    {
                        break;
                    }

                    var scaledFrame = ApplyLiquidRescale(frame, resultingSize, tempFramePath);
                    frame.Dispose();

                    continueWorking = frameAction(scaledFrame.Bitmap, frameNumber);

                    videoWriter.Write(scaledFrame);
                    scaledFrame.Dispose();
                }
            }
        }

        private static Mat ApplyLiquidRescale(IImage original, Size size, string tempFramePath)
        {
            original.Save(tempFramePath);

            using (var magickFrame = new MagickImage(tempFramePath))
            {
                magickFrame.LiquidRescale(size.Width, size.Height);
                magickFrame.Write(tempFramePath);
                var scaledFrame = new Mat(tempFramePath);
                CvInvoke.Resize(scaledFrame, scaledFrame, size);

                File.Delete(tempFramePath);

                return scaledFrame;
            }
        }
    }
}
