using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Cyberiad;
using ProjectNavi.Navigation;
using Microsoft.Xna.Framework.Content;

namespace ProjectNavi.Tasks
{
    public class MoveToLandmark : TaskProvider
    {
        public MoveToLandmark()
        {
            MinSpeed = Steering.DefaultMinSpeed;
            MaxSpeed = Steering.DefaultMaxSpeed;
            Tolerance = Steering.DefaultTolerance;
        }

        [ContentSerializer(Optional = true)]
        public float MinSpeed { get; set; }

        [ContentSerializer(Optional = true)]
        public float MaxSpeed { get; set; }

        [ContentSerializer(Optional = true)]
        public float Tolerance { get; set; }

        public override IEnumerable<Action<GameTime>> Action(Game game, IServiceProvider provider)
        {
            var transform = provider.GetService<Transform2D>();
            var vehicle = provider.GetService<Vehicle>();

            bool arrived = false;
            var arrival = Steering.Arrival(transform, vehicle, MinSpeed, MaxSpeed, Tolerance);
            while (!arrived)
            {
                yield return gameTime =>
                {
                    arrival(gameTime);
                    if (vehicle.Steering.Length() <= Tolerance) arrived = true;
                };
            }
        }
    }
}
