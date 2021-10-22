using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Graphics.Imaging;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using WinRT;
using System.Windows;
using System.Windows.Media;

namespace Outline
{

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    class FrameProcessor
    {
        private static Mutex mut = new Mutex();
        private bool _running = false;
        private int _framesDropped = 0;
        private SoftwareBitmap _tmpBuffer;
        private WriteableBitmap _outputBuffer;

        public FrameProcessor(WriteableBitmap outputBuffer)
        {
            _outputBuffer = outputBuffer;
        }

        public void ProcessFrame(SoftwareBitmap bitmap)
        {
            mut.WaitOne();
            if (_tmpBuffer != null)
            {
                _tmpBuffer.Dispose();
                _tmpBuffer = null;
                _framesDropped++;
            }
            _tmpBuffer = SoftwareBitmap.Copy(bitmap);

            if (!_running)
            {
                _running = true;
                Task.Run(() => doProcessing());
            }

            mut.ReleaseMutex();
            bitmap.Dispose();

        }

        private void doProcessing()
        {
            mut.WaitOne();
            if (_tmpBuffer == null)
            {
                _running = false;
                return;
            }
            var bitmap = SoftwareBmpToWriteableBmp(_tmpBuffer);
            _tmpBuffer.Dispose();
            _tmpBuffer = null;
            mut.ReleaseMutex();

            DrawFrame(bitmap);
            _running = false;

        }

        private unsafe WriteableBitmap SoftwareBmpToWriteableBmp(SoftwareBitmap origin)
        {
            using (var buffer = origin.LockBuffer(BitmapBufferAccessMode.Read))
            using (var reference = buffer.CreateReference())
            {
                WriteableBitmap output = new WriteableBitmap
                (
                    origin.PixelWidth,
                    origin.PixelHeight,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null
                );

                output.Lock();
                var out_buffer = output.BackBuffer;

                byte* data;
                uint capacity;
                reference.As<IMemoryBufferByteAccess>().GetBuffer(out data, out capacity);
                int dest_capacity = output.PixelHeight * output.PixelWidth * 4;

                Buffer.MemoryCopy((void*)data, (void*)out_buffer, (long)dest_capacity, (long)(capacity - 1));
                output.AddDirtyRect(new Int32Rect(0, 0, output.PixelWidth, output.PixelHeight));
                output.Unlock();

                return output;
            }
        }

        private unsafe void DrawFrame(WriteableBitmap frame)
        {
            frame.Lock();
            var frame_buffer = frame.BackBuffer;
            _outputBuffer.Dispatcher.Invoke(() =>
            {
                _outputBuffer.Lock();
                int capacity = _outputBuffer.PixelWidth * _outputBuffer.PixelHeight * 4;
                var out_buffer = _outputBuffer.BackBuffer;
                Buffer.MemoryCopy((void*)frame_buffer, (void*)out_buffer, capacity, capacity);
                _outputBuffer.AddDirtyRect(new Int32Rect(0, 0, _outputBuffer.PixelWidth, _outputBuffer.PixelHeight));
                _outputBuffer.Unlock();
            });
            frame.Unlock();
        }
    }

}
