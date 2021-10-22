using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Storage.Pickers;
using System.Diagnostics;
using Windows.Media.Capture;
using System.Windows.Threading;
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Media.MediaProperties;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Media.Capture.Frames;
using WinRT;

namespace Outline
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private MediaCapture _mediaCapture;
        private MediaFrameReader _frameReader;
        private SoftwareBitmap _tmpBuffer;
        private WriteableBitmap _bitmap;
        private bool _taskRunning;
        private FrameProcessor _frameProcessor;

        public MainWindow()
        {
            InitializeComponent();
        }
        
        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            await StartFrameReaderAsync();
        }



        private async Task StartFrameReaderAsync()
        {
            _mediaCapture = new MediaCapture();

            var initSettings = new MediaCaptureInitializationSettings();
            var frame_sources = await MediaFrameSourceGroup.FindAllAsync();
            // This will crash where no cameras are available
            initSettings.SourceGroup = frame_sources[0];
            initSettings.MemoryPreference = MediaCaptureMemoryPreference.Cpu;
            await _mediaCapture.InitializeAsync(initSettings);

            var FrameSource = _mediaCapture.FrameSources.First().Value;

            _bitmap = new WriteableBitmap(
                (int) FrameSource.CurrentFormat.VideoFormat.Width,
                (int) FrameSource.CurrentFormat.VideoFormat.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null);

            DrawArea.Source = _bitmap;
            _frameProcessor = new FrameProcessor(_bitmap);


            _frameReader = await _mediaCapture.CreateFrameReaderAsync(FrameSource);
            _frameReader.FrameArrived += ProcessFrame;
            await _frameReader.StartAsync();

        }

        private void ProcessFrame(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            var mediaFrameReference = sender.TryAcquireLatestFrame();
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var frameBitmap = videoMediaFrame?.SoftwareBitmap;

            if (frameBitmap == null)
            {
                return;
            }

            var frameBitmapBgra8 = frameBitmap;
            if (frameBitmap is { BitmapPixelFormat: not BitmapPixelFormat.Bgra8 } or { BitmapAlphaMode: not BitmapAlphaMode.Premultiplied })
            {
                frameBitmapBgra8 = SoftwareBitmap.Convert(frameBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                frameBitmap.Dispose();
            }
            _frameProcessor.ProcessFrame(frameBitmapBgra8);
            frameBitmapBgra8 = Interlocked.Exchange(ref _tmpBuffer, frameBitmapBgra8);
            frameBitmapBgra8?.Dispose();

        }
        


    }
}
