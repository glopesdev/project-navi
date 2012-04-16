using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;

namespace ProjectNavi.Hardware
{
    public class DifferentialSteeringBoard : HardwareComponent, IDifferentialSteering
    {
        const byte SetWheelVelocityCode = 0x86;
        ICommunicationManager manager;
        private WheelVelocity currentVelocity;
        private WheelVelocity previousVelocity;
        float accelerationWeight = 0.8f;
        float breakingWeight = 0.8f;

        public DifferentialSteeringBoard(ICommunicationManager communicationManager, double wheelRadius, int wheelClicks)
            : this(communicationManager, wheelRadius, wheelClicks, 1f, 1f)
        {
        }

        public DifferentialSteeringBoard(ICommunicationManager communicationManager, double wheelRadius, int wheelClicks, float accelerationWeight, float breakingWeight)
        {
            previousVelocity = new WheelVelocity();
            currentVelocity = new WheelVelocity();
            manager = communicationManager;
            CommandChecksum = from command in Observable.FromEventPattern<CommandCompletedEventArgs>(manager, "CommandCompleted")
                              where command.EventArgs.UserState == this
                              select ParseMotorActuatorResponse(command.EventArgs.Response);
            WheelRadius = wheelRadius;
            WheelClicks = wheelClicks; ; 

            this.accelerationWeight = accelerationWeight;
            this.breakingWeight = breakingWeight;
        }
        public double WheelRadius { get; private set; }
        public int WheelClicks { get; private set; }

        public IObservable<DifferentialSteeringResponse> CommandChecksum { get; private set; }

        public void UpdateWheelVelocity(WheelVelocity wheelVelocity)
        {
            currentVelocity = wheelVelocity;
        }

        DifferentialSteeringResponse ParseMotorActuatorResponse(byte[] response)
        {
            return new DifferentialSteeringResponse(currentVelocity);
        }

        public void Actuate()
        {
            var leftWeight = (Math.Abs(currentVelocity.LeftVelocity) - Math.Abs(previousVelocity.LeftVelocity) > 0) ? accelerationWeight : breakingWeight;
            if (Math.Sign(currentVelocity.LeftVelocity) != Math.Sign(previousVelocity.LeftVelocity))
                leftWeight = breakingWeight;
            if (previousVelocity.LeftVelocity == 0)
                leftWeight = accelerationWeight;
            var rightWeight = (Math.Abs(currentVelocity.RightVelocity) - Math.Abs(previousVelocity.RightVelocity) > 0) ? accelerationWeight : breakingWeight;
            if (Math.Sign(currentVelocity.RightVelocity) != Math.Sign(previousVelocity.RightVelocity))
                rightWeight = breakingWeight;
            if (previousVelocity.RightVelocity ==0)
                rightWeight = accelerationWeight;

            var left = previousVelocity.LeftVelocity + (leftWeight * (currentVelocity.LeftVelocity - previousVelocity.LeftVelocity));
            var right = previousVelocity.RightVelocity + (rightWeight * (currentVelocity.RightVelocity - previousVelocity.RightVelocity));
            previousVelocity = new WheelVelocity(left, right);
            var command = ParseUpdateWheelVelocity(previousVelocity, SetWheelVelocityCode, WheelClicks);
            manager.CommandAsync(command, 0, this);
        }

        public static byte[] ParseUpdateWheelVelocity(WheelVelocity wheelVelocity, byte setWheelVelocityCode, int wheelClicks)
        {
            int minValue = 7;
            var velocity = wheelVelocity;
            //1050 ex 588 clicks por tempo de amostragem
            var leftVelocity = -velocity.LeftVelocity;
            var rightVelocity = velocity.RightVelocity;

            if (leftVelocity != 0 && Math.Abs(leftVelocity) < minValue)
            {
                leftVelocity = (Math.Sign(leftVelocity) * minValue);
            }
            if (rightVelocity != 0 && Math.Abs(rightVelocity) < minValue)
            {
                rightVelocity = (Math.Sign(rightVelocity) * minValue);
            }
           
            //Trace.WriteLine(leftVelocity + " " + rightVelocity);
            var command = new byte[5];
            command[0] = setWheelVelocityCode;
            command[1] = (byte)((short)rightVelocity >> 8);
            command[2] = (byte)(short)rightVelocity;
            command[3] = (byte)((short)leftVelocity >> 8);
            command[4] = (byte)(short)leftVelocity;
            return command;
        }
    }
}