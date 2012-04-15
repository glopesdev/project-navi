using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using ProjectNavi.Hardware;
using Cyberiad;

namespace ProjectNavi.Navigation
{
    public static class Locomotion
    {
        public static Action<GameTime> DifferentialSteering(Vehicle vehicle, DifferentialSteeringBoard actuators, double wheelDistance, double inPlaceThreshold, double linearGain, double angularGain, double inPlaceAngularGain)
        {
            var halfDistance = wheelDistance / 2;
            return gameTime =>
            {
                var currentDirection = Vector2.UnitX.Rotate(vehicle.Transform.Rotation);
                var angularSteering = currentDirection.GetRotationTo(vehicle.Steering);
                var linearSteering = Vector2.Dot(currentDirection, vehicle.Steering);

                double left, right;
                if (Math.Abs(angularSteering) < inPlaceThreshold)
                {
                    left = linearGain * linearSteering - angularGain * halfDistance * angularSteering;
                    right = linearGain * linearSteering + angularGain * halfDistance * angularSteering;
                }
                else
                {
                    var rotationDirection = Math.Sign(angularSteering);
                    if (rotationDirection == 0)
                        rotationDirection = 1;
                    right = inPlaceAngularGain * halfDistance * vehicle.Steering.Length() * rotationDirection;
                    left = -right;
                }

                actuators.UpdateWheelVelocity(new WheelVelocity(left, right));
                vehicle.Steering = Vector2.Zero;
            };
        }
    }
}
