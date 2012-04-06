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
                    let texture = game.Content.Load<Texture2D>("magabot_cm")
                    let odometry = new OdometryBoard(communication, wheelClicks, wheelRadius, wheelDistance)
                    let kalman = new KalmanFilter
                    {
                        Mean = new DenseVector(3),
                        Covariance = new DenseMatrix(new double[,] {
                            { 1, 0, 0 },
                            { 0, 1, 0 },
                            { 0, 0, 1 } })
                    }

                    let odometryMeasurements = from measurement in odometry.Odometry
                                               select new Vector2(
                                                   (float)(measurement.LinearDisplacement * Math.Cos(measurement.AngularDisplacement)),
                                                   (float)(measurement.LinearDisplacement * Math.Sin(measurement.AngularDisplacement)))
                    let behavior = scheduler.TaskUpdate
                                            .Do(time => odometry.UpdateOdometryCommand())
                                            .Take(1)
                    select new CompositeDisposable(
                        odometry,
                        renderer.SubscribeTexture(transform, texture),
                        behavior.Subscribe(),
                        odometry.Odometry.Subscribe(m =>
                        {
                            OdometryLocalization.OdometerPredict(kalman, m.LinearDisplacement, m.AngularDisplacement);
                            transform.Position = new Vector2((float)kalman.Mean[0], (float)kalman.Mean[1]);
                            transform.Rotation = (float)kalman.Mean[2];
                            odometry.UpdateOdometryCommand();
                        })))
                    .First();
        }
    }
}
