﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Net.Mime;
using System.Net.Mail;
using System.Threading;

namespace Sacknet.KinectFacialRecognition
{
    /// <summary>
    /// Frame source that ties into the Kinect AllFramesReady event
    /// </summary>
    public class AllFramesReadyFrameSource : IFrameSource, IDisposable
    {
        private KinectSensor sensor;
        private int trackedSkeletonId = -1;
        private EmailAlert Alert = new EmailAlert();
        private bool FirstAlert = true;





        /// <summary>
        /// Initializes a new instance of the AllFramesReadyFrameSource class
        /// </summary>
        public AllFramesReadyFrameSource(KinectSensor sensor)
        {
            this.sensor = sensor;
            this.sensor.AllFramesReady += this.Sensor_AllFramesReady;
        }

        /// <summary>
        /// Raised when a new frame of data is available
        /// </summary>
        public event EventHandler<FrameData> FrameDataUpdated;

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            this.sensor.AllFramesReady -= this.Sensor_AllFramesReady;
        }

        /// <summary>
        /// Handles the Kinect AllFramesReady event
        /// </summary>
        private void Sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            ColorImageFrame colorFrame = null;
            DepthImageFrame depthFrame = null;
            Skeleton[] skeletonData;
            EmailAlert Alert = new EmailAlert();

            try
            {
                colorFrame = e.OpenColorImageFrame();
                depthFrame = e.OpenDepthImageFrame();

                using (var skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (colorFrame == null || depthFrame == null || skeletonFrame == null)
                        return;

                    skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletonData);
                }

                // Find a skeleton to track.
                // First see if our old one is good.
                // When a skeleton is in PositionOnly tracking state, don't pick a new one
                // as it may become fully tracked again.
                Skeleton skeletonOfInterest = skeletonData.FirstOrDefault(s => s.TrackingId == this.trackedSkeletonId && s.TrackingState != SkeletonTrackingState.NotTracked);

                if (skeletonOfInterest == null)
                {
                    // Old one wasn't around.  Find any skeleton that is being tracked and use it.
                    skeletonOfInterest = skeletonData.FirstOrDefault(s => s.TrackingState == SkeletonTrackingState.Tracked);

                    if (skeletonOfInterest != null)
                    {
                        this.trackedSkeletonId = skeletonOfInterest.TrackingId;

                        if (FirstAlert)
                        {
                            Alert.SendAlert(colorFrame);
                            FirstAlert = false;
                        }

                    }
                }

                if (this.FrameDataUpdated != null)
                {
                    this.FrameDataUpdated(this, new FrameData(colorFrame, depthFrame, skeletonOfInterest));
                }
            }
            finally
            {
                if (colorFrame != null)
                    {
                    colorFrame.Dispose();
                    }


                if (depthFrame != null)
                    depthFrame.Dispose();
            }
        }


     
        private Bitmap ImageToBitmap(byte[] buffer, int width, int height)
        {
            Bitmap bmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            BitmapData bmapdata = bmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;
            Marshal.Copy(buffer, 0, ptr, buffer.Length);
            bmap.UnlockBits(bmapdata);
            return bmap;
        }


    }
}





