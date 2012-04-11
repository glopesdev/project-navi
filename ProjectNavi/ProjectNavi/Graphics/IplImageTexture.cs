using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using OpenCV.Net;
using System.Runtime.InteropServices;

namespace ProjectNavi.Graphics
{
    public class IplImageTexture
    {
        bool update;
        Texture2D texture;
        byte[] textureBuffer;

        public IplImageTexture(GraphicsDevice graphicsDevice, int width, int height)
        {
            texture = new Texture2D(graphicsDevice, width, height);
            textureBuffer = new byte[width * height * 4];
        }

        public Texture2D Texture
        {
            get { return texture; }
        }

        public void SetData(IplImage image)
        {
            var bufferHandle = GCHandle.Alloc(textureBuffer, GCHandleType.Pinned);
            var bufferHeader = new IplImage(image.Size, 8, 4, bufferHandle.AddrOfPinnedObject());
            ImgProc.cvCvtColor(image, bufferHeader, ColorConversion.BGR2RGBA);
            bufferHandle.Free();

            update = true;
        }

        public void Update()
        {
            if (update)
            {
                texture.SetData(textureBuffer);
                update = false;
            }
        }
    }
}
