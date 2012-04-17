using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cyberiad;
using Microsoft.Xna.Framework.Graphics;
using Cyberiad.Graphics;
using Microsoft.Xna.Framework;
using ProjectNavi.Localization;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Reactive.Disposables;

namespace ProjectNavi.Entities
{
    public class SlamVisualizer
    {
        SpriteRenderer renderer;
        SlamController controller;
        List<SlamElement> elements;
        Texture2D covarianceTexture;
        SpriteFont font;

        public SlamVisualizer(Game game, SpriteRenderer renderer, SlamController controller)
        {
            font = game.Content.Load<SpriteFont>("DebugFont");
            var textureColor = new Color(255, 255, 255, 100);
            var strokeColor = new Color(0, 0, 0, 100);
            covarianceTexture = TextureFactory.CreateCircleTexture(game.GraphicsDevice, 1000, 20, 5, 5, textureColor, strokeColor);
            elements = new List<SlamElement>();
            this.controller = controller;
            this.renderer = renderer;
        }

        public void Update()
        {
            var elementsToAdd = ((controller.SlamEstimator.Mean.Count - 3) / 2) - elements.Count + 1;
            for (int i = 0; i < elementsToAdd; i++)
            {
                var transform = new Transform2D();
                var scaleTransform = new Transform2D(Vector2.Zero, 0, 0.01f * Vector2.One);
                var render = renderer.SubscribeTexture(transform, scaleTransform, covarianceTexture);
                if (elements.Count > 0)
                {
                    var landmarkId = controller.GetLandmarkId(elements.Count - 1);
                    var text = renderer.SubscribeText(transform, font, () => landmarkId.ToString(), Color.Red);
                    render = new CompositeDisposable(render, text);
                }
                elements.Add(new SlamElement(render, transform));
            }

            for (int i = 0; i < elements.Count; i++)
            {
                CopyCovarianceTransform(controller.SlamEstimator, i - 1, elements[i].Transform);
            }
        }

        void CopyCovarianceTransform(EkfSlam slam, int index, Transform2D transform)
        {
            var m = index >= 0 ? 2 * index + 3 : 0;
            var mean = slam.Mean.Take(m, m + 1);
            var covariance = slam.Covariance.Take(m, m + 1);

            var eigendecomposition = covariance.Evd();
            var eigenvalues = eigendecomposition.EigenValues();
            var eigenvectors = eigendecomposition.EigenVectors();

            // Transform of a unit circle
            var translation = new Vector2((float)mean[0], (float)mean[1]);
            var rotation = (float)Math.Atan2(eigenvectors[1, 0], eigenvectors[0, 0]);
            var scale = new Vector2((float)eigenvalues[0].Magnitude, (float)eigenvalues[1].Magnitude);

            transform.Position = translation;
            transform.Rotation = rotation;
            transform.Scale = scale;
        }

        class SlamElement
        {
            public SlamElement(IDisposable renderElement, Transform2D transform)
            {
                RenderElement = renderElement;
                Transform = transform;
            }

            public IDisposable RenderElement { get; private set; }

            public Transform2D Transform { get; private set; }
        }
    }
}
