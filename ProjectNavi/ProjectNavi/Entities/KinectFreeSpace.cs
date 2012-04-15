using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectNavi.Bonsai.Kinect;

namespace ProjectNavi.Entities
{
    public static class KinectFreeSpace
    {
        public static bool[] ComputeFreeSpace(KinectFrame frame, float threshold)
        {
            if (frame != null)
            {
                var depthImage = frame.DepthImage;
                var frameWidth = frame.Sensor.DepthStream.FrameWidth;
                var frameHeight = frame.Sensor.DepthStream.FrameHeight;
                var result = new bool[frameWidth];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = true;
                }

                for (int i = 0; i < frameHeight; i += 1)
                {
                    for (int j = 0; j < frameWidth; j += 1)
                    {
                        // depth in millimeters
                        var depth = ((ushort)depthImage[i * frameWidth + (frameWidth - j - 1)]) >> 3;
                        if (depth < threshold)
                        {
                            result[j] = false;
                        }
                    }
                }

                return result;
            }

            return null;
        }
    }
}
