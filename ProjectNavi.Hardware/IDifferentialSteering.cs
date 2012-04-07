using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectNavi.Hardware
{
    public interface IDifferentialSteering
    {
        void Actuate();
        void UpdateWheelVelocity(WheelVelocity wheelVelocity);
        IObservable<DifferentialSteeringResponse> CommandChecksum { get; }
    }

    public struct DifferentialSteeringResponse
    {
        WheelVelocity wheelVelocity;

        public DifferentialSteeringResponse(WheelVelocity velocity)
        {
            wheelVelocity = velocity;
        }

        public WheelVelocity Velocity
        {
            get { return wheelVelocity; }
        }
    }

    public struct WheelVelocity
    {
        double leftVelocity;
        double rightVelocity;

        public WheelVelocity(double leftVelocity, double rightVelocity)
        {
            this.leftVelocity = leftVelocity;
            this.rightVelocity = rightVelocity;
        }

        public double LeftVelocity
        {
            get { return leftVelocity; }
        }

        public double RightVelocity
        {
            get { return rightVelocity; }
        }
    }
}
