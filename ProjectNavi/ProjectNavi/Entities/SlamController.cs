using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectNavi.Localization;
using Microsoft.Xna.Framework.Graphics;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Xna.Framework;
using Cyberiad.Graphics;
using Aruco.Net;
using MathNet.Numerics.LinearAlgebra.Generic;
using Cyberiad;
using ProjectNavi.Bonsai.Aruco;
using ProjectNavi.Navigation;

namespace ProjectNavi.Entities
{
    public class SlamController
    {
        EkfSlam slam;
        Dictionary<int, int> landmarkIndices;
        Vehicle vehicle;

        Vector<double> motion;
        IEnumerable<LandmarkMeasurement> measurements;

        public SlamController(Vehicle agent)
        {
            slam = new EkfSlam();
            slam.MotionNoise = new DenseMatrix(new[,]
            {
                {0.01, 0, 0},
                {0, 0.01, 0},
                {0, 0, 0.02}
            });
            slam.MeasurementNoise = new DenseMatrix(new[,]
            {
                {0.1, 0},
                {0, 0.1}
            });

            landmarkIndices = new Dictionary<int, int>();
            motion = new DenseVector(2);
            measurements = Enumerable.Empty<LandmarkMeasurement>();
            vehicle = agent;
        }

        public EkfSlam SlamEstimator
        {
            get { return slam; }
        }

        public void UpdateMeasurements(MarkerFrame markerFrame)
        {
            measurements = markerFrame.DetectedMarkers.Select(marker =>
            {
                var markerTransform = marker.GetGLModelViewMatrix();
                var markerPosition = new DenseVector(new[] { -markerTransform[14], markerTransform[12] });
                var bearing = Math.Atan2(markerPosition[1], markerPosition[0]);
                var range = markerPosition.Norm(2);
                //System.Diagnostics.Trace.WriteLine(string.Format("mx: {0} my: {1} bearing:{2} range:{3}", markerPosition[0], markerPosition[1], bearing, range));

                int landmarkIndex;
                if (!landmarkIndices.TryGetValue(marker.Id, out landmarkIndex))
                {
                    landmarkIndex = landmarkIndices.Count;
                    landmarkIndices.Add(marker.Id, landmarkIndex);
                }

                return new LandmarkMeasurement(landmarkIndex, new DenseVector(new[] { range, bearing }));
            }).ToList();
        }

        public void UpdateMotion(double dx, double dtheta)
        {
            motion = new DenseVector(new[] { dx, dtheta });
            vehicle.Velocity = new Vector2((float)dx, (float)dtheta);
        }

        public void UpdateEstimate()
        {
            slam.Update(motion, measurements);

            // Wrap angle
            slam.Mean[2] = MathHelper.WrapAngle((float)slam.Mean[2]);
            vehicle.Transform.Position = new Vector2((float)slam.Mean[0], (float)slam.Mean[1]);
            vehicle.Transform.Rotation = (float)slam.Mean[2];
        }
    }
}
