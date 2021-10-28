using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Outline.Effects
{
    class BoxBlur : IBitmapEfect
    {
        private int _kernelSize = 3;
        private Kernel _kernel;
        public void Effect(WriteableBitmap input)
        {
            input.Lock();
            _kernel = new Kernel(GenerateKernel(_kernelSize), input.BackBuffer,
                input.BackBufferStride, input.PixelHeight, input.PixelWidth);
            _kernel.Blur();
            { };
            input.Unlock();
        }

        private float[][] GenerateKernel(int size)
        {
            float[][] kernel = new float[size][];
            float[] tmp = new float[size];
            Array.Fill(tmp, (float)1);
            Array.Fill(kernel, tmp);
            return kernel;
        }
    }
}
