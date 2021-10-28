using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Outline.Effects
{
    class Kernel
    {
        private float[][] _kernel;
        private IntPtr _backBuffer;
        private int _stride;
        private int _height;
        private int _width;
        private int _ptrStride;
        private int _borderSize;
        private int _kernelSize;
        private float[] _flatKernel;
        public Kernel(float[][] kernel, IntPtr backBuffer, int stride, int height, int width)
        //public Kernel(float[][] kernel)
        {
            _kernel = kernel;
            _backBuffer = backBuffer;
            _stride = stride;
            _ptrStride = stride / 4;
            _borderSize = _kernel.Length / 2;
            _kernelSize = _kernel.Length * _kernel.Length;
            _height = height;
            _width = width;
            
            _flatKernel = new float[_kernelSize];
            int currentIndex = 0;
            //Array.ForEach(_kernel, new Action<float[]>((subkernel) =>
            //{
            foreach (var subkernel in _kernel)
            {
                subkernel.CopyTo(_flatKernel, currentIndex);
                currentIndex += _kernel.Length;
            }
            //}));
            
        }
        public void Blur()
        {
            RunAll(average);

        }

        private UInt32 average(IntPtr[] pixels)
        {
            uint[] totals = new uint[4] { 0, 0, 0, 0 };
            var zipped = pixels.Zip(_flatKernel);
            foreach (var p in zipped)
            {
                unsafe
                {
                    IntPtr pixel = p.Item1;
                    float weight = p.Item2;
                    byte* ptr = (byte*)pixel.ToPointer();
                    for (int i = 0; i < 4; i++)
                    {
                        totals[i] += (uint)(*ptr * weight);
                        ptr++;
                    }
                }
            }
            UInt32 result = 0;
            unsafe
            {
                byte* ptr = (byte*) &result;
                *ptr = (byte)(totals[0] / _kernelSize);
                ptr++;
                *ptr = (byte)(totals[1] / _kernelSize);
                ptr++;
                *ptr = (byte)(totals[2] / _kernelSize);
                ptr++;
                *ptr = 255;


            }
            //Trace.WriteLine(totals);
            return result;
            //return 1000;
        }

        private IntPtr PointerFromCoord(int x, int y)
        {
            return _backBuffer + (_stride * y) + x * 4;
        }

        private IntPtr[] PixelPointers(int x, int y)
        {
            // Takes the centre pixel coord
            // Needs to start from the top left pixel
            IntPtr[] pixels = new IntPtr[_kernelSize];
            int halfLength = _kernel.Length / 2;
            for (int i = 0 - halfLength; i <= halfLength; i++)
            {
                for (int j = 0 - halfLength; j <= halfLength; j++)
                {
                    int idx = (i+halfLength)*_kernel.Length + j + halfLength;
                    //Trace.WriteLine((i,j));
                    //Trace.WriteLine(idx);
                    pixels[idx] = _backBuffer + _stride * (i + y) + j + x * 4;
                }
            }
            return pixels;
        }
        private void RunAll(Func<IntPtr[], UInt32> f)
        {
            //Assume square kenel

            int reqiredSize = 2 * _borderSize;
            bool small = _height - reqiredSize <= 0 || _width - reqiredSize <= 0;

            int topRow = small ? 0 : _borderSize;
            int bottomRow = small ? _height - 1 : _height - _borderSize - 1;
            int leftRow = small ? 0 : _borderSize;
            int rightRow = small ? _width - 1 : _width - _borderSize - 1;

            for (int i = topRow; i <= 500; i++)
            {
                for (int j = leftRow; j <= 500; j++)
                {
                    var pixels = PixelPointers(j, i);
                    UInt32 result = f(pixels);
                    IntPtr pixel = PointerFromCoord(j, i);
                    unsafe
                    {
                        var ptr = (UInt32*) pixel.ToPointer();
                        *ptr = (uint) result;
                        //ptr = 0;
                    }
                }
            }
        }
    }
}
