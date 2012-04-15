using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cyberiad;
using ProjectNavi.Navigation;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace ProjectNavi.Graphics
{
    public class SteeringVisualizer
    {
        public Vector2 Steering { get; set; }

        public void DrawSteeringVector(Transform2D transform, PrimitiveBatch primitiveBatch)
        {
            primitiveBatch.Begin(PrimitiveType.LineList);
            primitiveBatch.AddVertex(100 * new Vector2(1, -1) * transform.Position, Color.Green);
            primitiveBatch.AddVertex(100 * new Vector2(1, -1) * (Steering + transform.Position), Color.Green);
            primitiveBatch.End();
        }
    }
}
