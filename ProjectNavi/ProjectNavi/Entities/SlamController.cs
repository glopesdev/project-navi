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
        LandmarkMappingCollection landmarkIndices;
        Vehicle vehicle;

        Vector<double> motion;
        IEnumerable<LandmarkMeasurement> measurements;
        SlamControllerState memento;

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
                {1.0, 0},
                {0, 1.0}
            });

            landmarkIndices = new LandmarkMappingCollection();
            motion = new DenseVector(2);
            measurements = Enumerable.Empty<LandmarkMeasurement>();
            vehicle = agent;
        }

        public EkfSlam SlamEstimator
        {
            get { return slam; }
        }

        public int GetLandmarkId(int landmarkIndex)
        {
            return landmarkIndices.First(pair => pair.LandmarkIndex == landmarkIndex).MarkerId;
        }

        public Vector2? GetLandmarkPosition(int markerId)
        {
            if (!landmarkIndices.Contains(markerId))
            {
                return null;
            }

            var landmarkIndex = landmarkIndices[markerId].LandmarkIndex;
            var stateVectorIndex = EkfSlam.LandmarkDim * landmarkIndex + EkfSlam.StateDim;
            var landmarkX = slam.Mean[stateVectorIndex];
            var landmarkY = slam.Mean[stateVectorIndex + 1];
            return new Vector2((float)landmarkX, (float)landmarkY);
        }

        public void UpdateMeasurements(MarkerFrame markerFrame)
        {
            measurements = markerFrame.DetectedMarkers.Select(marker =>
            {
                var markerTransform = marker.GetGLModelViewMatrix();
                var markerPosition = new DenseVector(new[] { -markerTransform[14], markerTransform[12] });
                var bearing = -Math.Atan2(markerPosition[1], markerPosition[0]);
                var range = markerPosition.Norm(2);
                //System.Diagnostics.Trace.WriteLine(string.Format("mx: {0} my: {1} bearing:{2} range:{3}", markerPosition[0], markerPosition[1], bearing, range));

                if (!landmarkIndices.Contains(marker.Id))
                {
                    var nextIndex = landmarkIndices.Count;
                    landmarkIndices.Add(new LandmarkMapping { MarkerId = marker.Id, LandmarkIndex = nextIndex });
                }

                var landmarkIndex = landmarkIndices[marker.Id].LandmarkIndex;
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
            if (memento != null)
            {
                var covarianceOrder = (int)Math.Sqrt(memento.Covariance.Length);
                var motionNoiseOrder = (int)Math.Sqrt(memento.MotionNoise.Length);
                var measurementNoiseOrder = (int)Math.Sqrt(memento.MeasurementNoise.Length);
                slam.Mean = new DenseVector(memento.Mean);
                slam.Covariance = new DenseMatrix(covarianceOrder, covarianceOrder, memento.Covariance);
                slam.MotionNoise = new DenseMatrix(motionNoiseOrder, motionNoiseOrder, memento.MotionNoise);
                slam.MeasurementNoise = new DenseMatrix(measurementNoiseOrder, measurementNoiseOrder, memento.MeasurementNoise);

                landmarkIndices.Clear();
                foreach (var mapping in memento.LandmarkIndices)
                {
                    landmarkIndices.Add(mapping);
                }
                memento = null;
            }

            slam.Update(motion, measurements);

            // Wrap angle
            slam.Mean[2] = MathHelper.WrapAngle((float)slam.Mean[2]);
            vehicle.Transform.Position = new Vector2((float)slam.Mean[0], (float)slam.Mean[1]);
            vehicle.Transform.Rotation = (float)slam.Mean[2];
        }

        public SlamControllerState StoreControllerState()
        {
            var state = new SlamControllerState();
            state.Mean = slam.Mean.ToArray();
            state.Covariance = slam.Covariance.ToColumnWiseArray();
            state.MotionNoise = slam.MotionNoise.ToColumnWiseArray();
            state.MeasurementNoise = slam.MeasurementNoise.ToColumnWiseArray();

            foreach (var mapping in landmarkIndices)
            {
                state.LandmarkIndices.Add(mapping);
            }

            return state;
        }

        public void RestoreControllerState(SlamControllerState state)
        {
            memento = state;
        }
    }
}
