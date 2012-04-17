using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Cyberiad.Graphics;
using Cyberiad;
using System.Reactive.Disposables;

namespace ProjectNavi.Entities
{
    public class Grid
    {
        public static IDisposable Create(Game game, SpriteRenderer renderer)
        {
            return (from grid in Enumerable.Range(0, 1)
                    let transform = new Transform2D()
                    let texture = game.Content.Load<Texture2D>("map")
                    select new CompositeDisposable(
                        renderer.SubscribeTexture(transform, texture)))
                    .First();
        }
    }
}
