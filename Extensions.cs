using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using KinectStreams;
using System.Diagnostics;
using System.Drawing;

namespace KinectStreams
{
    public static class Extensions
    {
        // global variables
        
        static int rgbImageCount = 0;
        static int depthImageCount = 0;
        static int infraredImageCount = 0;



        static int frameWidth = -1;
        static int frameHeight = -1;

        static int outputFrameWidth = 720;
        static int outputFrameHeight = 480;

        private static double percentage = 20;

        #region Camera

        public static ImageSource ToBitmap(this ColorFrame frame, string pathToRgbFolder, LabelMode label, Boolean saveImage, int session)
        {
            int width = frameWidth != -1 ? frameWidth : frame.FrameDescription.Width;
            int height = frameHeight != -1 ? frameHeight : frame.FrameDescription.Height;

            // Changing resolution is as simple as changing aspect

            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((format.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = (int) width * format.BitsPerPixel / 8;

            BitmapSource image = BitmapSource.Create( width , height , 96, 96, format, null, pixels, stride);
            if (saveImage) {
                SaveImage(pathToRgbFolder + "\\" + session + "\\" + LabelToFilePath(label) + "\\" + rgbImageCount, image);
                rgbImageCount++;
            }

            return image;
        }

        public static ImageSource ToBitmap(this DepthFrame frame, string pathToDepthFolder, LabelMode label, Boolean saveImage, int session)
        {
            int width = frameWidth != -1 ? frameWidth : frame.FrameDescription.Width;
            int height = frameHeight != -1 ? frameHeight : frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            ushort[] pixelData = new ushort[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(pixelData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < pixelData.Length; ++depthIndex)
            {
                ushort depth = pixelData[depthIndex];

                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                pixels[colorIndex++] = intensity; // Blue
                pixels[colorIndex++] = intensity; // Green
                pixels[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            BitmapSource image = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
            if (saveImage) {
                SaveImage(pathToDepthFolder + "\\" + session + "\\" + LabelToFilePath(label) + "\\" + depthImageCount, image);
                depthImageCount++;
            }

            return image;
        }

        public static ImageSource ToBitmap(this InfraredFrame frame, string pathToInfraredFolder, LabelMode label, Boolean saveImage, int session)
        {
            int width = frameWidth != -1 ? frameWidth : frame.FrameDescription.Width;
            int height = frameHeight != -1 ? frameHeight : frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort[] frameData = new ushort[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(frameData);

            int colorIndex = 0;
            for (int infraredIndex = 0; infraredIndex < frameData.Length; infraredIndex++)
            {
                ushort ir = frameData[infraredIndex];

                byte intensity = (byte)(ir >> 7);

                pixels[colorIndex++] = (byte)(intensity / 1); // Blue
                pixels[colorIndex++] = (byte)(intensity / 1); // Green   
                pixels[colorIndex++] = (byte)(intensity / 0.4); // Red

                colorIndex++;
            }

            int stride = width * format.BitsPerPixel / 8;

            BitmapSource image = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
            if (saveImage) {
                SaveImage(pathToInfraredFolder + "\\" + session + "\\" + LabelToFilePath(label) + "\\" + infraredImageCount, image);
                infraredImageCount++;
            }

            return image;
        }

        #endregion

        #region Body

        public static Joint ScaleTo(this Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY)
        {
            joint.Position = new CameraSpacePoint
            {
                X = Scale(width, skeletonMaxX, joint.Position.X),
                Y = Scale(height, skeletonMaxY, -joint.Position.Y),
                Z = joint.Position.Z
            };

            return joint;
        }

        public static Joint ScaleTo(this Joint joint, double width, double height)
        {
            return ScaleTo(joint, width, height, 1.0f, 1.0f);
        }

        private static float Scale(double maxPixel, double maxSkeleton, float position)
        {
            float value = (float)((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

            if (value > maxPixel)
            {
                return (float)maxPixel;
            }

            if (value < 0)
            {
                return 0;
            }

            return value;
        }

        #endregion

        #region Drawing

        public static void DrawSkeleton(this Canvas canvas, Body body)
        {
            if (body == null) return;

            foreach (Joint joint in body.Joints.Values)
            {
                canvas.DrawPoint(joint);
            }

            canvas.DrawLine(body.Joints[JointType.Head], body.Joints[JointType.Neck]);
            canvas.DrawLine(body.Joints[JointType.Neck], body.Joints[JointType.SpineShoulder]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderLeft]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderRight]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.SpineMid]);
            canvas.DrawLine(body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft]);
            canvas.DrawLine(body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight]);
            canvas.DrawLine(body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft]);
            canvas.DrawLine(body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight]);
            canvas.DrawLine(body.Joints[JointType.WristLeft], body.Joints[JointType.HandLeft]);
            canvas.DrawLine(body.Joints[JointType.WristRight], body.Joints[JointType.HandRight]);
            canvas.DrawLine(body.Joints[JointType.HandLeft], body.Joints[JointType.HandTipLeft]);
            canvas.DrawLine(body.Joints[JointType.HandRight], body.Joints[JointType.HandTipRight]);
            canvas.DrawLine(body.Joints[JointType.HandTipLeft], body.Joints[JointType.ThumbLeft]);
            canvas.DrawLine(body.Joints[JointType.HandTipRight], body.Joints[JointType.ThumbRight]);
            canvas.DrawLine(body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase]);
            canvas.DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipLeft]);
            canvas.DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipRight]);
            canvas.DrawLine(body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft]);
            canvas.DrawLine(body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight]);
            canvas.DrawLine(body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft]);
            canvas.DrawLine(body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight]);
            canvas.DrawLine(body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft]);
            canvas.DrawLine(body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight]);
        }

        public static void DrawPoint(this Canvas canvas, Joint joint)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return;

            joint = joint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(Colors.LightBlue)
            };

            Canvas.SetLeft(ellipse, joint.Position.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, joint.Position.Y - ellipse.Height / 2);

            canvas.Children.Add(ellipse);
        }

        public static void DrawLine(this Canvas canvas, Joint first, Joint second)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            first = first.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);
            second = second.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

            Line line = new Line
            {
                X1 = first.Position.X,
                Y1 = first.Position.Y,
                X2 = second.Position.X,
                Y2 = second.Position.Y,
                StrokeThickness = 8,
                Stroke = new SolidColorBrush(Colors.LightBlue)
            };

            canvas.Children.Add(line);
        }

        #endregion


        public static void SaveImage(string filePath, BitmapSource image)
        {
            filePath += ".png";

            // Create the directory if it doesn't exist
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
            // Change Resolution here
            if (filePath.Contains("RGB"))
            {
                //Bitmap Bmp = new Bitmap(filePath);
                //float tempWidth = Bmp.Width;
                //float tempHeight = Bmp.Height;

                //int width = (int)(tempWidth * percentage / 100);
                //int height = (int)(tempHeight * percentage / 100);
                //Bitmap Bmp2 = new Bitmap(Bmp, width , height);
                //Bmp.Dispose();
                //File.Delete(filePath);
                //Bmp2.Save(filePath);
                //Bmp2.Dispose();
                changeResolution(filePath);
            }
        }

        private static string LabelToFilePath(LabelMode label)
        {
            if(label == LabelMode.WASH)
            {
                return "Wash";
            }
            else if(label == LabelMode.CONTACT)
            {
                return "Contact";
            }
            else
            {
                return "None";
            }
        }

        static Bitmap LoadImage(string filePath)
        {
            return (Bitmap)Bitmap.FromFile(filePath); // Original Kinect RGB size 1280X1024
        }

        public static void changeResolution(string filePath)
        {
            using (var absentRectangleImage = LoadImage(filePath))
            {
                absentRectangleImage.SetResolution(24, 24);
                using (var currentTile = new Bitmap(1476, 1080, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                {
                    currentTile.SetResolution(absentRectangleImage.HorizontalResolution, absentRectangleImage.VerticalResolution);

                    using (Graphics currentTileGraphics = Graphics.FromImage(currentTile))
                    {
                        currentTileGraphics.Clear(System.Drawing.Color.Black);
                        var absentRectangleArea = new System.Drawing.Rectangle(240, 0, 1476, 1080); // (top left x-coordinate, top left y-coordinate, width, height)
                        // Quick maths: x = (1900-512)/2; y=(1080-424)/2;  -- This doesn't work :( 
                        currentTileGraphics.DrawImage(absentRectangleImage, 0, 0, absentRectangleArea, GraphicsUnit.Pixel);
                    }

                    // Delete the file before saving it
                    // File.Delete(filePath);
                    absentRectangleImage.Dispose();
                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    Bitmap dstImage = ResizeImage(currentTile, 512, 424);
                    dstImage.Save(filePath);
                    //currentTile.Save(filePath);
                    currentTile.Dispose();
                    dstImage.Dispose();
                }

            }
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            var destRect = new System.Drawing.Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                using (var wrapMode = new System.Drawing.Imaging.ImageAttributes())
                {
                    wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }


    }
}
