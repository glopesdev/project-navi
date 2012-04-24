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
using Coding4Fun.Kinect.Wpf;
using Microsoft.Kinect;


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

        public MagabotState Magabot {get; set;}

        
        int bumpMsgTime;
        int holeMsgTime;
        bool safetyBump, safetyGround;

        char lastDirectionSet;
        String lastDirectionSetter;

        int selectedTrackingId;
        bool reportSkeletons;

        Button[] personButton = new Button[6];

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

            personButton[0] = personButton0;
            personButton[1] = personButton1;
            personButton[2] = personButton2;
            personButton[3] = personButton3;
            personButton[4] = personButton4;
            personButton[5] = personButton5;

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

            reportSkeletons = false;
        }

        public void OnKinectFrame(KinectFrame kinectFrame)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                String skeletonsMsg = "People: ";
                
                foreach (Button button in personButton)
                {
                    button.Content = "";
                    button.Visibility = System.Windows.Visibility.Hidden;
                }

                for (int i = 0; i < 6; i++)
                {
                    Skeleton skeleton = kinectFrame.SkeletonData.ElementAt(i);

                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        foreach (Joint joint in skeleton.Joints)
                        {
                            if (joint.JointType == JointType.Head)
                            {
                                personButton[i].Content = skeleton.TrackingId;
                                personButton[i].Visibility = System.Windows.Visibility.Visible;

                                Joint scaledJoint = joint.ScaleTo(320, 240, .3f, .3f);

                                Canvas.SetLeft(personButton[i], scaledJoint.Position.X - (personButton[i].Width / 2));
                                Canvas.SetTop(personButton[i], scaledJoint.Position.Y);

                                skeletonsMsg += String.Format("{0}({1}|{2});", skeleton.TrackingId, joint.Position.X, joint.Position.Y);
                            }
                        }

                        if (skeleton.TrackingId == selectedTrackingId)
                        {
                            if (skeleton.Position.Z < 1)
                                SetDirection('s', "Skeleton");
                            else if (skeleton.Position.Z > 1.5)
                                SetDirection('w', "Skeleton");
                            else if (skeleton.Position.X < -.2f)
                                SetDirection('d', "Skeleton");
                            else if (skeleton.Position.X > -.2f && skeleton.Position.X < .2f)
                                SetDirection('p', "Skeleton");
                            else if (skeleton.Position.X > .2f)
                                SetDirection('a', "Skeleton");
                        }
                    }
                }

                if (reportSkeletons )
                {
                    if (comboBoxSelectedUser.SelectedItem != null && checkBoxSendBumperMessage.IsChecked == true)
                    {
                        skype.SendMessage(comboBoxSelectedUser.SelectedItem.ToString(), skeletonsMsg);
                    }

                    reportSkeletons = false;
                }
            }));
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

            if (Magabot.BumperSensorState.BumperLeft || Magabot.BumperSensorState.BumperRight)
            {
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
                    else if (msg.Body == "What people are there?")
                    {
                        reportSkeletons = true;
                    }
                    else if (msg.Body.Contains("Follow: "))
                    {
                        selectedTrackingId = (int)Int16.Parse(msg.Body.Split(new [] {':'})[1]);
                    }
                    else if(msg.Body.StartsWith("#"))
                    {
                        string markerName = msg.Body.Substring(1);
                        Magabot.ActivateMarker(markerName);
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

                        if (comboBoxSelectedUser.SelectedItem == null) // First chat
                            comboBoxSelectedUser.SelectedItem = msg.Sender.Handle;

                        if (msg.Sender.Handle == comboBoxSelectedUser.SelectedItem.ToString() && msg.Sender.Handle != "") // Message from the selected user
                        {
                            switch (msg.Body)
                            {
                                case "w": SetDirection('w', "Message");
                                    break;
                                case "s": SetDirection('s', "Message");
                                    break;
                                case "a": SetDirection('a', "Message");
                                    break;
                                case "d": SetDirection('d', "Message");
                                    break;
                                case "p": SetDirection('p', "Message");
                                    break;
                                default: SetDirection('p', "Message");
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
                if (status == TCallStatus.clsRinging && call.Type != TCallType.cltOutgoingP2P)
                {
                    if (skypeCalls == 0)
                    {
                        try
                        {
                            call.Answer();
                        }
                        catch { }
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

        private void personButton1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                selectedTrackingId = (int)personButton1.Content;
            }
            catch { }
        }

        private void personButton2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                selectedTrackingId = (int)personButton2.Content;
            }
            catch { }
        }

        private void personButton3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                selectedTrackingId = (int)personButton3.Content;
            }
            catch { }
        }

        private void personButton4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                selectedTrackingId = (int)personButton4.Content;
            }
            catch { }
        }

        private void personButton5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                selectedTrackingId = (int)personButton5.Content;
            }
            catch { }
        }

        private void personButton0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                selectedTrackingId = (int)personButton0.Content;
            }
            catch { }
        }

        public void SetDirection(char direction, String setter)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                String msg = "";

                switch (direction)
                {
                    case 'a':
                        msg = "a - left";
                        Magabot.Left();
                        break;
                    case 'd':
                        msg = "d - right";
                        Magabot.Right();
                        break;
                    case 'w':
                        msg = "w - up";
                        Magabot.Forward();
                        break;
                    case 's':
                        msg = "s - down";
                        Magabot.Backward();
                        break;
                    case 'p':
                        msg = "p - stop";
                        Magabot.Stop();
                        break;
                    default:
                        msg = "p - stop";
                        Magabot.Stop();
                        break;
                }
                lastDirectionSet = direction;
                lastDirectionSetter = setter;

                labelOrderValue.Content = msg;

                if (setter != "Skeleton")
                    selectedTrackingId = -1;

            }));
        }

    }
}
