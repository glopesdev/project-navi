using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace ProjectNavi.Hardware
{
    public class CommunicationManagerSerial : HardwareComponent, ICommunicationManager
    {
        bool disposed;
        SerialPort serialPort;
        byte[] readBuffer;
        PendingCommand currentCommand;
        Dictionary<byte, PendingCommand> pendingCommands;

        public CommunicationManagerSerial(string portName, int baudRate)
        {
            pendingCommands = new Dictionary<byte, PendingCommand>();

            serialPort = new SerialPort();
            serialPort.PortName = portName;
            serialPort.BaudRate = baudRate;
            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Handshake = Handshake.None;
            serialPort.ReadTimeout = 0x3e8;
            serialPort.WriteTimeout = 0x3e8;
            readBuffer = new byte[serialPort.ReadBufferSize];
            serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);

            serialPort.Open();
            serialPort.DiscardInBuffer();
        }

        public event EventHandler<CommandCompletedEventArgs> CommandCompleted;

        private void OnCommandCompleted(CommandCompletedEventArgs e)
        {
            var handler = CommandCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void CommandAsync(byte[] command, int responseSize, object userState)
        {
            
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (command.Length < 1)
            {
                throw new ArgumentException("Command bytes must not be empty.");
            }

            var code = command[0];

            var response = new byte[responseSize];
            var pendingCommand = new PendingCommand(code, response, userState);
            
            lock (pendingCommands)
            {
                if (pendingCommands.Count > 0)
                {
                    throw new TimeoutException("Communication manager adding a new command before response :" + "Trying to place a request a " + code + " when there is a " + pendingCommands.First().Key + " on the response queue");
                }
                pendingCommands.Add(code, pendingCommand);
            }

            serialPort.Write(command, 0, command.Length);
        }

        public void CancelAsync(byte code)
        {
            lock (pendingCommands)
            {
                pendingCommands.Remove(code);
            }
        }

        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (serialPort.BytesToRead > 0)
            {
                if (currentCommand == null)
                {
                    var code = (byte)serialPort.ReadByte();
                    if (!pendingCommands.TryGetValue(code, out currentCommand))
                    {
                        throw new InvalidOperationException("Unexpected command response.");
                    }
                }

                var response = currentCommand.Response;
                var bytesRead = serialPort.Read(response, currentCommand.Offset, response.Length - currentCommand.Offset);
                currentCommand.Offset += bytesRead;

                if (currentCommand.Offset == response.Length)
                {
                    var userState = currentCommand.UserState;
                    byte code = currentCommand.Code;
                    lock (pendingCommands)
                    {
                        pendingCommands.Remove(currentCommand.Code);
                    }

                    currentCommand = null;
                    OnCommandCompleted(new CommandCompletedEventArgs(code, response, null, false, userState));
                }

            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    serialPort.Close();
                    disposed = true;
                }
            }
            base.Dispose(disposing);
        }
    }
}
