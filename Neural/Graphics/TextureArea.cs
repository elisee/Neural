using Neural.Math;
using System;

namespace Neural.Graphics
{
    public class TextureArea
    {
        public IntPtr Texture;
        public Rectangle Rectangle;

        public TextureArea(IntPtr texture, Rectangle area)
        {
            Texture = texture;
            Rectangle = area;
        }
    }
}
