using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectNavi.Hardware;
using Cyberiad;

namespace ProjectNavi.Entities
{
    public class RemoteControl 
    {
        BumperBoard bumpers;
        BatteryBoard battery; 
        GroundSensorBoard ground; 
        LedBoard leds; 
        SonarsBoard sonars; 
        DifferentialSteeringBoard steering; 
        OdometryBoard odometry;
        public RemoteControl(BumperBoard bumpers, BatteryBoard battery, GroundSensorBoard ground, LedBoard leds, SonarsBoard sonars, DifferentialSteeringBoard steering, OdometryBoard odometry)
        {
            this.bumpers = bumpers;
            this.battery = battery;
            this.ground = ground;
            this.leds = leds;
            this.sonars = sonars;
            this.steering = steering;
            this.odometry = odometry;
        }
    }

}
