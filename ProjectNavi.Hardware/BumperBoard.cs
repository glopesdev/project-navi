using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;

namespace ProjectNavi.Hardware
{
    public class BumperBoard : HardwareComponent
    {
        const byte GetBumperStateCode = 0x66;

        ICommunicationManager manager;

        public BumperBoard(ICommunicationManager communicationManager)
        {
            manager = communicationManager;
            BumpersMeasure = (from command in Observable.FromEventPattern<CommandCompletedEventArgs>(manager, "CommandCompleted")
                            where command.EventArgs.UserState == this
                            select ParseBumpersResponse(command.EventArgs.Response));
        }

        public IObservable<BumpersMeasurement> BumpersMeasure { get; private set; }

        public void GetBumperState()
        {
            var command = new byte[1];
            command[0] = GetBumperStateCode;
            manager.CommandAsync(command, 2, this);
        }

        BumpersMeasurement ParseBumpersResponse(byte[] response)
        {
            bool bumperLeft;
            bool bumperRight;

            bumperLeft = (bool)(response[0] != 0);
            bumperRight = (bool)(response[1] != 0);
           
            return new BumpersMeasurement(bumperLeft, bumperRight);
        }
    }

    public struct BumpersMeasurement
    {
        bool bumperLeft;
        bool bumperRight;

        public BumpersMeasurement(bool bumperLeft, bool bumperRight)
        {
            this.bumperLeft = bumperLeft;
            this.bumperRight = bumperLeft;
        }

        public bool BumperLeft
        {
            get { return bumperLeft; }
        }

        public bool BumperRight
        {
            get { return bumperRight; }
        }
    }
}
