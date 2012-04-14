using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Reactive.Disposables;
using Cyberiad;

namespace ProjectNavi.Graphics
{
    public class PrimitiveBatchRenderer : Microsoft.Xna.Framework.DrawableGameComponent
    {
        event Action draw;
        PrimitiveBatch primitiveBatch;

        public PrimitiveBatchRenderer(Game game)
            : base(game)
        {
        }

        public Matrix View
        {
            get { return primitiveBatch.View; }
            set { primitiveBatch.View = value; }
        }

        public Matrix Projection { get; set; }

        public IDisposable SubscribePrimitive(Transform2D transform, Action<Transform2D, PrimitiveBatch> primitive)
        {
            Action handler = () =>
            {
                primitive(transform, primitiveBatch);
            };

            draw += handler;
            return Disposable.Create(() => draw -= handler);
        }

        protected override void LoadContent()
        {
            primitiveBatch = new PrimitiveBatch(GraphicsDevice);
            base.LoadContent();
        }

        private void OnDraw()
        {
            var handler = draw;
            if (handler != null)
            {
                handler();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            OnDraw();
            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            primitiveBatch.Dispose();
            base.UnloadContent();
        }
    }
}
