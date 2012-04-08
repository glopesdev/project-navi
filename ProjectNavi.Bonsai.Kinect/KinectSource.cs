using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using Bonsai;
using OpenCV.Net;
using System.Threading;
using System.Runtime.InteropServices;

namespace ProjectNavi.Bonsai.Kinect
{
    public class KinectSource : Source<KinectFrame>
    {
        KinectSensor kinect;
        byte[] colorImageBuffer;

        protected override void Start()
        {
            kinect.Start();
        }

        protected override void Stop()
        {
            kinect.Stop();
        }

        public override IDisposable Load()
        {
            kinect = KinectSensor.KinectSensors.FirstOrDefault();
            kinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinect.SkeletonStream.Enable();
            kinect.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(kinect_AllFramesReady);
            return base.Load();
        }

        void kinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using(var colorFrame = e.OpenColorImageFrame())
            using (var skeletonFrame = e.OpenSkeletonFrame())
            {
                if (colorFrame != null && skeletonFrame != null)
                {
                    var bufferLength = colorFrame.Width * colorFrame.Height * colorFrame.BytesPerPixel;
                    if (colorImageBuffer == null || colorImageBuffer.Length != bufferLength)
                    {
                        colorImageBuffer = new byte[bufferLength];
                    }

                    var skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletonData);
                    colorFrame.CopyPixelDataTo(colorImageBuffer);
                    var bufferHandle = GCHandle.Alloc(colorImageBuffer, GCHandleType.Pinned);
                    var image = new IplImage(new CvSize(colorFrame.Width, colorFrame.Height), colorFrame.BytesPerPixel * 8 / 4, 4, bufferHandle.AddrOfPinnedObject());
                    var output = new IplImage(image.Size, image.Depth, 3);
                    ImgProc.cvCvtColor(image, output, ColorConversion.BGRA2BGR);
                    bufferHandle.Free();
                    Subject.OnNext(new KinectFrame(kinect, output, skeletonData));
                }
            }
        }

        protected override void Unload()
        {
            kinect.Dispose();
            base.Unload();
        }
    }
}
