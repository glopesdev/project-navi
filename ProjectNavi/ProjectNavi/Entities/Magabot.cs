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

namespace ProjectNavi.Entities
{
    public class Magabot
    {
        public static IDisposable Create(Game game, SpriteRenderer renderer, TaskScheduler scheduler, ICommunicationManager communication)
        {
            return (from magabot in Enumerable.Range(0, 1)
                    let wheelClicks = 3900
                    let wheelDistance = 0.345f
                    let wheelRadius = 0.0467f
                    let transform = new Transform2D()
                    let text = new StringBuilder()
                    let font = game.Content.Load<SpriteFont>("DebugFont")
                    let texture = game.Content.Load<Texture2D>("magabot_cm")
                    let bumperTexture = game.Content.Load<Texture2D>("square")
                    let bumpers = new BumperBoard(communication)
                    let battery = new BatteryBoard(communication)
                    let ground = new GroundSensorBoard(communication)
                    let leds = new LedBoard(communication)
                    let sonars = new SonarsBoard(communication)
                    let differentialSteering = new DifferentialSteeringBoard(communication, wheelRadius)
                    let odometry = new OdometryBoard(communication, wheelClicks, wheelRadius, wheelDistance)
                    let magabotState = new MagabotState(leds, differentialSteering)
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
                                            .Do(time => magabotState.DifferentialSteering.UpdateWheelVelocity(new WheelVelocity(-3, -3)))
                                            .Do(time => magabotState.Leds.SetLedBoardState(255, 0, 0))
                                            .Take(1)
                    select new CompositeDisposable(
                        bumpers,
                        battery,
                        ground,
                        leds,
                        sonars,
                        odometry,
                        differentialSteering,
                        renderer.SubscribeTexture(transform, texture),
                        renderer.SubscribeText(transform, font, () => text.ToString()),
                        behavior.Subscribe(),
                        differentialSteering.CommandChecksum.Subscribe(m => bumpers.GetBumperState()),
                        bumpers.BumpersMeasure.Subscribe(m =>
                        {
                            magabotState.BumperLeft = m.BumperLeft;
                            magabotState.BumperRight = m.BumperRight;
                            battery.GetBatteryState();
                        }),
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

                            for(int count =0; count < magabotState.Sonar.Length; count++)
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
                            OdometryLocalization.OdometerPredict(kalman, m.LinearDisplacement, m.AngularDisplacement);
                            transform.Position = new Vector2((float)kalman.Mean[0], (float)kalman.Mean[1]);
                            transform.Rotation = (float)kalman.Mean[2];
                            magabotState.Transform = transform ;

                            differentialSteering.Actuate();

                        })))
                    .First();
        }
    }
}
