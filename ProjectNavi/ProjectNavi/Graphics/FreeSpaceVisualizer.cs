using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cyberiad;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace ProjectNavi.Graphics
{
    public class FreeSpaceVisualizer
    {
        public bool[] FreeSpace { get; set; }

        public void DrawFreeSpace(Transform2D transform, PrimitiveBatch primitiveBatch)
        {
            if (FreeSpace != null)
            {
                var sizeX = new Vector2(2, 0);
                var sizeY = new Vector2(0, 2);
                primitiveBatch.Begin(PrimitiveType.TriangleList);
                for (int i = 0; i < FreeSpace.Length; i++)
                {
                    var color = FreeSpace[i] ? Color.Green : Color.Red;
                    primitiveBatch.AddVertex(transform.Position + i * sizeX, color);
                    primitiveBatch.AddVertex(transform.Position + i * sizeX + sizeX, color);
                    primitiveBatch.AddVertex(transform.Position + i * sizeX + sizeX + sizeY, color);
                    primitiveBatch.AddVertex(transform.Position + i * sizeX + sizeX + sizeY, color);
                    primitiveBatch.AddVertex(transform.Position + i * sizeX + sizeY, color);
                    primitiveBatch.AddVertex(transform.Position + i * sizeX, color);
                }
                primitiveBatch.End();
            }
        }
    }
}
