using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cyberiad;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace ProjectNavi.Graphics
{
    public class SonarVisualizer
    {
        readonly Transform2D[] SonarTransforms = new[]
        {
            new Transform2D(new Vector2(0.15f, 0.14f), MathHelper.ToRadians(45), Vector2.One),
            new Transform2D(new Vector2(0.165f, 0.07f), MathHelper.ToRadians(20), Vector2.One),
            new Transform2D(new Vector2(0.18f, 0.0f), MathHelper.ToRadians(0), Vector2.One),
            new Transform2D(new Vector2(0.165f, -0.07f), MathHelper.ToRadians(-20), Vector2.One),
            new Transform2D(new Vector2(0.15f, -0.14f), MathHelper.ToRadians(-45), Vector2.One),
        };

        public short[] SonarFrame { get; set; }

        public void DrawSonarFrame(Transform2D transform, PrimitiveBatch primitiveBatch)
        {
            var sonarFrame = SonarFrame;
            if (sonarFrame != null)
            {
                primitiveBatch.Begin(PrimitiveType.LineList);

                for (int i = 0; i < sonarFrame.Length; i++)
                {
                    var measurement = sonarFrame[i] / 100f;
                    var obstacle = new Vector2(
                        SonarTransforms[i].Position.X + measurement * (float)Math.Cos(SonarTransforms[i].Rotation),
                        SonarTransforms[i].Position.Y + measurement * (float)Math.Sin(SonarTransforms[i].Rotation));
                    var sonarStart = SonarTransforms[i].Position.Rotate(transform.Rotation) + transform.Position;
                    obstacle = obstacle.Rotate(transform.Rotation) + transform.Position;
                    sonarStart *= 100 * new Vector2(1, -1);
                    obstacle *= 100 * new Vector2(1, -1);

                    //var offset = 4 * Vector2.UnitY.Rotate(transform.Rotation);
                    //var offset = Vector2.Normalize((obstacle - sonarStart).Rotate(MathHelper.PiOver2));
                    //primitiveBatch.AddVertex(sonarStart, Color.Red);
                    //primitiveBatch.AddVertex(obstacle, Color.Red);
                    //primitiveBatch.AddVertex(obstacle + offset, Color.Red);
                    //primitiveBatch.AddVertex(obstacle + offset, Color.Red);
                    //primitiveBatch.AddVertex(sonarStart + offset, Color.Red);
                    //primitiveBatch.AddVertex(sonarStart, Color.Red);

                    primitiveBatch.AddVertex(sonarStart, Color.Red);
                    primitiveBatch.AddVertex(obstacle, Color.Red);
                }

                primitiveBatch.End();
            }
        }
    }
}
