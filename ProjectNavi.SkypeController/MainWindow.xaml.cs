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
using ProjectNavi.Bonsai.Kinect;

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
        System.Windows.Threading.DispatcherTimer frameTimer;
        int skypeChats = 0;
        int skypeCalls = 0;

        //public SerialPort serialPort;
        public MagabotState Magabot {get; set;}

        int maxIRValue;
        
        int bumpMsgTime;
        int holeMsgTime;
        bool safetyBump, safetyGround;


        public MainWindow(MagabotState Magabot)
        {
            this.Magabot = Magabot;
            InitializeComponent();

            _dispatcher = this.Dispatcher;

            listEvents.Items.Clear();

            skype = new Skype();
            skype.MessageStatus += new _ISkypeEvents_MessageStatusEventHandler(skype_MessageStatus);
            skype.CallStatus += new _ISkypeEvents_CallStatusEventHandler(skype_CallStatus);
            
            TimerCallback tcb = this.CheckStatus;
            AutoResetEvent ar = new AutoResetEvent(true);
            time = new System.Threading.Timer(tcb, ar, 250, 250);
            
            frameTimer = new System.Windows.Threading.DispatcherTimer();
            frameTimer.Tick += Update;
            frameTimer.Interval = TimeSpan.FromSeconds(1.0 / 60.0);
            frameTimer.Start();

            Properties.Settings.Default.Reload();


            bumpMsgTime = 1000;
            holeMsgTime = 1000;

            Magabot.Stop();
            Magabot.Navigation = MagabotState.NavigationMode.Assisted;
            Magabot.SafetyBump.Subscribe(m =>
            {
                safetyBump = m;
                Magabot.Leds.SetLedBoardState(255, 0, 0);
                if (m)
                    Magabot.Leds.SetLedBoardState(255, 0, 0);
                else
                    Magabot.Leds.SetLedBoardState(0, 0, 255);
            });
            Magabot.SafetyGround.Subscribe(m =>
            {
                safetyGround = m;
                if (m)
                    Magabot.Leds.SetLedBoardState(255, 0, 0);
                else
                    Magabot.Leds.SetLedBoardState(0, 0, 255);
            });
        }

        public void OnKinectFrame(KinectFrame kinectFrame)
        {
            //TODO: Kinect skeleton code here: kinectFrame.SkeletonData (...)

            //kinectFrame.SkeletonData
        }

        private void CheckStatus(Object stateInfo)
        {
            try
            {
                skypeChats = skype.ActiveChats.Count;
            }
            catch (InvalidCastException e)
            {

            }

            try
            {
                skypeCalls = 0;

                _dispatcher.BeginInvoke((Action)(() =>
                {
                    if (skype.ActiveCalls.Count > 0)
                    {
                        foreach (Call call in skype.ActiveCalls)
                        {
                            if (call.Status == TCallStatus.clsInProgress)
                            {
                                _dispatcher.BeginInvoke((Action)(() =>
                                {
                                    if (!comboBoxSelectedUser.Items.Cast<string>().Contains(call.PartnerHandle)) // New user
                                    {
                                        comboBoxSelectedUser.Items.Add(call.PartnerHandle);
                                    }
                                
                                    comboBoxSelectedUser.SelectedItem = call.PartnerHandle.ToString();
                                }));

                                skypeCalls = 1;

                                if (call.VideoSendStatus == TCallVideoSendStatus.vssAvailable)
                                {
                                    call.StartVideoSend();
                                }
                            }
                        }
                    }
                    else
                    {
                        //buttonUncheckSelectedUser.IsEnabled = false;
                        //comboBoxSelectedUser.SelectedIndex = -1;
                    }
                }));
            }
            catch
            {
            }
            
        }

        private void Update(object sender, EventArgs e)
        {
            bumpMsgTime++;
            holeMsgTime++;
            backUpTime++;

            if (Magabot.BumperSensorState.BumperLeft || Magabot.BumperSensorState.BumperRight)
            {
                //SetDirection('s', "Safety");
                

                if (bumpMsgTime > 25)
                {
                    _dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (comboBoxSelectedUser.SelectedItem != null && checkBoxSendBumperMessage.IsChecked == true)
                        {
                            skype.SendMessage(comboBoxSelectedUser.SelectedItem.ToString(), Properties.Settings.Default.bumperMessage);
                        }
                    }));

                    bumpMsgTime = 0;
                }
            }
            if (safetyGround)
            {
                //SetDirection('s', "Safety");
               // Magabot.Leds.SetLedBoardState(255, 0, 0);

                if (holeMsgTime > 50)
                {
                    _dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (comboBoxSelectedUser.SelectedItem != null && checkBoxSendBumperMessage.IsChecked == true)
                        {
                            skype.SendMessage(comboBoxSelectedUser.SelectedItem.ToString(), Properties.Settings.Default.holeMessage);
                        }
                    }));

                    holeMsgTime = 0;
                }
            }

            if (backUpTime == 10)
            {
                SetDirection('p', "Safety");
                Magabot.Leds.SetLedBoardState(0, 0, 255);
            }
        }

        public void skype_MessageStatus(ChatMessage msg, TChatMessageStatus Status)
        {
            if (Status == TChatMessageStatus.cmsReceived)
            {
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

                        if (!comboBoxSelectedUser.Items.Cast<string>().Contains(msg.Sender.Handle)) // New chat
                        {
                            comboBoxSelectedUser.Items.Add(msg.Sender.Handle);

                            if (checkBoxSendWelcomeMessage.IsChecked == true)
                            {
                                msg.Chat.SendMessage(Properties.Settings.Default.welcomeMessage);
                        }
                            }
                        }

                        if (comboBoxSelectedUser.SelectedItem == null) // First chat
                            comboBoxSelectedUser.SelectedItem = msg.Sender.Handle;

                        if (msg.Sender.Handle == comboBoxSelectedUser.SelectedItem.ToString() && msg.Sender.Handle != "") // Message from the selected user
                        {
                            switch (msg.Body)
                            {
                                case "w": Magabot.Forward();
                                    break;
                                case "s": Magabot.Backward();
                                    break;
                                case "a": Magabot.Left();
                                    break;
                                case "d": Magabot.Right();
                                    break;
                                case "p": Magabot.Stop();
                                    break;
                                default: Magabot.Stop();
                                    break;
                            }
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
                if (status == TCallStatus.clsRinging)
                {
                    if (skypeCalls == 0)
                    {
                        call.Answer();
                    }
                    else
                    {
                        try
                        {
                            call.Finish();

                            skype.SendMessage(call.PartnerHandle, Properties.Settings.Default.waitMessage);
                        }
                        catch { }
                    }
                }
            }
            catch (InvalidCastException e)
            {
            }
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

        //public void SetDirection(char direction, String setter)
        //{
        //    _dispatcher.BeginInvoke((Action)(() =>
        //    {
        //        String msg = "";

        //        switch(direction)
        //        {
        //            case 'a':
        //                msg = "a - left";
        //                Magabot.DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(spinVelocity, -spinVelocity));
        //                break;
        //            case 'd':
        //                msg = "d - right";
        //                Magabot.DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(-spinVelocity,spinVelocity));
        //                break;
        //            case 'w':
        //                msg = "w - up";
        //                Magabot.DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(mainVelocity,mainVelocity));
        //                break;
        //            case 's':
        //                msg = "s - down";
        //                Magabot.DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(-mainVelocity,-mainVelocity));
        //                break;
        //            case 'p':   
        //                msg = "p - stop";
        //                Magabot.DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(0,0));
        //                break;
        //            default :
        //                msg = "p - stop";
        //                Magabot.DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(0,0));
        //                break;
        //        }
        //        lastDirectionSet = direction;
        //        lastDirectionSetter = setter;

        //        labelOrderValue.Content = msg;
        //    }));
        //}

    }
}
