using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Cyberiad;

namespace ProjectNavi.Navigation
{
    public static class Steering
    {
        public static Vector2 PolarToCartesian(Vector2 polar)
        {
            return new Vector2(polar.X * (float)Math.Cos(polar.Y), polar.X * (float)Math.Sin(polar.Y));
        }

        public static Vector2 CartesianToPolar(Vector2 cartesian)
        {
            return new Vector2(cartesian.Length(), (float)Math.Atan2(cartesian.Y, cartesian.X));
        }

        public static Action<GameTime> Arrival(Transform2D target, Vehicle agent, float minSpeed, float maxSpeed, float tolerance)
        {
            return gameTime =>
            {
                var desiredVelocity = (target.Position - agent.Transform.Position);
                if (desiredVelocity.Length() > tolerance)
                {
                    desiredVelocity += minSpeed * Vector2.Normalize(desiredVelocity);
                    desiredVelocity = desiredVelocity.Truncate(maxSpeed);
                    agent.Steering += desiredVelocity;
                }
            };
        }

        public static Action<GameTime> PathFollow(Transform2D target, IEnumerable<Vector2> path, Vehicle agent, float minSpeed, float maxSpeed, float tolerance)
        {
            bool waypoint = true;
            var pathEnumerator = path.GetEnumerator();
            var steering = Arrival(target, agent, minSpeed, maxSpeed, tolerance);
            return gameTime =>
            {
                if (waypoint && pathEnumerator.MoveNext())
                {
                    target.Position = pathEnumerator.Current;
                    waypoint = false;
                }
                else
                {
                    steering(gameTime);
                    if (agent.Steering.Length() <= tolerance) waypoint = true;
                }
            };
        }
    }
}
