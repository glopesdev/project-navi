using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ProjectNavi.Localization
{
    public class MarkerLocalization
    {
        public Vector<double> MarkerPosition { get; set; }

        public Vector<double> SensorOffset { get; set; }

        public Vector<double> Measurement(Vector<double> mean)
        {
            // Compute position of marker relative to sensor position
            // mean - {x, y, theta}
            // h - {x', y'}
            // measurement = marker_pos - sensor_pos
            // sensor_x = x + rs * cos(theta + theta_s)
            // sensor_y = y + rs * sin(theta + theta_s)
            var result = new DenseVector(2);
            result[0] = MarkerPosition[0] - (mean[0] + SensorOffset[0] * Math.Cos(mean[2] + SensorOffset[1]));
            result[1] = MarkerPosition[1] - (mean[1] + SensorOffset[0] * Math.Sin(mean[2] + SensorOffset[1]));
            return result;
        }

        public Matrix<double> MeasurementJacobian(Vector<double> mean)
        {
            var jacobian = new[,] {{-1, 0, SensorOffset[0] * -Math.Sin(mean[2] + SensorOffset[1])},
                                   {0, -1, SensorOffset[0] * Math.Cos(mean[2] + SensorOffset[1])}};
            return new DenseMatrix(jacobian);
        }

        public void MarkerUpdate(KalmanFilter kalman, Vector<double> measurement)
        {
            var noise = new DenseMatrix(new double[,]{{2000, 0},
                                         {0, 2000},});
            kalman.Update(measurement, Measurement, MeasurementJacobian, noise);
        }
    }
}
