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
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SKYPE4COMLib;

namespace ProjectNavi.RemoteGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Skype skype;

        private Dispatcher _dispatcher;
        System.Windows.Threading.DispatcherTimer frameTimer;

        string selectedSkypeChatUser;
        System.Threading.Timer time;
        int skypeChats = 0;

        char lastDirectionSet;
        String lastDirectionSetter;

        SolidColorBrush transparentBrush = new SolidColorBrush(Colors.Transparent);
        SolidColorBrush whiteBrush = new SolidColorBrush(Colors.White);

        int bumpTime;
        int holeTime;

        Button[] personButton = new Button[6];

        int selectedTrackingId;

        public MainWindow()
        {
            InitializeComponent();

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

            _dispatcher = this.Dispatcher;

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close,
                new ExecutedRoutedEventHandler(delegate(object sender, ExecutedRoutedEventArgs args) { this.Close(); })));

            SetDirection('p', "init");

            expanderControls.Visibility = System.Windows.Visibility.Hidden;
            expanderDetails.Visibility = System.Windows.Visibility.Hidden;

            //imageBumpAlert.Visibility = System.Windows.Visibility.Hidden;
            //imageHoleAlert.Visibility = System.Windows.Visibility.Hidden;

            bumpTime = 20;
            holeTime = 20;

            personButton[0] = personButton0;
            personButton[1] = personButton1;
            personButton[2] = personButton2;
            personButton[3] = personButton3;
            personButton[4] = personButton4;
            personButton[5] = personButton5;

            foreach (Button button in personButton)
            {
                button.Visibility = System.Windows.Visibility.Hidden;
            }

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
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    if (skype.ActiveCalls.Count > 0)
                    {
                        this.Background = transparentBrush;

                        foreach (Call call in skype.ActiveCalls)
                        {
                            if (call.Status == TCallStatus.clsInProgress)
                            {
                                selectedSkypeChatUser = call.PartnerHandle.ToString();

                                labelSelectedUser.Content = call.PartnerHandle.ToString();
                            }
                        }
                    }
                    else
                    {
                        this.Background = whiteBrush;
                    }
                }));
            }
            catch
            {
            }

            try
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    expanderControls.Visibility = System.Windows.Visibility.Visible;
                    Canvas.SetRight(expanderControls, 10);
                    Canvas.SetTop(expanderControls, 10);

                    expanderDetails.Visibility = System.Windows.Visibility.Visible;
                    Canvas.SetLeft(expanderDetails, 10);
                    Canvas.SetTop(expanderDetails, 10);

                    Canvas.SetLeft(imageBumpAlert, 100);
                    Canvas.SetBottom(imageBumpAlert, 10);

                    Canvas.SetRight(imageHoleAlert, 100);
                    Canvas.SetBottom(imageHoleAlert, 10);
                }));
            }
            catch
            {
            }
        }

        public void skype_MessageStatus(ChatMessage msg, TChatMessageStatus Status)
        {
            if (Status == TChatMessageStatus.cmsReceived)
            {
                if (msg.Sender.Handle == selectedSkypeChatUser)
                {
                    if (msg.Body == "b" || msg.Body == Properties.Settings.Default.bumperMessage)
                    {
                        bumpTime = 0;
                    }
                    else if (msg.Body == "h" || msg.Body == Properties.Settings.Default.holeMessage)
                    {
                        holeTime = 0;
                    }
                    else if(msg.Body.Contains("People: "))
                    {
                        String[] skeletonData = new String[6];

                        skeletonData[0] = msg.Body.Split(' ')[1];

                        int i = 0;
                        while (skeletonData[i].IndexOf(';') > 0)
                        {
                            //skeletonData[i+1] = skeletonData[i].Split(';')[1];
                            //skeletonData[i] = skeletonData[i].Split(';')[0];
                            skeletonData[i+1] = skeletonData[i].Substring(skeletonData[i].IndexOf(';')+1);
                            skeletonData[i] = skeletonData[i].Substring(0,skeletonData[i].IndexOf(';'));

                            i++;
                        }

                        for (int o = 0; o < 6; o++) 
                        {
                            if (skeletonData[o] != null && skeletonData[o].Contains('(') && skeletonData[o].Contains('|') && skeletonData[o].Contains(')'))
                            {

                                personButton[o].Content = skeletonData[o].Substring(0, skeletonData[o].IndexOf('('));
                                personButton[o].Visibility = System.Windows.Visibility.Visible;

                                String cordinates = skeletonData[o].Substring(skeletonData[o].IndexOf('('));

                                double x = (float)Double.Parse(cordinates.Substring(1, cordinates.IndexOf('|') - 1));
                                double y = (float)Double.Parse(cordinates.Substring(cordinates.IndexOf('|') + 1, cordinates.IndexOf(')') - cordinates.IndexOf('|') - 1));

                                x = (MainCanvas.ActualWidth / 2) + (x * MainCanvas.ActualWidth) - (personButton[o].Width / 2);
                                y = (MainCanvas.ActualHeight / 2) + (y * MainCanvas.ActualHeight);

                                Canvas.SetLeft(personButton[o], x);
                                Canvas.SetTop(personButton[o], y);
                            }
                        }
                    }
                }
            }
        }

        public void skype_CallStatus(Call call, TCallStatus status)
        {
            try
            {
                if (status == TCallStatus.clsInProgress)
                {
                    _dispatcher.BeginInvoke((Action)(() =>
                    {
                        selectedSkypeChatUser = call.PartnerHandle.ToString();

                        labelSelectedUser.Content = call.PartnerDisplayName.ToString();
                    }));
                }
            }
            catch
            {

            }
        }

        public void DragWindow(object sender, MouseButtonEventArgs args)
        {
            DragMove();
        }

        private void maximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState != WindowState.Maximized)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        public void SetDirection(char direction, String setter)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {

                String msg = "";
                msg += direction;

                if (direction == 'a')
                {
                    labelOrderValue.Content = "a - left";
                    buttonForward.Opacity = 0.5;
                    buttonBackward.Opacity = 0.5;
                    buttonLeft.Opacity = 1;
                    buttonRight.Opacity = 0.5;
                    buttonStop.Opacity = 0.5;
                }
                if (direction == 'd')
                {
                    labelOrderValue.Content = "d - right";
                    buttonForward.Opacity = 0.5;
                    buttonBackward.Opacity = 0.5;
                    buttonLeft.Opacity = 0.5;
                    buttonRight.Opacity = 1;
                    buttonStop.Opacity = 0.5;
                }
                if (direction == 'w')
                {
                    labelOrderValue.Content = "w - up";
                    buttonForward.Opacity = 1;
                    buttonBackward.Opacity = 0.5;
                    buttonLeft.Opacity = 0.5;
                    buttonRight.Opacity = 0.5;
                    buttonStop.Opacity = 0.5;
                }
                if (direction == 's')
                {
                    labelOrderValue.Content = "s - down";
                    buttonForward.Opacity = 0.5;
                    buttonBackward.Opacity = 1;
                    buttonLeft.Opacity = 0.5;
                    buttonRight.Opacity = 0.5;
                    buttonStop.Opacity = 0.5;
                }
                if (direction == 'p')
                {
                    labelOrderValue.Content = "p - stop";
                    buttonForward.Opacity = 0.5;
                    buttonBackward.Opacity = 0.5;
                    buttonLeft.Opacity = 0.5;
                    buttonRight.Opacity = 0.5;
                    buttonStop.Opacity = 1;
                }

                if (selectedSkypeChatUser != "" && selectedSkypeChatUser != null)
                {
                    if (msg != "" && direction != lastDirectionSet)
                        skype.SendMessage(selectedSkypeChatUser, msg);
                }
                else
                {
                    labelOrderValue.Content = "No user selected";
                }


                lastDirectionSet = direction;
                lastDirectionSetter = setter;

            }));
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                SetDirection('a', "keyboard");
            }
            if (e.Key == Key.Right)
            {
                SetDirection('d', "keyboard");
            }
            if (e.Key == Key.Up)
            {
                SetDirection('w', "keyboard");
            }
            if (e.Key == Key.Down)
            {
                SetDirection('s', "keyboard");
            }
            if (e.Key == Key.Space)
            {
                SetDirection('p', "keyboard");
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            SetDirection('p', "keyboard");
        }

        private void buttonForward_Click(object sender, RoutedEventArgs e)
        {
            SetDirection('w', "button");
        }

        private void buttonBackward_Click(object sender, RoutedEventArgs e)
        {
            SetDirection('s', "button");
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            SetDirection('p', "button");
        }

        private void buttonLeft_Click(object sender, RoutedEventArgs e)
        {
            SetDirection('a', "button");
        }

        private void buttonRight_Click(object sender, RoutedEventArgs e)
        {
            SetDirection('d', "button");
        }

        private void Update(object sender, EventArgs e)
        {
            try
            {
                // Get the game pad state.
                GamePadState currentState = GamePad.GetState(PlayerIndex.One);

                if (currentState.IsConnected)
                {
                    // Allows the game to exit
                    //if (currentState.Buttons.Back == ButtonState.Pressed)
                    //    this.Close();

                    String msgThumbstick = "";
                    String msgButtons = "";

                    float xValue = 0.0f;
                    float yValue = 0.0f;

                    // Rotate the model using the left thumbstick, and scale it down
                    xValue = currentState.ThumbSticks.Left.X * 0.10f;
                    yValue = currentState.ThumbSticks.Left.Y * 0.10f;

                    if (xValue < -0.09)
                        msgThumbstick = "a";
                    else if (xValue > 0.09)
                        msgThumbstick = "d";
                    else if (yValue < -0.09)
                        msgThumbstick = "s";
                    else if (yValue > 0.09)
                        msgThumbstick = "w";
                    else
                        msgThumbstick = "p";

                    if (currentState.DPad.Left == ButtonState.Pressed)
                        msgButtons = "a";
                    else if (currentState.DPad.Right == ButtonState.Pressed)
                        msgButtons = "d";
                    else if (currentState.DPad.Up == ButtonState.Pressed)
                        msgButtons = "w";
                    else if (currentState.DPad.Down == ButtonState.Pressed)
                        msgButtons = "s";
                    else
                        msgButtons = "p";


                    if (msgThumbstick == "p" && msgButtons == "p" && lastDirectionSetter == "controller")
                    {
                        SetDirection('p', "controller");
                    }
                    else if (msgThumbstick == "a" || msgButtons == "a")
                    {
                        SetDirection('a', "controller");
                    }
                    else if (msgThumbstick == "d" || msgButtons == "d")
                    {
                        SetDirection('d', "controller");
                    }
                    else if (msgThumbstick == "w" || msgButtons == "w")
                    {
                        SetDirection('w', "controller");
                    }
                    else if (msgThumbstick == "s" || msgButtons == "s")
                    {
                        SetDirection('s', "controller");
                    }


                    if (bumpTime < 20 || holeTime < 20)
                    {
                        GamePad.SetVibration(PlayerIndex.One,
                                40,
                                40);
                    }
                    else
                    {
                        GamePad.SetVibration(PlayerIndex.One,
                                0,
                                0);
                    }
                }
            }
            catch
            {

            }

            _dispatcher.BeginInvoke((Action)(() =>
            {
                if (bumpTime > 20)
                {
                    imageBumpAlert.Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    imageBumpAlert.Visibility = System.Windows.Visibility.Visible;
                }


                if (holeTime > 20)
                {
                    imageHoleAlert.Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    imageHoleAlert.Visibility = System.Windows.Visibility.Visible;
                }
            }));

            bumpTime++;
            holeTime++;
        }


        private void personButton0_Click(object sender, RoutedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                skype.SendMessage(selectedSkypeChatUser, "Follow: " + personButton0.Content);

                foreach (Button button in personButton)
                {
                    button.Visibility = System.Windows.Visibility.Hidden;
                }
            }));
        }

        private void personButton1_Click(object sender, RoutedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                skype.SendMessage(selectedSkypeChatUser, "Follow: " + personButton1.Content);

                foreach (Button button in personButton)
                {
                    button.Visibility = System.Windows.Visibility.Hidden;
                }
            }));
        }

        private void personButton2_Click(object sender, RoutedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                skype.SendMessage(selectedSkypeChatUser, "Follow: " + personButton2.Content);

                foreach (Button button in personButton)
                {
                    button.Visibility = System.Windows.Visibility.Hidden;
                }
            }));
        }

        private void personButton3_Click(object sender, RoutedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                skype.SendMessage(selectedSkypeChatUser, "Follow: " + personButton3.Content);

                foreach (Button button in personButton)
                {
                    button.Visibility = System.Windows.Visibility.Hidden;
                }
            }));
        }

        private void personButton4_Click(object sender, RoutedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                skype.SendMessage(selectedSkypeChatUser, "Follow: " + personButton4.Content);

                foreach (Button button in personButton)
                {
                    button.Visibility = System.Windows.Visibility.Hidden;
                }
            }));
        }

        private void personButton5_Click(object sender, RoutedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                skype.SendMessage(selectedSkypeChatUser, "Follow: " + personButton5.Content);

                foreach (Button button in personButton)
                {
                    button.Visibility = System.Windows.Visibility.Hidden;
                }
            }));
        }

        private void buttonFollowPerson_Click(object sender, RoutedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                if (selectedSkypeChatUser != "" && selectedSkypeChatUser != null)
                {
                    skype.SendMessage(selectedSkypeChatUser, "What people are there?");
                }
                else
                {
                    labelOrderValue.Content = "No user selected";
                }
            }));
        }
    }



}
