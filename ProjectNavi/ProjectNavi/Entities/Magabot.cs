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
using ProjectNavi.Navigation;
using System.Reactive.Concurrency;
using OpenCV.Net;
using ProjectNavi.Tasks;
using Microsoft.Xna.Framework.Input;

namespace ProjectNavi.Entities
{
    public class Magabot
    {
        public static IDisposable Create(Game game, SpriteRenderer renderer, SpriteRenderer backRenderer, PrimitiveBatchRenderer primitiveRenderer, TaskScheduler scheduler, ICommunicationManager communication)
        {
            var kinectStream = game.Services.GetService<IObservable<KinectFrame>>();
            var markerStream = game.Services.GetService<IObservable<Tuple<IplImage, MarkerFrame>>>();

            return (from magabot in Enumerable.Range(0, 1)
                    //let wheelClicks = 1400
                    let wheelClicks = 3900
                    let wheelDistance = 0.357f
                    let wheelRadius = 0.0475f
                    let vehicle = new Vehicle()
                    let slam = new SlamController(vehicle)
                    let slamVisualizer = new SlamVisualizer(game, backRenderer, slam)
                    let kinectVisualizer = new KinectVisualizer(game)
                    let sonarVisualizer = new SonarVisualizer()
                    let steeringVisualizer = new SteeringVisualizer()
                    let freeSpaceVisualizer = new FreeSpaceVisualizer()
                    let actionPlayer = new ActionPlayer(game, game.Services)
                    let markerText = new StringBuilder()
                    let text = new StringBuilder()
                    let transform = vehicle.Transform
                    let font = game.Content.Load<SpriteFont>("DebugFont")
                    let texture = game.Content.Load<Texture2D>("magabot_cm")
                    let bumperTexture = game.Content.Load<Texture2D>("square")
                    let kinectTexture = new IplImageTexture(game.GraphicsDevice, 640, 480)
                    let bumpers = new BumperBoard(communication)
                    let battery = new BatteryBoard(communication)
                    let ground = new GroundSensorBoard(communication)
                    let leds = new LedBoard(communication)
                    let sonars = new SonarsBoard(communication)
                    let differentialSteering = new DifferentialSteeringBoard(communication, wheelRadius, wheelClicks)
                    let odometry = new OdometryBoard(communication, wheelClicks, wheelRadius, wheelDistance)
                    let magabotState = new MagabotState(leds, differentialSteering, bumpers, battery, ground, sonars)
                    //let skype = new MainWindow(magabotState)
                    let kalman = new KalmanFilter
                    {
                        Mean = new DenseVector(3),
                        Covariance = new DenseMatrix(new double[,] {
                            { 1, 0, 0 },
                            { 0, 1, 0 },
                            { 0, 0, 1 } })
                    }
                    //let target = new Transform2D(new Vector2(1, 0), 0, Vector2.One)
                    let target = new Transform2D()
                    let targetTexture = TextureFactory.CreateCircleTexture(game.GraphicsDevice, 2, Color.Violet)
                    let path = new[] { Vector2.UnitX, Vector2.One, Vector2.UnitY, Vector2.Zero }
                    let playActions = scheduler.TaskUpdate.Do(actionPlayer.Update)
                    let steeringBehavior = scheduler.TaskUpdate
                                            .Do(Steering.PathFollow(target, path, vehicle, 0.5f, 3, 0.05f))
                                            //.Do(Steering.Arrival(target, vehicle, 1, 3, 0.3f))
                                            .Do(gameTime => steeringVisualizer.Steering = vehicle.Steering)
                                            .Do(Locomotion.DifferentialSteering(vehicle, differentialSteering, wheelDistance, MathHelper.Pi / 16, 10, 100, 3))
                    let visualizerLoop = scheduler.TaskUpdate
                                            .Do(time => slamVisualizer.Update())
                                            .Do(time => kinectTexture.Update())
                    let behavior = scheduler.TaskUpdate
                                            .Do(time => odometry.UpdateOdometryCommand())
                                            //.Do(time => skype.Magabot = magabotState)
                                            .Do(time => magabotState.DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(0, 0)))
                                            .Do(time => magabotState.Leds.SetLedBoardState(255, 255, 255))
                                            //.Do(time => skype.Show())
                                            .Take(1)
                    select new CompositeDisposable(
                        bumpers,
                        battery,
                        ground,
                        leds,
                        sonars,
                        odometry,
                        differentialSteering,
                        playActions.Subscribe(),
                        steeringBehavior.Subscribe(),
                        visualizerLoop.Subscribe(),
                        kinectStream.Subscribe(),
                        backRenderer.SubscribeTexture(new Transform2D(new Vector2(-2.75f, 1.7f), 0, new Vector2(0.25f)), kinectTexture.Texture),
                        renderer.SubscribeTexture(transform, texture),
                        renderer.SubscribeTexture(target, targetTexture),
                        //renderer.SubscribeText(transform, font, () => text.ToString()),
                        renderer.SubscribeText(new Transform2D(-Vector2.One, 0, Vector2.One), font, () => markerText.ToString(), Color.White),
                        primitiveRenderer.SubscribePrimitive(transform, kinectVisualizer.DrawKinectDepthMap),
                        primitiveRenderer.SubscribePrimitive(transform, sonarVisualizer.DrawSonarFrame),
                        primitiveRenderer.SubscribePrimitive(transform, steeringVisualizer.DrawSteeringVector),
                        primitiveRenderer.SubscribePrimitive(new Transform2D(new Vector2(-350, 200), 0, Vector2.One), freeSpaceVisualizer.DrawFreeSpace),
                        behavior.Subscribe(),
                        kinectStream.Subscribe(kinectFrame =>
                        {
                            kinectVisualizer.Frame = kinectFrame;
                            freeSpaceVisualizer.FreeSpace = KinectFreeSpace.ComputeFreeSpace(kinectFrame, 1500);
                            //skype.OnKinectFrame(kinectFrame);
                        }),
                        markerStream.Subscribe(markerOutput =>
                        {
                            var image = markerOutput.Item1.Clone();
                            foreach (var marker in markerOutput.Item2.DetectedMarkers)
                            {
                                marker.Draw(image.DangerousGetHandle(), 0, 0, 255, 2, true);
                            }

                            kinectTexture.SetData(image);
                            slam.UpdateMeasurements(markerOutput.Item2);
                        }),
                        differentialSteering.CommandChecksum.Subscribe(m => bumpers.GetBumperState()),
                        bumpers.BumpersMeasure.Subscribe(m =>
                        {
                            battery.GetBatteryState();
                        }),
                        battery.BatteryMeasure.Subscribe(m =>
                        {
                            text.Clear();
                            text.AppendLine(string.Format("Battery: {0}", m.ToString()));
                            ground.GetGroundSensorState();
                        }),
                        ground.GroundSensorsMeasure.Subscribe(m =>
                        {
                            text.AppendLine(string.Format("IR: {0} IR: {1} IR: {2}", m.SensorLeft, m.SensorMiddle, m.SensorRight));
                            sonars.GetSonarsBoardState();
                        }),
                        sonars.SonarsBoardMeasure.Subscribe(m =>
                        {
                            sonarVisualizer.SonarFrame = m;
                            for (int count = 0; count < m.Length; count++)
                            {
                                //magabotState
                                var sonar = m[count];
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
