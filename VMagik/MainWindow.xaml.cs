using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Emgu.CV;
using ImageMagick;
using Image = System.Drawing.Image;

namespace VMagik
{
    public partial class MainWindow
    {
        private readonly BackgroundWorker _backgroundWorker;
        private readonly ManualResetEvent _busy;
        private bool _isPaused;
        private DateTime _lastFrameProcessed;

        public MainWindow()
        {
            InitializeComponent();

            CvInvoke.UseOptimized = true;

            CvInvoke.UseOpenCL = true;
            OpenCL.IsEnabled = true;

            _backgroundWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };
            _backgroundWorker.ProgressChanged += OnRenderProgressUpdate;
            _backgroundWorker.DoWork += DoRenderWork;
            _backgroundWorker.RunWorkerCompleted += OnRenderCompleted;

            _busy = new ManualResetEvent(true);
            _isPaused = false;

            UpdateStatusText("Ready.");
            EnableInputs();
        }

        #region Utilities
        private void UpdateStatusText(string text)
        {
            OutputInfoLabel.Content = text;
        }

        private void UpdatePreviewImage(Image image)
        {
            using (var memory = new MemoryStream())
            {
                image.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                ImageRenderPreview.Source = bitmapImage;
            }
        }

        private static void PromptForFile(TextBox destination, bool needsToExist, string videoType)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                DefaultExt = "." + videoType,
                Filter = string.Format("Video Files (*.{0})|*.{0}", videoType),
                CheckFileExists = needsToExist
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                destination.Text = dialog.FileName;
            }
        }

        private void DisableInputs()
        {
            FileInputBox.IsEnabled = false;
            FileOutputBox.IsEnabled = false;
            AmountSlider.IsEnabled = false;
            RenderStartButton.IsEnabled = false;
            GpuAccelerationCheckbox.IsEnabled = false;

            RenderCancelButton.IsEnabled = true;
            RenderPauseResumeButton.IsEnabled = true;
        }

        private void EnableInputs()
        {
            FileInputBox.IsEnabled = true;
            FileOutputBox.IsEnabled = true;
            AmountSlider.IsEnabled = true;
            RenderStartButton.IsEnabled = true;
            GpuAccelerationCheckbox.IsEnabled = true;

            RenderCancelButton.IsEnabled = false;
            RenderPauseResumeButton.IsEnabled = false;
        }
        #endregion

        #region Render Work
        private void DoRenderWork(object sender, DoWorkEventArgs eventArgs)
        {
            var rescaleInfo = eventArgs.Argument is LiquidRescaleInfo info ? info : new LiquidRescaleInfo();
            var video = new LiquidRescaler(rescaleInfo.InputPath);
            var worker = sender as BackgroundWorker;

            video.Process(rescaleInfo.OutputPath, rescaleInfo.Amount, (frameImage, frameNumber) =>
            {
                worker?.ReportProgress((int)(100 * ((double)frameNumber / video.TotalFrames)), new LiquidRescaleProgress(frameImage, frameNumber, video.TotalFrames));
                rescaleInfo.ResetEvent.WaitOne();
                return !_backgroundWorker.CancellationPending;
            });
        }

        private void OnRenderProgressUpdate(object sender, ProgressChangedEventArgs eventArgs)
        {
            if (eventArgs.UserState != null)
            {
                var rescaleProgress = eventArgs.UserState is LiquidRescaleProgress progress ? progress : new LiquidRescaleProgress();

                var remainingTimeStr = "unknown time remaining";

                var now = DateTime.Now;
                var fps = Math.Round(1 / (now - _lastFrameProcessed).TotalSeconds, 2);

                if (fps > 0)
                {
                    var remainingTime = TimeSpan.FromSeconds((1 / fps) * (rescaleProgress.TotalFrames - rescaleProgress.CurrentFrame));
                    remainingTimeStr = remainingTime.ToString(@"hh\:mm\:ss") + " remaining";
                }

                _lastFrameProcessed = now;

                if (!_isPaused)
                {
                    UpdateStatusText($"Rendering frame {rescaleProgress.CurrentFrame}/{rescaleProgress.TotalFrames} " +
                                     $"({eventArgs.ProgressPercentage}%, {fps} fps, {remainingTimeStr}, {rescaleProgress.Image.Size.Width} x {rescaleProgress.Image.Height})");
                    RenderProgressBar.Value = (double)eventArgs.ProgressPercentage / 100;
                }
                else
                {
                    UpdateStatusText($"Paused at frame {rescaleProgress.CurrentFrame}/{rescaleProgress.TotalFrames}");
                }

                UpdatePreviewImage(rescaleProgress.Image);
            }
        }

        private void OnRenderCompleted(object sender, RunWorkerCompletedEventArgs eventArgs)
        {
            EnableInputs();
            UpdateStatusText("Finished!");
            RenderProgressBar.Value = 0;
        }
        #endregion

        #region UI Event Handlers
        private void OnInputFocused(object sender, RoutedEventArgs e)
        {
            PromptForFile(FileInputBox, true, "mp4");
        }

        private void OnOutputFocused(object sender, RoutedEventArgs e)
        {
            PromptForFile(FileOutputBox, false, "avi");
        }

        private void OnGpuAccelerationCheckboxClicked(object sender, RoutedEventArgs e)
        {
            if (GpuAccelerationCheckbox?.IsChecked == null) return;

            CvInvoke.UseOpenCL = GpuAccelerationCheckbox.IsChecked.Value;
            OpenCL.IsEnabled = GpuAccelerationCheckbox.IsChecked.Value;
        }

        private void OnRenderPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                RenderPauseResumeButton.Content = "Pause";
                _busy.Set();
                _isPaused = false;
            }
            else
            {
                RenderPauseResumeButton.Content = "Resume";
                _busy.Reset();
                _isPaused = true;
            }
        }

        private void OnRenderStartButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(FileInputBox.Text))
            {
                UpdateStatusText("Input file not found.");
                return;
            }

            if (File.Exists(FileOutputBox.Text))
            {
                MessageBoxResult dialogResult = MessageBox.Show($"The output file {FileOutputBox.Text} already exists. Delete it?",
                    "File Exists", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    File.Delete(FileOutputBox.Text);
                }
                else
                {
                    UpdateStatusText("Output file already exists.");
                    return;
                }
            }

            UpdateStatusText("Starting render...");

            DisableInputs();
            _backgroundWorker.RunWorkerAsync(new LiquidRescaleInfo(FileInputBox.Text, FileOutputBox.Text, 1 - AmountSlider.Value, _busy));
        }

        private void OnRenderCancelButtonClicked(object sender, RoutedEventArgs e)
        {
            _backgroundWorker.CancelAsync();
            EnableInputs();
        }
        #endregion
    }
}
