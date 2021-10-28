using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Outline.Effects
{
    interface IBitmapEfect
    {
        void Effect(WriteableBitmap input);
    }
}
