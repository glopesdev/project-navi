using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCV.Net;
using Microsoft.Kinect;

namespace ProjectNavi.Bonsai.Kinect
{
    public class KinectFrame
    {
        public KinectFrame(KinectSensor sensor, IplImage colorImage, Skeleton[] skeletonData)
        {
            Sensor = sensor;
            ColorImage = colorImage;
            SkeletonData = skeletonData;
        }

        public KinectSensor Sensor { get; private set; }

        public IplImage ColorImage { get; private set; }

        public Skeleton[] SkeletonData { get; private set; }
    }
}
