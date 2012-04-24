using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectNavi.Entities
{
    public class SlamControllerState
    {
        LandmarkMappingState landmarkIndices = new LandmarkMappingState();

        public double[] Mean { get; set; }

        public double[] Covariance { get; set; }

        public double[] MotionNoise { get; set; }

        public double[] MeasurementNoise { get; set; }

        public LandmarkMappingState LandmarkIndices
        {
            get { return landmarkIndices; }
        }
    }
}
