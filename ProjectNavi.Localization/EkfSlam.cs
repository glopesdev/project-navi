using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;

namespace ProjectNavi.Localization
{
    public class EkfSlam
    {
        public const int StateDim = 3;
        public const int LandmarkDim = 2;

        public EkfSlam()
        {
            Mean = new DenseVector(StateDim);
            Covariance = new DenseMatrix(StateDim);
        }

        public Vector<double> Mean { get; set; }

        public Matrix<double> Covariance { get; set; }

        public Matrix<double> MotionNoise { get; set; }

        public Matrix<double> MeasurementNoise { get; set; }

        public void Update<TMeasurements>(Vector<double> motion, TMeasurements measurements) where TMeasurements : IEnumerable<LandmarkMeasurement>
        {
            // expand kalman filter to accomodate new landmarks, if necessary
            var prevMeanCount = Mean.Count;
            var maxIndex = measurements.Max(m => (int?)m.Index);
            if (maxIndex != null)
            {
                var m = LandmarkDim * maxIndex.Value + StateDim;
                if (m >= prevMeanCount)
                {
                    var mean = new DenseVector(m + LandmarkDim);
                    var covariance = new DenseMatrix(m + LandmarkDim);
                    covariance.SetDiagonal((Vector<double>)new DenseVector(m + LandmarkDim, 10000000));
                    Mean.CopyTo(mean, 0, 0, Mean.Count);
                    covariance.SetSubMatrix(0, Covariance.RowCount, 0, Covariance.ColumnCount, Covariance);
                    Mean = mean;
                    Covariance = covariance;
                }
            }

            var N = (Mean.Count - StateDim) / LandmarkDim;
            var Fx = new DenseMatrix(StateDim, StateDim + LandmarkDim * N);
            for (int i = 0; i < StateDim; i++)
            {
                Fx[i, i] = 1;
            }

            var motionModel = new DenseVector(new[] { motion[0] * Math.Cos(Mean[2]), motion[0] * Math.Sin(Mean[2]), motion[1] });
            var motionJacobian = new DenseMatrix(new[,]
            {
                { 0, 0, -motion[0] * Math.Sin(Mean[2]) },
                { 0, 0, motion[0] * Math.Cos(Mean[2]) },
                { 0, 0, 0 }
            });

            // Integrate motion
            var identity = DenseMatrix.Identity(Mean.Count);
            Mean += Fx.TransposeThisAndMultiply(motionModel.ToColumnMatrix()).Column(0);
            var jacobian = identity + Fx.TransposeThisAndMultiply(motionJacobian) * Fx;
            Covariance = jacobian * Covariance.TransposeAndMultiply(jacobian) + Fx.TransposeThisAndMultiply(MotionNoise) * Fx;

            // Auxiliary variables
            var positionEstimate = new DenseVector(new[] { Mean[0], Mean[1] });
            var Fxj = new DenseMatrix(StateDim + LandmarkDim, LandmarkDim * N + StateDim);
            for (int i = 0; i < StateDim; i++)
            {
                Fxj[i, i] = 1;
            }

            // Integrate measurements
            foreach (var measurement in measurements)
            {
                // m is the corrected index of the landmark
                var m = LandmarkDim * measurement.Index + StateDim;

                if (m >= prevMeanCount)
                {
                    // landmark never seen before
                    Mean[m] = Mean[0] + measurement.Measurement[0] * Math.Cos(measurement.Measurement[1] + Mean[2]);
                    Mean[m + 1] = Mean[1] + measurement.Measurement[0] * Math.Sin(measurement.Measurement[1] + Mean[2]);
                }

                var landmarkEstimate = new DenseVector(new[] { Mean[m], Mean[m + 1] });
                var landmarkDelta = landmarkEstimate - positionEstimate;
                var q = landmarkDelta.DotProduct(landmarkDelta);
                var sqrtQ = Math.Sqrt(q);
                var measurementModel = new DenseVector(new[] { sqrtQ, Math.Atan2(landmarkDelta[1], landmarkDelta[0]) - Mean[2] });
                for (int i = 0; i < LandmarkDim; i++)
                {
                    Fxj[StateDim + i, m + i] = 1;
                }

                var measurementJacobian = (1 / q) * new DenseMatrix(new[,]
                {
                    { -sqrtQ * landmarkDelta[0], -sqrtQ * landmarkDelta[1], 0, sqrtQ * landmarkDelta[0], sqrtQ * landmarkDelta[1] },
                    { landmarkDelta[1], -landmarkDelta[0], -q, -landmarkDelta[1], landmarkDelta[0] }
                }) * Fxj;

                var kalmanGain = Covariance * measurementJacobian.TransposeThisAndMultiply((measurementJacobian * Covariance.TransposeAndMultiply(measurementJacobian) + MeasurementNoise).Inverse());
                Mean += kalmanGain * (measurement.Measurement - measurementModel);
                Covariance = (identity - kalmanGain * measurementJacobian) * Covariance;

                for (int i = 0; i < LandmarkDim; i++)
                {
                    Fxj[StateDim + i, m + i] = 0;
                }
            }
        }
    }

    public class LandmarkMeasurement
    {
        public LandmarkMeasurement(int index, Vector<double> measurement)
        {
            Index = index;
            Measurement = measurement;
        }

        public int Index { get; private set; }

        public Vector<double> Measurement { get; private set; }
    }
}
