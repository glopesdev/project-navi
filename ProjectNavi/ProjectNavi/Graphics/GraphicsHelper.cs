using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace ProjectNavi.Graphics
{
    public static class GraphicsHelper
    {
        public static Color HsvToRgb(Vector3 color)
        {
            var h = color.X;
            var s = color.Y;
            var v = color.Z;
            var c = s * v;
            h = h / 60f;
            var x = c * (1 - Math.Abs(h % 2 - 1));
            if (h >= 0 && h < 1) return new Color(c, x, 0);
            if (h >= 1 && h < 2) return new Color(x, c, 0);
            if (h >= 2 && h < 3) return new Color(0, c, x);
            if (h >= 3 && h < 4) return new Color(0, x, c);
            if (h >= 4 && h < 5) return new Color(x, 0, c);
            if (h >= 5 && h < 6) return new Color(c, 0, x);
            return Color.Black;
        }
    }
}
