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

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

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

        public MainWindow()
        {
            Trace.WriteLine("Test");
            InitializeComponent();
        }
        
        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            //await StartFrameReaderAsync();
            //_mediaCapture = new MediaCapture();
            //var frame_sources = await MediaFrameSourceGroup.FindAllAsync();
            //Trace.WriteLine(frame_sources.Count);
            //Trace.WriteLine(frame_sources[0].DisplayName);
            //Trace.WriteLine(frame_sources[0].Id);
            //var infos = frame_sources[0].SourceInfos;
            //Trace.WriteLine(infos[0].MediaStreamType);


            //await _mediaCapture.InitializeAsync(initSettings);
            //Thread.Sleep(1000);
            Trace.WriteLine("oi");
            try
            {
                //await _mediaCapture.StartPreviewAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when starting the preview: {0}", ex.ToString());
            }
            //Tick();
        }

        private async void StartClick(object sender, RoutedEventArgs e)
        {
            await StartFrameReaderAsync();
        }

        private async Task StartFrameReaderAsync()
        {
            _mediaCapture = new MediaCapture();

            var initSettings = new MediaCaptureInitializationSettings();
            var frame_sources = await MediaFrameSourceGroup.FindAllAsync();
            initSettings.SourceGroup = frame_sources[0];
            initSettings.MemoryPreference = MediaCaptureMemoryPreference.Cpu;
            await _mediaCapture.InitializeAsync(initSettings);
            //await _mediaCapture.InitializeAsync();

            //var frameInfos = frame_sources[0].SourceInfos[0];
            //var frameSource = _mediaCapture.FrameSources.FirstOrDefault();

            var FrameSource = _mediaCapture.FrameSources.First().Value;

            _bitmap = new WriteableBitmap(
                (int) FrameSource.CurrentFormat.VideoFormat.Width,
                (int) FrameSource.CurrentFormat.VideoFormat.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null);

            DrawArea.Source = _bitmap;
            //Trace.WriteLine(FrameSource.Value.SupportedFormats.First().MajorType);
            //Trace.WriteLine(FrameSource.Value.SupportedFormats.First().Subtype);
            //Trace.WriteLine(preferredFormat.MajorType);
            //Trace.WriteLine(preferredFormat.Subtype);

            //_frameReader = await _mediaCapture.CreateFrameReaderAsync (FrameSource, MediaEncodingSubtypes.Argb32);
            _frameReader = await _mediaCapture.CreateFrameReaderAsync(FrameSource);
            _frameReader.FrameArrived += ProcessFrame;
            await _frameReader.StartAsync();


            //await _mediaCapture.StartPreviewAsync();
            //_timer = new DispatcherTimer();
            //_timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            //_timer.Tick += Tick;
            //_timer.Start();
        }

        private void ProcessFrame(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            var mediaFrameReference = sender.TryAcquireLatestFrame();
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var frameBitmap = videoMediaFrame?.SoftwareBitmap;

            Trace.WriteLine("ProcessFrame");

            if (frameBitmap == null)
            {
                Trace.WriteLine("Nothing");
                return;
            }
            if (frameBitmap is { BitmapPixelFormat: not BitmapPixelFormat.Bgra8 } or { BitmapAlphaMode: not BitmapAlphaMode.Premultiplied })
            {
               frameBitmap = SoftwareBitmap.Convert(frameBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }
            frameBitmap = Interlocked.Exchange(ref _tmpBuffer, frameBitmap);
            frameBitmap?.Dispose();

            SoftwareBmpToWriteableBmp(_tmpBuffer, _bitmap);

        }
        
        private unsafe void SoftwareBmpToWriteableBmp(SoftwareBitmap origin, WriteableBitmap destination)
        {
            using (var buffer = origin.LockBuffer(BitmapBufferAccessMode.Read))
            using (var reference = buffer.CreateReference())
            {
                Trace.WriteLine("oi");
                if (_taskRunning)
                {
                    return;
                }
                _taskRunning = true;

                _bitmap.Dispatcher.Invoke(() =>
                {
                    destination.Lock();
                    var out_buffer = destination.BackBuffer;

                    byte* data;
                    uint capacity;
                    reference.As<IMemoryBufferByteAccess>().GetBuffer(out data, out capacity);
                    int dest_capacity = destination.PixelHeight * destination.PixelWidth * 4;

                    Buffer.MemoryCopy((void*) data, (void*) out_buffer, (long) dest_capacity, (long) (capacity - 1));
                    destination.AddDirtyRect(new Int32Rect(0, 0, destination.PixelWidth, destination.PixelHeight));

                    destination.Unlock();
                });
                _taskRunning = false;
            }
        }


    }
}
