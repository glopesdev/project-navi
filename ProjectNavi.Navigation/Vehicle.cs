using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cyberiad;
using Microsoft.Xna.Framework;

namespace ProjectNavi.Navigation
{
    public class Vehicle
    {
        public Vehicle()
        {
            Transform = new Transform2D();
        }

        public Transform2D Transform { get; private set; }

        public Vector2 Velocity { get; set; }

        public Vector2 Steering { get; set; }
    }
}
