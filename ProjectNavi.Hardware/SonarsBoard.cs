using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;

namespace ProjectNavi.Hardware
{
    public class SonarsBoard : HardwareComponent
    {
        const byte GetSonarsStateCode = 0x83;

        ICommunicationManager manager;


        public SonarsBoard(ICommunicationManager communicationManager)
        {
            manager = communicationManager;
            SonarsBoardMeasure = (from command in Observable.FromEventPattern<CommandCompletedEventArgs>(manager, "CommandCompleted")
                                    where command.EventArgs.UserState == this
                                    select ParseSonarsBoardResponse(command.EventArgs.Response));
        }

        public IObservable<short[]> SonarsBoardMeasure { get; private set; }

        public void GetSonarsBoardState()
        {
            var command = new byte[1];
            command[0] = GetSonarsStateCode;
            manager.CommandAsync(command, 10, this);
        }

        short[] ParseSonarsBoardResponse(byte[] response)
        {
            var result = new short[5];
            result[0] = (short)((response[0] << 8) + response[1]);
            result[1] = (short)((response[2] << 8) + response[3]);
            result[2] = (short)((response[4] << 8) + response[5]);
            result[3] = (short)((response[6] << 8) + response[7]);
            result[4] = (short)((response[8] << 8) + response[9]);
            return result;
        }
    }

   
}
