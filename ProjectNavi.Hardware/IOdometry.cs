using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectNavi.Hardware
{
    public interface IOdometry
    {
        IObservable<OdometryMeasurement> Odometry { get; }

        void UpdateOdometryCommand();
    }
    
    public struct OdometryMeasurement
    {
        double linearDisplacement;
        double angularDisplacement;

        public OdometryMeasurement(double linearDisplacement, double angularDisplacement)
        {
            this.linearDisplacement = linearDisplacement;
            this.angularDisplacement = angularDisplacement;
        }

        public double LinearDisplacement
        {
            get { return linearDisplacement; }
        }

        public double AngularDisplacement
        {
            get { return angularDisplacement; }
        }
    }
}
