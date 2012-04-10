using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using System.IO.Ports;
using ProjectNavi.Hardware;
using SKYPE4COMLib;

namespace ProjectNavi.SkypeController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Skype skype;

        private Dispatcher _dispatcher;
        System.Threading.Timer time;
        int skypeChats = 0;
        int skypeCalls = 0;
        int callID;

        //public SerialPort serialPort;
        public MagabotState Magabot {get; set;}

        bool isAssistedNavigation = false;

        public MainWindow()
        {
            InitializeComponent();

            _dispatcher = this.Dispatcher;

            listEvents.Items.Clear();

            //availabe COM ports
            SerialPort tmp;
            foreach (string str in SerialPort.GetPortNames())
            {
                tmp = new SerialPort(str);
                if (tmp.IsOpen == false)
                    comboBoxSerialPort.Items.Add(str);
            }

         
            Properties.Settings.Default.Reload();
        }


        #region Sign In
        private void buttonSignIn_Click(object sender, RoutedEventArgs e)
        {
            skype = new Skype();
            skype.MessageStatus += new _ISkypeEvents_MessageStatusEventHandler(skype_MessageStatus);
            skype.CallStatus += new _ISkypeEvents_CallStatusEventHandler(skype_CallStatus);
            skype.CallDtmfReceived += new _ISkypeEvents_CallDtmfReceivedEventHandler(skype_CallDtmfReceived);
            TimerCallback tcb = this.CheckStatus;
            AutoResetEvent ar = new AutoResetEvent(true);
            time = new System.Threading.Timer(tcb, ar, 250, 250);

            buttonSignIn.IsEnabled = false;

            expanderChat.IsExpanded = true;
            expanderSignIn.IsExpanded = false;
        }

        #region Skype
        private void CheckStatus(Object stateInfo)
        {
            try
            {
                skypeChats = skype.ActiveChats.Count;
            }
            catch (InvalidCastException e)
            {
            }
        }
        #endregion

        public void skype_MessageStatus(ChatMessage msg, TChatMessageStatus Status)
        {
            if (Status == TChatMessageStatus.cmsReceived)
            {
                // ignore empty messages (events)
                if (msg.Body == null)
                    return;

                _dispatcher.BeginInvoke((Action)(() =>
                {
                    listEvents.Items.Add(String.Format("Message from {0}: {1}", msg.Sender.Handle, msg.Body));
                    listEvents.SelectedIndex = listEvents.Items.Count - 1;
                }));

                _dispatcher.BeginInvoke((Action)(() =>
                {
                    if (msg.Body == "Available?")
                    {
                        if (skypeCalls == 0)
                        {
                            msg.Chat.SendMessage("Yes");
                        }
                        else
                        {
                            msg.Chat.SendMessage("No");
                        }
                    }
                    else
                    {
                        int sameItemNumber = 0;
                        int i = 0;
                        while (i < comboBoxSelectedUser.Items.Count)
                        {
                            if (msg.Sender.Handle == comboBoxSelectedUser.Items.GetItemAt(i).ToString())
                                sameItemNumber++;

                            i++;
                        }

                        if (sameItemNumber == 0) // New chat
                        {
                            comboBoxSelectedUser.Items.Add(msg.Sender.Handle);

                            if (checkBoxSendWelcomeMessage.IsChecked == true)
                            {
                                msg.Chat.SendMessage(Properties.Settings.Default.welcomeMessage);
                            }
                        }

                        if (comboBoxSelectedUser.SelectedItem == null) // First chat
                            comboBoxSelectedUser.SelectedItem = msg.Sender.Handle;

                        if (msg.Sender.Handle == comboBoxSelectedUser.SelectedItem.ToString() && msg.Sender.Handle != "") // Message from the selected user
                        {
                        //    if (serialPort.IsOpen == true)
                        //    {
                        //        if (msg.Body.Contains('2'))
                        //            isAssistedNavigation = false;
                        //        else if (msg.Body.Contains('1'))
                        //            isAssistedNavigation = true;

                        //        if (!msg.Body.Contains('1') && !msg.Body.Contains('2') && isAssistedNavigation == false)
                        //        {
                        //            serialPort.WriteLine("1");
                        //            textBoxSerial.Text += string.Format("S: {0} \r\n", '1');
                        //            isAssistedNavigation = true;
                        //        }


                        //        serialPort.Write(msg.Body.ToCharArray(), 0, 1);
                        //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg.Body);
                        //    }
                        //    else
                        //    {
                        //        if (!msg.Body.Contains('1') && !msg.Body.Contains('2') && isAssistedNavigation == false)
                        //        {
                        //            if (serialPort.IsOpen == true) serialPort.WriteLine("1");
                        //            textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", '1');
                        //            //isAssistedNavigation = true;
                        //        }

                        //        if (checkBoxSendFailedToSendMessage.IsChecked == true)
                        //        {
                        //            textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg.Body);
                        //            msg.Chat.SendMessage(Properties.Settings.Default.failedToSendMessage);
                        //        }
                        //    }
                        
                        }
                        else // Message from other User
                        {
                            if (checkBoxSendWaitMessage.IsChecked == true)
                            {
                                msg.Chat.SendMessage(Properties.Settings.Default.waitMessage);
                            }
                        }
                    }
                }));
            }
        }

        public void skype_CallStatus(Call call, TCallStatus status)
        {
            try
            {
                if (call.VideoSendStatus == TCallVideoSendStatus.vssAvailable)
                {
                    call.StartVideoSend();
                }
            }
            catch { }

            if (status == TCallStatus.clsInProgress)
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    if (comboBoxSelectedUser.SelectedItem == null) // First chat
                        comboBoxSelectedUser.SelectedItem = call.PartnerHandle;

                }));
            }

            if (status == TCallStatus.clsRinging)
            {
                if (skypeCalls == 0)
                {
                    call.Answer();

                    _dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (comboBoxSelectedUser.SelectedItem == null) // First chat
                            comboBoxSelectedUser.SelectedItem = call.PartnerHandle;

                    }));
                    callID = call.Id;
                    skypeCalls++;
                }
                else if (skypeCalls == 1 && callID != call.Id)
                {
                    try
                    {
                        call.Finish();
                    }
                    catch (InvalidCastException e)
                    {
                    }
                }
            }

            if (status == TCallStatus.clsFinished && call.Id == callID)
            {
                skypeCalls--;
                callID = 0;
            }
        }

        public void skype_CallDtmfReceived(Call call, string code)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                // Always use try/catch with ANY Skype calls.
                try
                {
                    String msg = "";
                    if (code == "2")
                        msg = "w";
                    else if (code == "4")
                        msg = "a";
                    else if (code == "6")
                        msg = "d";
                    else if (code == "8")
                        msg = "s";
                    else if (code == "5")
                        msg = "p";
                    else if (code == "*")
                        msg = "1";
                    else if (code == "#")
                        msg = "2";

                    // Write Call DTMF Received to Window
                    listEvents.Items.Add(String.Format("Code DTMF from {0}: {1}", call.PartnerHandle, code));
                    listEvents.SelectedIndex = listEvents.Items.Count - 1;

                    //if (serialPort.IsOpen == true)
                    //{
                    //    if (msg == "2")
                    //        isAssistedNavigation = false;
                    //    else if (msg == "1")
                    //        isAssistedNavigation = true;

                    //    if (msg != "1" && msg != "2" && isAssistedNavigation == false)
                    //    {
                    //        serialPort.WriteLine("1");
                    //        textBoxSerial.Text += string.Format("S: {0} \r\n", "1");
                    //        isAssistedNavigation = true;
                    //    }

                    //    serialPort.Write(msg);
                    //    textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                    //}
                    //else
                    //{
                    //    if (msg != "1" && msg != "2" && isAssistedNavigation == false)
                    //    {
                    //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", "1");
                    //    }

                    //    if (checkBoxSendFailedToSendMessage.IsChecked == true)
                    //    {
                    //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                    //        skype.SendMessage(comboBoxSelectedUser.Text, Properties.Settings.Default.failedToSendMessage);
                    //    }
                    //}
                }
                catch (Exception e)
                {
                }
            }));
        }
        #endregion


        #region Serial Port
        private void comboBoxSerialPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //_dispatcher.BeginInvoke((Action)(() =>
            //{
            //    serialPort.PortName = comboBoxSerialPort.SelectedItem.ToString();

            //    //open serial port
            //    serialPort.Open();
            //    comboBoxSerialPort.IsEnabled = false;
            //    buttonCloseSerialPort.IsEnabled = true;
            //    buttonFindSerialPort.IsEnabled = false;
            //    buttonOpenSerialPort.IsEnabled = false;

            //    serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);

            //    expanderControls.IsExpanded = true;
            //    expanderSerialPort.IsExpanded = false;
            //}));
        }

        private void buttonFindSerialPort_Click(object sender, RoutedEventArgs e)
        {
            ////availabe COM ports
            //SerialPort tmp;
            //foreach (string str in SerialPort.GetPortNames())
            //{
            //    tmp = new SerialPort(str);

            //    int sameItemNumber = 0;
            //    int i = 0;
            //    while (i < comboBoxSerialPort.Items.Count)
            //    {
            //        if (str == comboBoxSerialPort.Items.GetItemAt(i).ToString())
            //            sameItemNumber++;

            //        i++;
            //    }

            //    if (sameItemNumber == 0)
            //    {
            //        if (tmp.IsOpen == false)
            //            comboBoxSerialPort.Items.Add(str);
            //    }
            //}
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //// blocks until TERM_CHAR is received
            //string msg = serialPort.ReadExisting();

            //if (msg[0] == 'i')
            //{
            //    _dispatcher.BeginInvoke((Action)(() =>
            //    {
            //        if (comboBoxSelectedUser.SelectedItem != null && checkBoxSendHoleMessage.IsChecked == true)
            //        {
            //            skype.SendMessage(comboBoxSelectedUser.SelectedItem.ToString(), Properties.Settings.Default.holeMessage);
            //        }

            //        textBoxSerial.Text += string.Format("R: {0}", msg);
            //        textBoxSerial.ScrollToEnd();
            //    }));
            //}
            //else if (msg[0] == 'b')
            //{
            //    _dispatcher.BeginInvoke((Action)(() =>
            //    {
            //        if (comboBoxSelectedUser.SelectedItem != null && checkBoxSendBumperMessage.IsChecked == true)
            //        {
            //            skype.SendMessage(comboBoxSelectedUser.SelectedItem.ToString(), Properties.Settings.Default.bumperMessage);
            //        }

            //        textBoxSerial.Text += string.Format("R: {0}", msg);
            //        textBoxSerial.ScrollToEnd();
            //    }));
            //}
        }

        private void buttonCloseSerialPort_Click(object sender, RoutedEventArgs e)
        {
            //serialPort.Close();

            //_dispatcher.BeginInvoke((Action)(() =>
            //{
            //    serialPort.Close();

            //    buttonCloseSerialPort.IsEnabled = false;
            //    buttonFindSerialPort.IsEnabled = true;
            //    buttonOpenSerialPort.IsEnabled = true;
            //    comboBoxSerialPort.IsEnabled = true;
            //    expanderControls.IsExpanded = false;
            //}));


        }

        private void buttonOpenSerialPort_Click(object sender, RoutedEventArgs e)
        {
            //_dispatcher.BeginInvoke((Action)(() =>
            //{
            //    serialPort.PortName = comboBoxSerialPort.SelectedItem.ToString();

            //    //open serial port
            //    serialPort.Open();

            //    comboBoxSerialPort.IsEnabled = false;
            //    buttonCloseSerialPort.IsEnabled = true;
            //    buttonOpenSerialPort.IsEnabled = false;
            //    buttonFindSerialPort.IsEnabled = false;

            //    serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);

            //    expanderControls.IsExpanded = true;
            //    expanderSerialPort.IsExpanded = false;
            //}));
        }

        private void textBoxSerial_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxSerial.ScrollToEnd();
        }

        private void listEvents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listEvents.Items.Count > 1)
            {
                //listEvents.ScrollIntoView(listEvents.Items.GetItemAt(0));
            }
        }

        private void checkBoxAutoMessages_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void checkBoxAutoMessages_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void comboBoxSelectedUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                if (checkBoxSendControllerMessage.IsChecked == true)
                {
                    skype.SendMessage(comboBoxSelectedUser.Text, Properties.Settings.Default.controllerMessage);
                }

                buttonUncheckSelectedUser.IsEnabled = true;
            }));
        }

        private void buttonUncheckSelectedUser_Click(object sender, RoutedEventArgs e)
        {
            buttonUncheckSelectedUser.IsEnabled = false;
            comboBoxSelectedUser.SelectedIndex = -1;
        }
        #endregion


        #region Send Message
        private void buttonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            // Send a message
            skype.SendMessage(comboBoxOnlineFriends.SelectedItem.ToString(), textBoxMessage.Text);

            textBoxMessage.Text = "";
        }

        private void comboBoxOnlineFriends_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buttonSendMessage.IsEnabled = true;
        }
        #endregion


        #region Controls
        private void buttonForward_Click(object sender, RoutedEventArgs e)
        {
            if (isAssistedNavigation == false)
            {
                //_dispatcher.BeginInvoke((Action)(() =>
                //{
                //    String msg = "1";
                //    if (serialPort.IsOpen)
                //    {
                //        serialPort.WriteLine(msg);
                //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                //    }
                //    else
                //    {
                //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                //    }
                //}));

                isAssistedNavigation = true;
            }

            //_dispatcher.BeginInvoke((Action)(() =>
            //{
            //    String msg = "w";
            //    if (serialPort.IsOpen)
            //    {
            //        serialPort.WriteLine(msg);
            //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
            //    }
            //    else
            //    {
            //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
            //    }
            //}));
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            if (isAssistedNavigation == false)
            {
                //_dispatcher.BeginInvoke((Action)(() =>
                //{
                //    String msg = "1";
                //    if (serialPort.IsOpen)
                //    {
                //        serialPort.WriteLine(msg);
                //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                //    }
                //    else
                //    {
                //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                //    }
                //}));

                isAssistedNavigation = true;
            }

            //_dispatcher.BeginInvoke((Action)(() =>
            //{
            //    String msg = "p";
            //    if (serialPort.IsOpen)
            //    {
            //        serialPort.WriteLine(msg);
            //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
            //    }
            //    else
            //    {
            //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
            //    }
            //}));
        }

        private void buttonBackward_Click(object sender, RoutedEventArgs e)
        {
            if (isAssistedNavigation == false)
            {
                //_dispatcher.BeginInvoke((Action)(() =>
                //{
                //    String msg = "1";
                //    if (serialPort.IsOpen)
                //    {
                //        serialPort.WriteLine(msg);
                //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                //    }
                //    else
                //    {
                //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                //    }
                //}));

                isAssistedNavigation = true;
            }

            //_dispatcher.BeginInvoke((Action)(() =>
            //{
            //    String msg = "s";
            //    if (serialPort.IsOpen)
            //    {
            //        serialPort.WriteLine(msg);
            //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
            //    }
            //    else
            //    {
            //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
            //    }
            //}));

        }

        private void buttonLeft_Click(object sender, RoutedEventArgs e)
        {
            if (isAssistedNavigation == false)
            {
                //_dispatcher.BeginInvoke((Action)(() =>
                //{
                //    String msg = "1";
                //    if (serialPort.IsOpen)
                //    {
                //        serialPort.WriteLine(msg);
                //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                //    }
                //    else
                //    {
                //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                //    }
                //}));

                isAssistedNavigation = true;
            }

            //_dispatcher.BeginInvoke((Action)(() =>
            //{
            //    String msg = "a";
            //    if (serialPort.IsOpen)
            //    {
            //        serialPort.WriteLine(msg);
            //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
            //    }
            //    else
            //    {
            //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
            //    }
            //}));
        }

        private void buttonRight_Click(object sender, RoutedEventArgs e)
        {
            if (isAssistedNavigation == false)
            {
                //_dispatcher.BeginInvoke((Action)(() =>
                //{
                //    String msg = "1";
                //    if (serialPort.IsOpen)
                //    {
                //        serialPort.WriteLine(msg);
                //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
                //    }
                //    else
                //    {
                //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
                //    }
                //}));

                isAssistedNavigation = true;
            }

            //_dispatcher.BeginInvoke((Action)(() =>
            //{
            //    String msg = "d";
            //    if (serialPort.IsOpen)
            //    {
            //        serialPort.WriteLine(msg);
            //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
            //    }
            //    else
            //    {
            //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
            //    }
            //}));
        }

        private void buttonAutonomousNavigation_Click(object sender, RoutedEventArgs e)
        {
            isAssistedNavigation = false;

            //_dispatcher.BeginInvoke((Action)(() =>
            //{
            //    String msg = "2";
            //    if (serialPort.IsOpen)
            //    {
            //        serialPort.WriteLine(msg);
            //        textBoxSerial.Text += string.Format("S: {0} \r\n", msg);
            //    }
            //    else
            //    {
            //        textBoxSerial.Text += string.Format("Failed to Send: {0} \r\n", msg);
            //    }
            //}));
        }
        #endregion


        #region Expander Events
        private void expanderGoogleChat_Expanded(object sender, RoutedEventArgs e)
        {
            manageCollapses();
        }

        private void expanderControls_Expanded(object sender, RoutedEventArgs e)
        {
            manageCollapses();
        }

        private void expanderGoogleChat_Collapsed(object sender, RoutedEventArgs e)
        {
            manageCollapses();
        }

        private void expanderControls_Collapsed(object sender, RoutedEventArgs e)
        {
            manageCollapses();
        }

        private void expanderSignIn_Expanded(object sender, RoutedEventArgs e)
        {
            //expanderGoogleChat.Margin.Top = expanderSignIn.Margin.Top + expanderSignIn.Height;
            try
            {
                Canvas.SetTop(expanderChat, expanderSignIn.Margin.Top + expanderSignIn.Height);
            }
            catch
            {
            }

            manageCollapses();
        }

        private void expanderSignIn_Collapsed(object sender, RoutedEventArgs e)
        {
            try
            {
                Canvas.SetTop(expanderChat, expanderSignIn.Margin.Top + 50);
            }
            catch
            {
            }

            manageCollapses();
        }

        private void expanderSerialPort_Expanded(object sender, RoutedEventArgs e)
        {
            //expanderGoogleChat.Margin.Top = expanderSignIn.Margin.Top + expanderSignIn.Height;
            try
            {
                Canvas.SetTop(expanderControls, expanderSerialPort.Margin.Top + expanderSerialPort.Height);
            }
            catch
            {
            }

            manageCollapses();
        }

        private void expanderSerialPort_Collapsed(object sender, RoutedEventArgs e)
        {
            try
            {
                Canvas.SetTop(expanderControls, expanderSerialPort.Margin.Top + 50);
            }
            catch
            {
            }

            manageCollapses();
        }

        private void manageCollapses()
        {
            try
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    int leftHeight = 64;
                    if (expanderSerialPort.IsExpanded == true)
                        leftHeight += (int)expanderSerialPort.Height;
                    else
                        leftHeight += 32;

                    if (expanderControls.IsExpanded == true)
                        leftHeight += (int)expanderControls.Height;
                    else
                        leftHeight += 32;

                    int rightHeight = 64;
                    if (expanderSignIn.IsExpanded == true)
                        rightHeight += (int)expanderSignIn.Height;
                    else
                        rightHeight += 32;

                    if (expanderChat.IsExpanded == true)
                        rightHeight += (int)expanderChat.Height;
                    else
                        rightHeight += 32;

                    if (leftHeight > rightHeight)
                    {
                        skypeRobotController.Height = leftHeight;
                        MainCanvas.Height = skypeRobotController.Height - 40;
                    }
                    else
                    {
                        skypeRobotController.Height = rightHeight;
                        MainCanvas.Height = skypeRobotController.Height - 40;
                    }
                }));
            }
            catch
            {
            }
        }
        #endregion
    }
}
