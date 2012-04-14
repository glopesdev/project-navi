using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;

namespace ProjectNavi.Hardware
{
    public class GroundSensorBoard : HardwareComponent
    {
        const byte GetGroundSensorStateCode = 0x73;

        ICommunicationManager manager;

        public GroundSensorBoard(ICommunicationManager communicationManager)
        {
            manager = communicationManager;
            GroundSensorsMeasure = (from command in Observable.FromEventPattern<CommandCompletedEventArgs>(manager, "CommandCompleted")
                                    where command.EventArgs.UserState == this
                                    select ParseGroundSensorsResponse(command.EventArgs.Response));
        }

        public IObservable<GroundSensorsMeasurement> GroundSensorsMeasure { get; private set; }

        public void GetGroundSensorState()
        {
            var command = new byte[1];
            command[0] = GetGroundSensorStateCode;
            manager.CommandAsync(command, 6, this);
        }

        GroundSensorsMeasurement ParseGroundSensorsResponse(byte[] response)
        {
            var sensorLeft = (short)((response[0] << 8) + response[1]);
            var sensorMiddle = (short)((response[2] << 8) + response[3]);
            var sensorRight = (short)((response[4] << 8) + response[5]);


            return new GroundSensorsMeasurement(sensorLeft, sensorMiddle, sensorRight);
        }
    }

    public struct GroundSensorsMeasurement
    {
        short sensorLeft;
        short sensorMiddle;
        short sensorRight;

        public GroundSensorsMeasurement(short sensorLeft, short sensorMiddle, short sensorRight)
        {
            this.sensorLeft = sensorLeft;
            this.sensorMiddle = sensorMiddle;
            this.sensorRight = sensorRight;
        }

        public short SensorLeft
        {
            get { return sensorLeft; }
        }
        public short SensorMiddle
        {
            get { return sensorMiddle; }
        }
        public short SensorRight
        {
            get { return sensorRight; }
        }
    
    }
}
