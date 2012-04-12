using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Cyberiad.Graphics;
using System.Reactive.Disposables;
using Microsoft.Xna.Framework.Graphics;
using Cyberiad;
using ProjectNavi.Hardware;
using System.Reactive.Linq;
using ProjectNavi.Localization;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Threading;
using Bonsai;
using System.Linq.Expressions;
using ProjectNavi.Bonsai.Kinect;
using Aruco.Net;
using MathNet.Numerics.LinearAlgebra.Generic;
using ProjectNavi.SkypeController;
using ProjectNavi.Bonsai.Aruco;
using ProjectNavi.Graphics;

namespace ProjectNavi.Entities
{
    public class Magabot
    {
        public static IDisposable Create(Game game, SpriteRenderer renderer, SpriteRenderer backRenderer, TaskScheduler scheduler, ICommunicationManager communication, ReactiveWorkflow vision)
        {
            var connections = vision.Connections.ToArray();
            var kinectStream = Expression.Lambda<Func<IObservable<KinectFrame>>>(connections[0]).Compile()();
            var markerStream = Expression.Lambda<Func<IObservable<MarkerFrame>>>(connections[1]).Compile()();

            return (from magabot in Enumerable.Range(0, 1)
                    let wheelClicks = 3900
                    let wheelDistance = 0.345f
                    let wheelRadius = 0.0467f
                    let slam = new SlamController()
                    let slamVisualizer = new SlamVisualizer(game, backRenderer, slam)
                    let markerText = new StringBuilder()
                    let text = new StringBuilder()
                    let transform = slam.AgentTransform
                    let covarianceTransform = new Transform2D()
                    let font = game.Content.Load<SpriteFont>("DebugFont")
                    let texture = game.Content.Load<Texture2D>("magabot_cm")
                    let bumperTexture = game.Content.Load<Texture2D>("square")
                    let kinectTexture = new IplImageTexture(game.GraphicsDevice, 640, 480)
                    let covarianceTexture = TextureFactory.CreateCircleTexture(game.GraphicsDevice, 10, Color.White)
                    let bumpers = new BumperBoard(communication)
                    let battery = new BatteryBoard(communication)
                    let ground = new GroundSensorBoard(communication)
                    let leds = new LedBoard(communication)
                    let sonars = new SonarsBoard(communication)
                    let differentialSteering = new DifferentialSteeringBoard(communication, wheelRadius, wheelClicks)
                    let odometry = new OdometryBoard(communication, wheelClicks, wheelRadius, wheelDistance)
                    let magabotState = new MagabotState(leds, differentialSteering)
                    let skype = new MainWindow()
                    let kalman = new KalmanFilter
                    {
                        Mean = new DenseVector(3),
                        Covariance = new DenseMatrix(new double[,] {
                            { 1, 0, 0 },
                            { 0, 1, 0 },
                            { 0, 0, 1 } })
                    }
                    let visualizerLoop = scheduler.TaskUpdate
                                            .Do(time => slamVisualizer.Update())
                                            .Do(time => kinectTexture.Update())
                    let behavior = scheduler.TaskUpdate
                                            .Do(time => odometry.UpdateOdometryCommand())
                                            .Do(time => magabotState.DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(0, 0)))
                                            .Do(time => magabotState.Leds.SetLedBoardState(255, 255, 255))
                        //.Do(time => skype.Show())
                        //.Do(time => skype.Magabot = magabotState)
                                            .Take(1)
                    select new CompositeDisposable(
                        bumpers,
                        battery,
                        ground,
                        leds,
                        sonars,
                        odometry,
                        differentialSteering,
                        visualizerLoop.Subscribe(),
                        //kinectStream.Subscribe(),
                        //renderer.SubscribeTexture(covarianceTransform, covarianceTexture),
                        backRenderer.SubscribeTexture(new Transform2D(), kinectTexture.Texture),
                        renderer.SubscribeTexture(transform, texture),
                        //renderer.SubscribeText(transform, font, () => text.ToString()),
                        renderer.SubscribeText(new Transform2D(-Vector2.One, 0, Vector2.One), font, () => markerText.ToString()),
                        behavior.Subscribe(),
                        kinectStream.Subscribe(kinectFrame =>
                        {
                            kinectTexture.SetData(kinectFrame.ColorImage);
                            skype.OnKinectFrame(kinectFrame);
                        }),
                        markerStream.Subscribe(markerFrame => slam.UpdateMeasurements(markerFrame)),
                        differentialSteering.CommandChecksum.Subscribe(m => bumpers.GetBumperState()),
                        bumpers.BumpersMeasure.Subscribe(m => battery.GetBatteryState()),
                        battery.BatteryMeasure.Subscribe(m =>
                        {
                            magabotState.Battery = m;
                            text.Clear();
                            text.AppendLine(string.Format("Battery: {0}", m.ToString()));
                            ground.GetGroundSensorState();
                        }),
                        ground.GroundSensorsMeasure.Subscribe(m =>
                        {
                            magabotState.IRGroundLeft = m.SensorLeft;
                            magabotState.IRGroundMiddle = m.SensorMiddle;
                            magabotState.IRGroundRight = m.SensorRight;
                            text.AppendLine(string.Format("IR: {0} IR: {1} IR: {2}", m.SensorLeft, m.SensorMiddle, m.SensorRight));
                            sonars.GetSonarsBoardState();
                        }),
                        sonars.SonarsBoardMeasure.Subscribe(m =>
                        {

                            for (int count = 0; count < magabotState.Sonar.Length; count++)
                            {
                                //magabotState
                                var sonar = m[count];
                                magabotState.Sonar[count] = sonar;
                                text.Append(string.Format("Sonar: {0} ", sonar));
                            }
                            text.AppendLine();
                            leds.Actuate();
                        }),
                        leds.LedBoardMeasure.Subscribe(m => odometry.UpdateOdometryCommand()),
                        odometry.Odometry.Subscribe(m =>
                        {
                            slam.UpdateMotion(m.LinearDisplacement, m.AngularDisplacement);
                            slam.UpdateEstimate();
                            differentialSteering.Actuate();

                        })))
                    .First();
        }
    }
}
