using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;

namespace ProjectNavi.Localization
{
    public class KalmanFilter
    {
        public Vector<double> Mean { get; set; }

        public Matrix<double> Covariance { get; set; }

        public void Predict(Vector<double> control, StateTransitionFunc stateTransition, StateTransitionJacobianFunc stateTransitionJacobian, Matrix<double> noise)
        {
            var jacobian = stateTransitionJacobian(control, Mean);
            Mean = stateTransition(control, Mean);
            Covariance = jacobian * Covariance * jacobian.Transpose() + noise;
        }

        public void Update(Vector<double> measurement, MeasurementFunc measurementFunction, MeasurementJacobianFunc measurementFunctionJacobian, Matrix<double> noise)
        {
            var jacobian = measurementFunctionJacobian(Mean);
            var kalmanGain = Covariance * jacobian.Transpose() * (jacobian * Covariance * jacobian.Transpose() + noise).Inverse();
            Mean = Mean + kalmanGain * (measurement - measurementFunction(Mean));
            Covariance = (DenseMatrix.Identity(Mean.Count) - kalmanGain * jacobian) * Covariance;
        }
    }

    public delegate Vector<double> StateTransitionFunc(Vector<double> control, Vector<double> mean);

    public delegate Matrix<double> StateTransitionJacobianFunc(Vector<double> control, Vector<double> mean);

    public delegate Vector<double> MeasurementFunc(Vector<double> mean);

    public delegate Matrix<double> MeasurementJacobianFunc(Vector<double> mean);
}
