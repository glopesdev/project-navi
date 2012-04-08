using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;

namespace ProjectNavi.Hardware
{
    public class LedBoard : HardwareComponent
    {
        const byte GetLedBoardStateCode = 0x76;

        ICommunicationManager manager;

        byte red, green, blue;
        public LedBoard(ICommunicationManager communicationManager)
        {
            manager = communicationManager;
            LedBoardMeasure = (from command in Observable.FromEventPattern<CommandCompletedEventArgs>(manager, "CommandCompleted")
                                    where command.EventArgs.UserState == this
                               select ParseLedBoardResponse(command.EventArgs.Response));
        }

        public IObservable<Unit> LedBoardMeasure { get; private set; }

        public void SetLedBoardState(byte red, byte green, byte blue)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
        }
        public void Actuate()
        {
            var command = new byte[4];
            command[0] = GetLedBoardStateCode;
            command[1] = red;
            command[2] = green;
            command[3] = blue;
            manager.CommandAsync(command, 0, this);
        }


        Unit ParseLedBoardResponse(byte[] response)
        {
            return Unit.Default;
        }
    }
}
