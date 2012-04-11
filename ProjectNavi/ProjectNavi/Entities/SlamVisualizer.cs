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

namespace ProjectNavi.Entities
{
    public class SlamVisualizer
    {
        SpriteRenderer renderer;
        SlamController controller;
        List<SlamElement> elements;
        Texture2D covarianceTexture;

        public SlamVisualizer(Game game, SpriteRenderer renderer, SlamController controller)
        {
            covarianceTexture = TextureFactory.CreateCircleTexture(game.GraphicsDevice, 10, Color.White);
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
                var render = renderer.SubscribeTexture(transform, covarianceTexture);
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
