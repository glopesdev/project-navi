using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Diagnostics;

namespace ProjectNavi.Hardware
{
    public class OdometryBoard : HardwareComponent, IOdometry
    {
        const double TwoPi = 2 * Math.PI;
        const byte GetOdometryCode = 0x74;
        ICommunicationManager manager;

        public OdometryBoard(ICommunicationManager communicationManager, double wheelClicks, double wheelRadius, double wheelDistance)
        {
            manager = communicationManager;
            WheelClicks = wheelClicks;
            WheelDistance = wheelDistance;
            WheelRadius = wheelRadius;
            var tmp = (from command in Observable.FromEventPattern<CommandCompletedEventArgs>(manager, "CommandCompleted")
                        where command.EventArgs.UserState == this
                        select ParseOdometryResponse(command.EventArgs.Response,WheelClicks,WheelRadius,WheelDistance))
                        .Select((m, i) => i < 1 ? new OdometryMeasurement() : m).Publish();
            tmp.Connect();
            Odometry = tmp;
        }

        public double WheelClicks { get; set; }

        public double WheelRadius { get; set; }

        public double WheelDistance { get; set; }

        public IObservable<OdometryMeasurement> Odometry { get; private set; }

        public void UpdateOdometryCommand()
        {
            var command = new byte[1];
            command[0] = GetOdometryCode;
            
            manager.CommandAsync(command, 4, this);
        }

        public static OdometryMeasurement ParseOdometryResponse(byte[] response, double wheelClicks, double wheelRadius, double wheelDistance)
        {
            var rightClicks = (short)((response[0] << 8) + response[1]);
            var leftClicks = (short)((response[2] << 8) + response[3]);

           // Trace.WriteLine(leftClicks + " " + rightClicks);

            var distanceLeft = leftClicks * TwoPi * wheelRadius / wheelClicks;
            var distanceRight = -rightClicks * TwoPi * wheelRadius / wheelClicks;

            var linearDisplacement = (distanceLeft + distanceRight) / 2;
            var angularDisplacement = (distanceRight - distanceLeft) / wheelDistance;

            return new OdometryMeasurement(linearDisplacement, angularDisplacement);
        }
    }
}