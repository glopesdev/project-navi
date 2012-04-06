using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.IO.Ports;
using System.Threading;

namespace ProjectNavi.Hardware
{
    public class DifferentialSteeringBoard : HardwareComponent, IDifferentialSteering
    {
        const byte SetWheelVelocityCode = 0x56;
        ICommunicationManager manager;
        private WheelVelocity currentVelocity;
        private WheelVelocity previousVelocity;
        float accelerationWeight = 1f;
        float breakingWeight = 0.1f;

        public DifferentialSteeringBoard(ICommunicationManager communicationManager, double wheelRadius, float accelerationWeight, float breakingWeight)
        {
            previousVelocity = new WheelVelocity();
            currentVelocity = new WheelVelocity();
            manager = communicationManager;
            CommandChecksum = from command in Observable.FromEventPattern<CommandCompletedEventArgs>(manager, "CommandCompleted")
                              where command.EventArgs.UserState == this
                              select ParseMotorActuatorResponse(command.EventArgs.Response);
            WheelRadius = wheelRadius;

            this.accelerationWeight = accelerationWeight;
            this.breakingWeight = breakingWeight;
        }
        public double WheelRadius { get; private set; }

        public IObservable<IDifferentialSteeringResponse> CommandChecksum { get; private set; }

        public void UpdateWheelVelocity(WheelVelocity wheelVelocity)
        {
            currentVelocity = wheelVelocity;
        }

        IDifferentialSteeringResponse ParseMotorActuatorResponse(byte[] response)
        {
            return new IDifferentialSteeringResponse(currentVelocity, (short)((response[0] << 8) + response[1]));
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
            var command = ParseUpdateWheelVelocity(previousVelocity, SetWheelVelocityCode);
            manager.CommandAsync(command, 3, this);
        }

        public static byte[] ParseUpdateWheelVelocity(WheelVelocity wheelVelocity, byte setWheelVelocityCode)
        {
            int minValue = 30;
            var velocity = wheelVelocity;
            //1050 ex 588 clicks por tempo de amostragem
            short leftVelocity = (short)(-velocity.LeftVelocity * 1050 / (2 * Math.PI));
            short rightVelocity = (short)(velocity.RightVelocity * 1050 / (2 * Math.PI));

            if (leftVelocity != 0 && Math.Abs(leftVelocity) < minValue)
            {
                leftVelocity = (short)(Math.Sign((int)leftVelocity) * minValue);
            }
            if (rightVelocity != 0 && Math.Abs(rightVelocity) < minValue)
            {
                rightVelocity = (short)(Math.Sign((int)rightVelocity) * minValue);
            }
            //Trace.WriteLine(leftVelocity + " " + rightVelocity);
            var command = new byte[5];
            command[0] = setWheelVelocityCode;
            command[1] = (byte)(rightVelocity >> 8);
            command[2] = (byte)rightVelocity;
            command[3] = (byte)(leftVelocity >> 8);
            command[4] = (byte)leftVelocity;
            return command;
        }
    }
}