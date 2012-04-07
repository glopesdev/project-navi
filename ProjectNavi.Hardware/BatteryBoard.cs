using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ProjectNavi.Hardware
{
    public class BatteryBoard : HardwareComponent
    {
        const byte GetBatteryStateCode = 0x4B;

        ICommunicationManager manager;

        public BatteryBoard(ICommunicationManager communicationManager)
        { 
            manager = communicationManager;
            BatteryMeasure = (from command in Observable.FromEventPattern<CommandCompletedEventArgs>(manager, "CommandCompleted")
                             where command.EventArgs.UserState == this
                             select ParseBatteryResponse(command.EventArgs.Response));
        }

        public IObservable<double> BatteryMeasure { get; private set; }

        public void GetBatteryState()
        {
            var command = new byte[1];
            command[0] = GetBatteryStateCode;
            manager.CommandAsync(command, 2, this);
        }

        double ParseBatteryResponse(byte[] response)
        {
            double motorBattery;
            var resp = (short)((response[0] << 8) + response[1]);
            motorBattery = (double)resp / 10.0;
            return motorBattery;
        }
    }
}
