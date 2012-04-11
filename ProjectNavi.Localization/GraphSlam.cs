using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ProjectNavi.Localization
{
    public class GraphSlam
    {
        public Matrix<double> Omega { get; set; }

        public Matrix<double> Xi { get; set; }

        public GraphSlam()
        {
            Omega = new DenseMatrix(2);
            Omega[0, 0] = 1;
            Omega[1, 1] = 2;

            Xi = new DenseMatrix(2, 1);
            Xi[0, 0] = 0;
            Xi[1, 0] = 0;
        }

        public void Update<TMeasurements>(Vector<double> motion, TMeasurements measurements, double motionNoise, double measurementNoise) where TMeasurements : IEnumerable<LandmarkMeasurement>
        {
            // expand information matrix to accomodate new landmarks, if necessary
            var maxIndex = measurements.Max(m => (int?)m.Index);
            if (maxIndex != null)
            {
                var m = 2 * (1 + maxIndex.Value);
                if (m >= Omega.RowCount)
                {
                    var indices = Enumerable.Range(0,Omega.RowCount).ToArray();
                    Omega = Omega.Expand(m + 2, m + 2, indices);
                    Xi = Xi.Expand(m + 2, 1, indices, new[] { 0 });
                }
            }

            var expandedSize = Omega.RowCount + 2;
            var expandedIndices = new int[Omega.RowCount];
            var reducedIndices = new int[Omega.RowCount];
            for (int i = 0; i < expandedIndices.Length; i++)
            {
                if (i < 2) expandedIndices[i] = i;
                else expandedIndices[i] = i + 2;

                reducedIndices[i] = i + 2;
            }

            // expand omega and xi for motion
            Omega = Omega.Expand(expandedSize, expandedSize, expandedIndices);
            Xi = Xi.Expand(expandedSize, 1, expandedIndices, new[] { 0 });

            // integrate the measurements
            foreach (var measurement in measurements)
            {
                // m is the corrected index of the landmark
                var m = 2 * (2 + measurement.Index);

                // update information matrix based on the measurement
                for (int i = 0; i < 2; i++)
                {
                    Omega[i, i] += 1.0 / measurementNoise;
                    Omega[m + i, m + i] += 1.0 / measurementNoise;
                    Omega[i, m + i] += -1.0 / measurementNoise;
                    Omega[m + i, i] += -1.0 / measurementNoise;
                    Xi[i, 0] += -measurement.Measurement[i] / measurementNoise;
                    Xi[m + i, 0] += measurement.Measurement[i] / measurementNoise;
                }
            }

            // update information matrix based on motion
            for (int i = 0; i < 4; i++)
            {
                Omega[i, i] += 1.0 / motionNoise;
                if (i < 2)
                {
                    Omega[i, i + 2] += -1.0 / motionNoise;
                    Omega[i + 2, i] += -1.0 / motionNoise;
                    Xi[i, 0] += -motion[i] / motionNoise;
                    Xi[i + 2, 0] += motion[i] / motionNoise;
                }
            }

            // reduce omega and xi
            var omegaPrime = Omega.Take(reducedIndices);
            var xiPrime = Xi.Take(reducedIndices, new[] { 0 });

            var A = Omega.Take(new[] { 0, 2 }, reducedIndices);
            var B = Omega.Take(new[] { 0, 2 });
            var C = Xi.Take(new[] { 0, 2 }, new[] { 0 });

            var AtBinv = A.TransposeThisAndMultiply(B.Inverse());
            omegaPrime = omegaPrime - (AtBinv * A);
            xiPrime = xiPrime - (AtBinv * C);

            Omega = omegaPrime;
            Xi = xiPrime;
        }
    }
}
