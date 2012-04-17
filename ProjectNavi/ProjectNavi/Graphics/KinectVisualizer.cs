using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cyberiad.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectNavi.Bonsai.Kinect;
using Cyberiad;

namespace ProjectNavi.Graphics
{
    public class KinectVisualizer
    {
        readonly Matrix DepthProjectionMatrix;

        public KinectVisualizer(Game game)
        {
            GraphicsDevice = game.GraphicsDevice;
            DepthProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(43), 4 / 3f, .01f, 1000);
        }

        public KinectFrame Frame { get; set; }

        public GraphicsDevice GraphicsDevice { get; private set; }

        // CalculateCursorRay Calculates a world space ray starting at the camera's
        // "eye" and pointing in the direction of the cursor. Viewport.Unproject is used
        // to accomplish this. see the accompanying documentation for more explanation
        // of the math behind this function.
        public Ray CalculateCursorRay(Viewport viewport, Vector2 position, Matrix projectionMatrix, Matrix viewMatrix)
        {
            // create 2 positions in screenspace using the cursor position. 0 is as
            // close as possible to the camera, 1 is as far away as possible.
            Vector3 nearSource = new Vector3(position, 0f);
            Vector3 farSource = new Vector3(position, 1f);

            // use Viewport.Unproject to tell what those two screen space positions
            // would be in world space. we'll need the projection matrix and view
            // matrix, which we have saved as member variables. We also need a world
            // matrix, which can just be identity.
            Vector3 nearPoint = viewport.Unproject(nearSource,
                projectionMatrix, viewMatrix, Matrix.Identity);

            Vector3 farPoint = viewport.Unproject(farSource,
                projectionMatrix, viewMatrix, Matrix.Identity);

            // find the direction vector that goes from the nearPoint to the farPoint
            // and normalize it....
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();

            // and then create a new ray using nearPoint as the source.
            return new Ray(nearPoint, direction);
        }

        const int DepthStep = 4;

        public void DrawKinectDepthMap(Transform2D transform, PrimitiveBatch primitiveBatch)
        {
            if (Frame != null)
            {
                var depthImage = Frame.DepthImage;
                var viewport = new Viewport(0, 0, Frame.Sensor.DepthStream.FrameWidth, Frame.Sensor.DepthStream.FrameHeight);
                primitiveBatch.Begin(PrimitiveType.TriangleList);

                for (int i = 0; i < viewport.Height; i += DepthStep)
                {
                    for (int j = 0; j < viewport.Width; j += DepthStep)
                    {
                        var depth = ((ushort)depthImage[i * viewport.Width + j]) >> 3;
                        if (depth > 0)
                        {
                            var ray = CalculateCursorRay(viewport, new Vector2(j, i), DepthProjectionMatrix, Matrix.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.Up));
                            var obstacle = (depth / 1000f) * new Vector2(-ray.Direction.Z, ray.Direction.X);
                            obstacle = obstacle.Rotate(transform.Rotation) + transform.Position;
                            obstacle *= Constants.PixelsPerWorldUnit * new Vector2(1, -1);

                            var color = GraphicsHelper.HsvToRgb(new Vector3(i, 1, 1));
                            primitiveBatch.AddVertex(obstacle, color);
                            primitiveBatch.AddVertex(obstacle + DepthStep * Vector2.UnitX, color);
                            primitiveBatch.AddVertex(obstacle + DepthStep * Vector2.UnitY, color);
                        }
                    }
                }

                primitiveBatch.End();
            }
        }
    }
}
