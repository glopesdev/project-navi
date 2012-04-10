using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cyberiad;
using ProjectNavi.Hardware;

namespace ProjectNavi.Hardware
{
    public class MagabotState :HardwareComponent
    {
        public bool BumperLeft { get; set; }
        public bool BumperRight { get; set; }
        public double Battery { get; set; }
        public double IRGroundLeft { get; set; }
        public double IRGroundMiddle { get; set; }
        public double IRGroundRight { get; set; }
        public double []Sonar { get; private set; }
        public Transform2D Transform { get; set; }
        public LedBoard Leds { get; private set; }
        public DifferentialSteeringBoard DifferentialSteering { get; private set; }
        public MagabotState(LedBoard leds, DifferentialSteeringBoard steering)
        {
            Leds = leds;
            DifferentialSteering = steering;
            Sonar = new double[5];
        }



    }
}
