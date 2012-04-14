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
        short[] depthImageBuffer;
        byte[] colorImageBuffer;

        Thread captureThread;
        volatile bool running;
        ManualResetEventSlim stop;

        protected override void Start()
        {
            kinect.Start();

            running = true;
            captureThread = new Thread(CaptureNewFrame);
            captureThread.Start();
        }

        protected override void Stop()
        {
            running = false;
            stop.Wait();

            kinect.Stop();
        }

        public override IDisposable Load()
        {
            stop = new ManualResetEventSlim();
            kinect = KinectSensor.KinectSensors.FirstOrDefault();
            kinect.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            kinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinect.SkeletonStream.Enable();
            return base.Load();
        }

        void CaptureNewFrame()
        {
            while (running)
            {
                using (var depthFrame = kinect.DepthStream.OpenNextFrame(50))
                using (var colorFrame = kinect.ColorStream.OpenNextFrame(50))
                using (var skeletonFrame = kinect.SkeletonStream.OpenNextFrame(50))
                {
                    if (depthFrame != null && colorFrame != null && skeletonFrame != null)
                    {
                        // Keep depth as managed array
                        var depthImageBuffer = new short[depthFrame.PixelDataLength];
                        //if (depthImageBuffer == null || depthImageBuffer.Length != depthFrame.PixelDataLength)
                        //{
                        //    depthImageBuffer = new short[depthFrame.PixelDataLength];
                        //}

                        if (colorImageBuffer == null || colorImageBuffer.Length != colorFrame.PixelDataLength)
                        {
                            colorImageBuffer = new byte[colorFrame.PixelDataLength];
                        }

                        var skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                        skeletonFrame.CopySkeletonDataTo(skeletonData);
                        colorFrame.CopyPixelDataTo(colorImageBuffer);
                        depthFrame.CopyPixelDataTo(depthImageBuffer);

                        // Flip color image to get proper measurements
                        var bufferHandle = GCHandle.Alloc(colorImageBuffer, GCHandleType.Pinned);
                        var colorImage = new IplImage(new CvSize(colorFrame.Width, colorFrame.Height), colorFrame.BytesPerPixel * 8 / 4, 4, bufferHandle.AddrOfPinnedObject());
                        var colorOutput = new IplImage(colorImage.Size, colorImage.Depth, 3);
                        ImgProc.cvCvtColor(colorImage, colorOutput, ColorConversion.BGRA2BGR);
                        Core.cvFlip(colorOutput, colorOutput, FlipMode.Horizontal);
                        bufferHandle.Free();

                        //bufferHandle = GCHandle.Alloc(depthImageBuffer, GCHandleType.Pinned);
                        //var depthImage = new IplImage(new CvSize(depthFrame.Width, depthFrame.Height), depthFrame.BytesPerPixel * 8, 1, bufferHandle.AddrOfPinnedObject());
                        //var depthOutput = new IplImage(depthImage.Size, depthImage.Depth, 1);
                        //Core.cvCopy(depthImage, depthOutput);
                        //Core.cvFlip(depthOutput, depthOutput, FlipMode.Horizontal);
                        //bufferHandle.Free();

                        //**** Flip depth horizontally ****
                        //bufferHandle = GCHandle.Alloc(depthImageBuffer, GCHandleType.Pinned);
                        //var depthImage = new IplImage(new CvSize(depthFrame.Width, depthFrame.Height), depthFrame.BytesPerPixel * 8, 1, bufferHandle.AddrOfPinnedObject());
                        //Core.cvFlip(depthImage, depthImage, FlipMode.Horizontal);
                        //bufferHandle.Free();

                        Subject.OnNext(new KinectFrame(kinect, depthImageBuffer, colorOutput, skeletonData));
                    }
                }
            }

            stop.Set();
        }

        protected override void Unload()
        {
            kinect.Dispose();
            base.Unload();
        }
    }
}
