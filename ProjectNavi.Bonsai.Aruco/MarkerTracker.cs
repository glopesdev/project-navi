using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCV.Net;
using Bonsai;
using Aruco.Net;
using System.ComponentModel;

namespace ProjectNavi.Bonsai.Aruco
{
    public class MarkerTracker : Projection<IplImage, IplImage>
    {
        CvMat cameraMatrix;
        CvMat distortion;
        CameraParameters parameters;
        MarkerDetector detector;

        public MarkerTracker()
        {
            Param1 = 7.0;
            Param2 = 7.0;
            ThresholdType = ThresholdMethod.AdaptiveThreshold;
            CornerRefinement = CornerRefinementMethod.Harris;
        }

        [FileNameFilter("YML Files|*.yml;*.xml")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string CameraParameters { get; set; }

        public ThresholdMethod ThresholdType { get; set; }

        public double Param1 { get; set; }

        public double Param2 { get; set; }

        public CornerRefinementMethod CornerRefinement { get; set; }

        public float MarkerSize { get; set; }

        public override IplImage Process(IplImage input)
        {
            var hInput = input.DangerousGetHandle();
            var hCamMatrix = cameraMatrix != null ? cameraMatrix.DangerousGetHandle() : IntPtr.Zero;
            var hDistortion = distortion != null ? distortion.DangerousGetHandle() : IntPtr.Zero;
            detector.ThresholdType = ThresholdType;
            detector.Param1 = Param1;
            detector.Param2 = Param2;
            detector.CornerRefinement = CornerRefinement;

            var markers = detector.Detect(hInput, hCamMatrix, hDistortion, MarkerSize);
            var output = new IplImage(input.Size, 8, 3);
            Core.cvCopy(input, output);
            //detector.CopyThresholdedImage(output.DangerousGetHandle());
            foreach (var marker in markers)
            {
                marker.Draw(output.DangerousGetHandle(), 0, 0, 255, 2, true);
                if (parameters != null)
                {
                    CvDrawingUtils.Draw3dCube(output.DangerousGetHandle(), marker, parameters);
                }
            }
            return output;
        }

        public override IDisposable Load()
        {
            detector = new MarkerDetector();
            if (!string.IsNullOrEmpty(CameraParameters))
            {
                parameters = new CameraParameters();
                parameters.ReadFromXmlFile(CameraParameters);

                cameraMatrix = new CvMat(3, 3, CvMatDepth.CV_32F, 1);
                distortion = new CvMat(1, 4, CvMatDepth.CV_32F, 1);
                parameters.CopyParameters(cameraMatrix.DangerousGetHandle(), distortion.DangerousGetHandle());
            }
            return base.Load();
        }

        protected override void Unload()
        {
            detector.Dispose();
            base.Unload();
        }
    }
}
