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
        IObservable<IDifferentialSteeringResponse> CommandChecksum { get; }
    }

    public struct IDifferentialSteeringResponse
    {
        WheelVelocity wheelVelocity;
        short checkSum;

        public IDifferentialSteeringResponse(WheelVelocity velocity, short checkSum)
        {
            wheelVelocity = velocity;
            this.checkSum = checkSum;
        }

        public WheelVelocity Velocity
        {
            get { return wheelVelocity; }
        }

        public short CheckSum
        {
            get { return checkSum; }
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
