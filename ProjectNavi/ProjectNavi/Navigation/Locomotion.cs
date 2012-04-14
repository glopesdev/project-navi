using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using ProjectNavi.Hardware;

namespace ProjectNavi.Navigation
{
    public static class Locomotion
    {
        public static IEnumerable<Action<GameTime>> DifferentialSteering(Vehicle vehicle, DifferentialSteeringBoard actuators, double wheelDistance, double gain)
        {
            var halfDistance = wheelDistance / 2;
            while (true)
            {
                yield return gameTime =>
                {
                    var left = vehicle.Steering.X - halfDistance * vehicle.Steering.Y;
                    var right = vehicle.Steering.X + halfDistance * vehicle.Steering.Y;
                    actuators.UpdateWheelVelocity(new WheelVelocity(gain * left, gain * right));
                    vehicle.Steering = Vector2.Zero;
                };
            }
        }
    }
}
