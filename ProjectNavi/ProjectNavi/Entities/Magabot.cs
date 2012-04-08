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
        public static Transform2D KalmanVisualization(KalmanFilter kalman)
        {
            var eigendecomposition = kalman.Covariance.Evd();
            var eigenvalues = eigendecomposition.EigenValues();
            var eigenvectors = eigendecomposition.EigenVectors();

            // Transform of a unit circle
            var translation = new Vector2((float)kalman.Mean[0], (float)kalman.Mean[1]);
            var rotation = (float)Math.Atan2(eigenvectors[1, 0], eigenvectors[0, 0]);
            var scale = new Vector2((float)eigenvalues[0].Real, (float)eigenvalues[1].Real);

            return new Transform2D(translation, rotation, scale);
        }

        public static IDisposable Create(Game game, SpriteRenderer renderer, TaskScheduler scheduler, ICommunicationManager communication)
        {
            return (from magabot in Enumerable.Range(0, 1)
                    let wheelClicks = 3900
                    let wheelDistance = 0.345f
                    let wheelRadius = 0.0467f
                    let text = new StringBuilder()
                    let transform = new Transform2D()
                    let textTransform = new Transform2D { Position = new Vector2(0, 1) }
                    let font = game.Content.Load<SpriteFont>("DebugFont")
                    let texture = game.Content.Load<Texture2D>("magabot_cm")
                    let bumperTexture = game.Content.Load<Texture2D>("square")
                    let bumpers = new BumperBoard(communication)
                    let battery = new BatteryBoard(communication)
                    let differentialSteering = new DifferentialSteeringBoard(communication, wheelRadius)
                    let odometry = new OdometryBoard(communication, wheelClicks, wheelRadius, wheelDistance)
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
                                            .Do(time => differentialSteering.UpdateWheelVelocity(new WheelVelocity(10, 10)))
                                            .Take(1)
                    select new CompositeDisposable(
                        bumpers,
                        battery,
                        odometry,
                        differentialSteering,
                        renderer.SubscribeTexture(transform, texture),
                        renderer.SubscribeText(textTransform, transform, font, () => text.ToString()),
                        behavior.Subscribe(),
                        differentialSteering.CommandChecksum.Subscribe(m => bumpers.GetBumperState()),
                        bumpers.BumpersMeasure.Subscribe(m =>
                            {
                                battery.GetBatteryState();
                            }),
                        battery.BatteryMeasure.Subscribe(m =>
                            {
                                text.Clear();
                                text.AppendLine(string.Format("Battery: {0}", m.ToString()));
                                odometry.UpdateOdometryCommand();
                            }),
                        odometry.Odometry.Subscribe(m =>
                        {
                            OdometryLocalization.OdometerPredict(kalman, m.LinearDisplacement, m.AngularDisplacement);
                            transform.Position = new Vector2((float)kalman.Mean[0], (float)kalman.Mean[1]);
                            transform.Rotation = (float)kalman.Mean[2];
                            differentialSteering.Actuate();
                        })))
                    .First();
        }
    }
}
