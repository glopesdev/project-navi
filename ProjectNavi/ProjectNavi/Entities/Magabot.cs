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
using ProjectNavi.SkypeController;

namespace ProjectNavi.Entities
{
    public class Magabot
    {
        public static void KalmanVisualization(KalmanFilter kalman, Transform2D transform)
        {
            var eigendecomposition = kalman.Covariance.Take(new[] { 0, 1 }).Evd();
            var eigenvalues = eigendecomposition.EigenValues();
            var eigenvectors = eigendecomposition.EigenVectors();

            // Transform of a unit circle
            var translation = new Vector2((float)kalman.Mean[0], (float)kalman.Mean[1]);
            var rotation = (float)Math.Atan2(eigenvectors[1, 0], eigenvectors[0, 0]);
            var scale = new Vector2((float)eigenvalues[0].Magnitude, (float)eigenvalues[1].Magnitude);

            transform.Position = translation;
            transform.Rotation = rotation;
            transform.Scale = scale;
        }

        public static IDisposable Create(Game game, SpriteRenderer renderer, TaskScheduler scheduler, ICommunicationManager communication, ReactiveWorkflow vision)
        {
            //var connections = vision.Connections.ToArray();
            //var kinectStream = Expression.Lambda<Func<IObservable<KinectFrame>>>(connections[0]).Compile()();
            //var markerStream = Expression.Lambda<Func<IObservable<IEnumerable<Marker>>>>(connections[0]).Compile()();

            return (from magabot in Enumerable.Range(0, 1)
                    let wheelClicks = 3900
                    let wheelDistance = 0.345f
                    let wheelRadius = 0.0467f
                   /* let marker0 = new MarkerLocalization
                    {
                        MarkerPosition = new DenseVector(new[] { .0, .0 }),
                        SensorOffset = new DenseVector(new[] { .1, .0 }),
                    }
                    */
                    let markerText = new StringBuilder()
                    let text = new StringBuilder()
                    let transform = new Transform2D()
                    let covarianceTransform = new Transform2D()
                    let font = game.Content.Load<SpriteFont>("DebugFont")
                    let texture = game.Content.Load<Texture2D>("magabot_cm")
                    let bumperTexture = game.Content.Load<Texture2D>("square")
                    let covarianceTexture = TextureFactory.CreateCircleTexture(game.GraphicsDevice, 10, Color.White)
                    let bumpers = new BumperBoard(communication)
                    let battery = new BatteryBoard(communication)
                    let ground = new GroundSensorBoard(communication)
                    let leds = new LedBoard(communication)
                    let sonars = new SonarsBoard(communication)
                    let differentialSteering = new DifferentialSteeringBoard(communication, wheelRadius,wheelClicks)
                    let odometry = new OdometryBoard(communication, wheelClicks, wheelRadius, wheelDistance)
                    let magabotState = new MagabotState(leds, differentialSteering,bumpers,battery,ground, sonars)
                    let skype = new MainWindow(magabotState)
                    let kalman = new KalmanFilter
                    {
                        Mean = new DenseVector(3),
                        Covariance = new DenseMatrix(new double[,] {
                            { 1, 0, 0 },
                            { 0, 1, 0 },
                            { 0, 0, 1 } })
                    }
                    let behavior = scheduler.TaskUpdate
                                            .Do(time => odometry.UpdateOdometryCommand())
                                            .Do(time => magabotState.DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(0, 0)))
                                            .Do(time => magabotState.Leds.SetLedBoardState(255, 255, 255))
                                            .Do(time => skype.Show())
                                            .Take(1)
                    select new CompositeDisposable(
                        bumpers,
                        battery,
                        ground,
                        leds,
                        sonars,
                        odometry,
                        differentialSteering,
                        //kinectStream.Subscribe(),
                        renderer.SubscribeTexture(covarianceTransform, covarianceTexture),
                        renderer.SubscribeTexture(transform, texture),
                        //renderer.SubscribeText(transform, font, () => text.ToString()),
                        renderer.SubscribeText(new Transform2D(-Vector2.One, 0, Vector2.One), font, () => markerText.ToString()),
                        behavior.Subscribe(),
                        //markerStream.Subscribe(markers =>
                        //{
                        //    foreach (var marker in markers)
                        //    {
                        //        var markerTransform = marker.GetGLModelViewMatrix();
                        //        //var markerPosition = new Vector3((float)markerTransform[12], (float)markerTransform[13], (float)markerTransform[14]);
                        //        var markerPosition = new DenseVector(new[] { markerTransform[14], markerTransform[12] });
                        //        marker0.MarkerUpdate(kalman, markerPosition);
                        //        transform.Position = new Vector2((float)kalman.Mean[0], (float)kalman.Mean[1]);
                        //        transform.Rotation = (float)kalman.Mean[2];
                        //        KalmanVisualization(kalman, covarianceTransform);
                        //        markerText.Clear();
                        //        markerText.Append(markerPosition.ToString());
                        //        //System.Diagnostics.Trace.WriteLine(markerPosition);
                        //    }
                        //}),
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

                            for(int count =0; count < m.Length; count++)
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
                            if (m.LinearDisplacement != 0 && m.AngularDisplacement != 0)
                            {
                                OdometryLocalization.OdometerPredict(kalman, m.LinearDisplacement, m.AngularDisplacement);
                                transform.Position = new Vector2((float)kalman.Mean[0], (float)kalman.Mean[1]);
                                transform.Rotation = (float)kalman.Mean[2];
                                KalmanVisualization(kalman, covarianceTransform);
                           
                                //System.Diagnostics.Trace.WriteLine("l: " + m.LinearDisplacement + " a: " + m.AngularDisplacement);
                            }
                            differentialSteering.Actuate();

                        })))
                    .First();
        }
    }
}
