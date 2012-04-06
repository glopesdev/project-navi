using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Net.Sockets;
using System.Net;

namespace ProjectNavi.Hardware
{
    public class CommunicationManagerTcp : HardwareComponent, IUbiCommunicationManager
    {

        private const int MAXMESSAGESIZE = 1024;
        private byte[] readBuffer = new byte[MAXMESSAGESIZE];
        private Socket communicationSocket;
        UbisensePendingCommand currentCommand;
        Dictionary<int, UbisensePendingCommand> pendingCommands;

        public CommunicationManagerTcp(string hostName, int port)
        {
            pendingCommands = new Dictionary<int, UbisensePendingCommand>();
            IPAddress[] hostAddresses = Dns.GetHostAddresses(hostName);
            if (hostAddresses.Length == 0)
            {
                throw new Exception("IP address not found for host " + hostName);
            }
            var endPoint = new IPEndPoint(hostAddresses[0], port);
            this.startConnection(endPoint);
        }
       
        public event EventHandler<UbisenseCommandCompletedEventArgs> CommandCompleted;

        #region TCP
        private void ConnectCallback(IAsyncResult result)
        {
            var sock = (Socket)result.AsyncState;
            if (result.IsCompleted)
            {
                try
                {
                    //sock.EndConnect( ar );
                    if (sock.Connected)
                        sock.BeginReceive(readBuffer, 0, readBuffer.Length, SocketFlags.None, OnRecievedData, sock);
                    else
                        Console.WriteLine("Unable to connect to remote machine", "Connect Failed!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unusual error during Connect!" + ex.Message);
                }
            }
        }

        /// <summary>
        /// Get the new data and send it out to all other connections. 
        /// Note: If not data was recieved the connection has probably 
        /// died.
        /// </summary>
        /// <param name="ar"></param>
        int bytesRecv = 0;
        private void OnRecievedData(IAsyncResult ar)
        {
            // Socket was the passed in object
            Socket sock = (Socket)ar.AsyncState;
            // Check if we got any data
           
            bytesRecv  += sock.EndReceive(ar);
            if (currentCommand == null && bytesRecv < 5)
            {
                sock.BeginReceive(readBuffer, bytesRecv, readBuffer.Length - bytesRecv, SocketFlags.None, OnRecievedData, sock);
                return;
            }
            else
            {
                if (currentCommand == null)
                {
                    var tagId = BitConverter.ToInt32(readBuffer, 1);
                    if (!pendingCommands.TryGetValue(tagId, out currentCommand))
                    {
                        throw new InvalidOperationException("Unexpected command response.");
                    }
                }
                if (bytesRecv - 1 >= currentCommand.Response.Length)
                {
                    var response = currentCommand.Response;
                    var userState = currentCommand.UserState;
                    for (int count = 0; count < currentCommand.Response.Length; count++)
                    {
                        currentCommand.Response[count] = readBuffer[count + 1];
                    }
                    var tagId = BitConverter.ToInt32(readBuffer, 1);      
                    lock (pendingCommands)
                    {
                        pendingCommands.Remove(tagId);
                    }
                    currentCommand = null;
                    OnCommandCompleted(new UbisenseCommandCompletedEventArgs(tagId, response, null, false, userState));
                    bytesRecv = 0;

                }
            }
            sock.BeginReceive(readBuffer, bytesRecv, readBuffer.Length-bytesRecv, SocketFlags.None, OnRecievedData, sock);
        }


     
        private void startConnection(IPEndPoint endPoint)
        {
            var callBack = new AsyncCallback(ConnectCallback);
            communicationSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            communicationSocket.BeginConnect(endPoint, callBack, communicationSocket);
           // communicationSocket
        }

        private bool isConnected()
        {
            return ((communicationSocket != null) && communicationSocket.Connected);
        }
        #endregion
        private void OnCommandCompleted(UbisenseCommandCompletedEventArgs e)
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
            var tagId = BitConverter.ToInt32(command,1);
            var response = new byte[responseSize];
            var pendingCommand = new UbisensePendingCommand(tagId, response, userState);
            lock (pendingCommands)
            {
                pendingCommands.Add(tagId, pendingCommand);
            }

            communicationSocket.Send(command, 0, command.Length,SocketFlags.None);
        }

        public void CancelAsync(byte code)
        {
            lock (pendingCommands)
            {
                pendingCommands.Remove(code);
            }
        }

        
    }
}
