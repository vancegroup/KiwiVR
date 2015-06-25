﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Interaction;
//using KinectBase;

namespace KinectV1Core
{
    public class KinectCoreV1 : KinectBase.IKinectCore
    {
        internal KinectSensor kinect;
        public int kinectID { get; set; }  //This is the index of the Kinect options in the Kinect settings list
        public string uniqueKinectID
        {
            get
            {
                if (kinect != null)
                {
                    return kinect.UniqueKinectId;
                }
                else
                {
                    return null;
                }
            }
        }
        public KinectBase.KinectVersion version
        {
            get { return KinectBase.KinectVersion.KinectV1; }
        }
        public bool ColorStreamEnabled
        {
            get { return isColorStreamOn; }
        }
        public bool DepthStreamEnabled
        {
            get { return isDepthStreamOn; }
        }

        internal KinectBase.MasterSettings masterSettings;
        public int skelcount;
        private InteractionStream interactStream;
        private System.Timers.Timer updateTimer;
        private List<HandGrabInfo> skeletonHandGrabData = new List<HandGrabInfo>();
        private Matrix3D skeletonTransformation = Matrix3D.Identity;
        private Quaternion skeletonRotQuaternion = Quaternion.Identity;
        private Vector4 lastAcceleration;
        private bool isColorStreamOn = false;
        private bool isDepthStreamOn = false;
        public bool? isXbox360Kinect = null;
        private bool isGUI = false;
        private System.IO.Stream audioStream = null;

        //Event declarations
        public event KinectBase.SkeletonEventHandler SkeletonChanged;
        public event KinectBase.DepthFrameEventHandler DepthFrameReceived;
        public event KinectBase.ColorFrameEventHandler ColorFrameReceived;
        public event KinectBase.AudioPositionEventHandler AudioPositionChanged;
        public event KinectBase.AccelerationEventHandler AccelerationChanged;
        public event KinectBase.LogMessageEventHandler LogMessageGenerated;

        public KinectCoreV1(ref KinectBase.MasterSettings settings, bool isGUILaunched, int? kinectNumber = null)  //Why is the kinectNumber nullable if it is requried later?
        {
            if (kinectNumber != null)
            {
                masterSettings = settings;

                if (KinectSensor.KinectSensors.Count > kinectNumber)
                {
                    //Get the sensor index in the global list
                    int globalIndex = -1;
                    for (int i = 0; i < KinectSensor.KinectSensors.Count; i++)
                    {
                        if (KinectSensor.KinectSensors[i].UniqueKinectId == ((KinectV1Settings)masterSettings.kinectOptionsList[(int)kinectNumber]).uniqueKinectID)
                        {
                            globalIndex = i;
                            break;
                        }
                    }
                    kinect = KinectSensor.KinectSensors[globalIndex];
                    kinectID = (int)kinectNumber;
                }
                else
                {
                    throw new System.IndexOutOfRangeException("Specified Kinect sensor does not exist");
                }

                if (isGUILaunched)
                {
                    isGUI = true;
                    LaunchKinect();
                }
                else
                {
                    launchKinectDelegate kinectDelegate = LaunchKinect;
                    IAsyncResult result = kinectDelegate.BeginInvoke(null, null);
                    kinectDelegate.EndInvoke(result);  //Even though this is blocking, the events should be on a different thread now.
                }
            }
            else
            {
                //TODO: Open the default Kinect?
                throw new NullReferenceException("To create a KinectCore object, the KinectNumber must be valid.");
            }
        }
        public void ShutdownSensor()
        {
            if (kinect != null)
            {
                //The "new" syntax is sort of odd, but these really do remove the handlers from the specified events
                kinect.ColorFrameReady -= kinect_ColorFrameReady;
                kinect.DepthFrameReady -= kinect_DepthFrameReady;
                kinect.SkeletonFrameReady -= kinect_SkeletonFrameReady;
                interactStream.InteractionFrameReady -= interactStream_InteractionFrameReady;
                if (updateTimer != null)
                {
                    updateTimer.Stop();
                    updateTimer.Elapsed -= updateTimer_Elapsed;
                    updateTimer.Dispose();
                }

                interactStream.Dispose();
                interactStream = null;

                if (kinect.AudioSource != null)
                {
                    if (audioStream != null)
                    {
                        audioStream.Close();
                        audioStream.Dispose();
                    }

                    kinect.AudioSource.Stop();
                }
                kinect.Stop();
            }
        }
        public void StartKinectAudio()
        {
            if (isGUI)
            {
                ActuallyStartAudio();
            }
            else
            {
                //Launch the audio on a seperate thread if it is in console mode (otherwise the events never get thrown successfully)
                startAudioDelegate audioDelegate = ActuallyStartAudio;
                IAsyncResult result = audioDelegate.BeginInvoke(null, null);
                audioDelegate.EndInvoke(result);
            }
        }
        private void ActuallyStartAudio()
        {
            if (kinect.IsRunning)
            {
                //Start the audio streams, if necessary -- NOTE: This must be after the skeleton stream is started (which it should be here)
                if (((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).sendAudioAngle || masterSettings.audioOptions.sourceID == kinectID)
                {
                    if (masterSettings.audioOptions.sourceID == kinectID)
                    {
                        kinect.AudioSource.EchoCancellationMode = (EchoCancellationMode)masterSettings.audioOptions.echoMode;
                        kinect.AudioSource.AutomaticGainControlEnabled = masterSettings.audioOptions.autoGainEnabled;
                        kinect.AudioSource.NoiseSuppression = masterSettings.audioOptions.noiseSurpression;
                        if (((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).sendAudioAngle)
                        {
                            if (((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).audioTrackMode != KinectBase.AudioTrackingMode.Loudest)
                            {
                                kinect.AudioSource.BeamAngleMode = BeamAngleMode.Manual;
                            }
                            else
                            {
                                kinect.AudioSource.BeamAngleMode = BeamAngleMode.Automatic;
                            }
                            kinect.AudioSource.SoundSourceAngleChanged += AudioSource_SoundSourceAngleChanged;
                        }
                    }
                    else if (((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).sendAudioAngle)
                    {
                        kinect.AudioSource.EchoCancellationMode = Microsoft.Kinect.EchoCancellationMode.None;
                        kinect.AudioSource.AutomaticGainControlEnabled = false;
                        kinect.AudioSource.NoiseSuppression = true;
                        if (((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).audioTrackMode != KinectBase.AudioTrackingMode.Loudest)
                        {
                            kinect.AudioSource.BeamAngleMode = BeamAngleMode.Manual;
                        }
                        else
                        {
                            kinect.AudioSource.BeamAngleMode = BeamAngleMode.Automatic;
                        }
                        kinect.AudioSource.SoundSourceAngleChanged += AudioSource_SoundSourceAngleChanged;
                    }

                    audioStream = kinect.AudioSource.Start();
                }
            }
        }
        public System.IO.Stream GetKinectAudioStream()
        {
            if (kinect.AudioSource != null)
            {
                return audioStream;
            }
            else
            {
                return null;
            }
        }

        public void ChangeColorResolution(ColorImageFormat newResolution)
        {
            kinect.ColorStream.Disable();
            if (newResolution != ColorImageFormat.Undefined)
            {
                kinect.ColorStream.Enable(newResolution);
                isColorStreamOn = true;
            }
            else
            {
                isColorStreamOn = false;
            }
        }
        public void ChangeDepthResolution(DepthImageFormat newResolution)
        {
            kinect.DepthStream.Disable();
            if (newResolution != DepthImageFormat.Undefined)
            {
                kinect.DepthStream.Enable(newResolution);
                isDepthStreamOn = true;
            }
            else
            {
                isDepthStreamOn = false;
            }
        }
        public void UpdateAudioAngle(Point3D position)
        {
            if (kinect.AudioSource != null)
            {
                if (((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).audioTrackMode == KinectBase.AudioTrackingMode.Feedback)
                {
                    double angle = Math.Atan2(position.X - ((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).kinectPosition.X, position.Z - ((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).kinectPosition.Z) * (180.0 / Math.PI);
                    kinect.AudioSource.ManualBeamAngle = angle; //This will be rounded automatically to the nearest 10 degree increment, in the range -50 to 50 degrees
                }
            }
        }
        public KinectBase.KinectSkeleton TransformSkeleton(KinectBase.KinectSkeleton skeleton)
        {
            KinectBase.KinectSkeleton transformedSkeleton = new KinectBase.KinectSkeleton();
            transformedSkeleton.leftHandClosed = skeleton.leftHandClosed;
            transformedSkeleton.rightHandClosed = skeleton.rightHandClosed;
            transformedSkeleton.TrackingId = skeleton.TrackingId;
            transformedSkeleton.SkeletonTrackingState = skeleton.SkeletonTrackingState;
            transformedSkeleton.utcSampleTime = skeleton.utcSampleTime;
            transformedSkeleton.sourceKinectID = skeleton.sourceKinectID;
            transformedSkeleton.Position = skeletonTransformation.Transform(skeleton.Position);

            //Transform the joints
            for (int i = 0; i < skeleton.skeleton.Count; i++)
            {
                transformedSkeleton.skeleton[i] = TransformJoint(skeleton.skeleton[i]);
            }

            return transformedSkeleton;
        }
        public KinectBase.Joint TransformJoint(KinectBase.Joint joint)
        {
            KinectBase.Joint transformedJoint = new KinectBase.Joint();
            transformedJoint.Confidence = joint.Confidence;
            transformedJoint.JointType = joint.JointType;
            transformedJoint.TrackingState = joint.TrackingState;
            transformedJoint.Orientation = skeletonRotQuaternion * joint.Orientation;
            transformedJoint.Position = skeletonTransformation.Transform(joint.Position);

            return transformedJoint;
        }
        public Point MapJointToColor(KinectBase.Joint joint, bool undoTransform)
        {
            Point mappedPoint = new Point(0, 0);
            Point3D transformedPosition = joint.Position;

            if (undoTransform)
            {
                Matrix3D inverseTransform = skeletonTransformation;
                inverseTransform.Invert();
                transformedPosition = inverseTransform.Transform(transformedPosition);
            }

            SkeletonPoint skelPoint = new SkeletonPoint();
            skelPoint.X = (float)transformedPosition.X;
            skelPoint.Y = (float)transformedPosition.Y;
            skelPoint.Z = (float)transformedPosition.Z;
            ColorImagePoint point = kinect.CoordinateMapper.MapSkeletonPointToColorPoint(skelPoint, kinect.ColorStream.Format);
            mappedPoint.X = point.X;
            mappedPoint.Y = point.Y;

            return mappedPoint;
        }
        public Point MapJointToDepth(KinectBase.Joint joint, bool undoTransform)
        {
            Point mappedPoint = new Point(0, 0);
            Point3D transformedPosition = joint.Position;

            if (undoTransform)
            {
                Matrix3D inverseTransform = skeletonTransformation;
                inverseTransform.Invert();
                transformedPosition = inverseTransform.Transform(transformedPosition);
            }

            SkeletonPoint skelPoint = new SkeletonPoint();
            skelPoint.X = (float)transformedPosition.X;
            skelPoint.Y = (float)transformedPosition.Y;
            skelPoint.Z = (float)transformedPosition.Z;
            DepthImagePoint point = kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(skelPoint, kinect.DepthStream.Format);
            mappedPoint.X = point.X;
            mappedPoint.Y = point.Y;

            return mappedPoint;
        }

        private void LaunchKinect()
        {
            //Setup default properties
            if (((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).colorImageMode != ColorImageFormat.Undefined)
            {
                kinect.ColorStream.Enable(((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).colorImageMode);
                kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
                isColorStreamOn = true;

                //Check to see if the Kinect is a Kinect for Windows or a Xbox 360 Kinect so options can be enabled accordingly
                try
                {
                    ColorCameraSettings test = kinect.ColorStream.CameraSettings;
                    test = null;
                    isXbox360Kinect = false;
                }
                catch
                {
                    isXbox360Kinect = true;
                }
            }
            if (((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).depthImageMode != DepthImageFormat.Undefined)
            {
                kinect.DepthStream.Enable();
                isDepthStreamOn = true;

                kinect.SkeletonStream.Enable(); //Note, the audio stream MUST be started AFTER this (known issue with SDK v1.7).  Currently not an issue as the audio isn't started until the server is launched later in the code.
                kinect.SkeletonStream.EnableTrackingInNearRange = true; //Explicitly enable depth tracking in near mode (this can be true when the depth mode is near or default, but if it is false, there is no skeleton data in near mode)
                
                //Create the skeleton data container
                if (skeletonHandGrabData == null)
                {
                    skeletonHandGrabData = new List<HandGrabInfo>();
                }
                else
                {
                    skeletonHandGrabData.Clear();
                }
                
                interactStream = new InteractionStream(kinect, new DummyInteractionClient());
                kinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(kinect_DepthFrameReady);
                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
                kinect.SkeletonStream.EnableTrackingInNearRange = true;
                interactStream.InteractionFrameReady += new EventHandler<InteractionFrameReadyEventArgs>(interactStream_InteractionFrameReady);
            }

            kinect.Start();

            StartUpdateTimer();
        }
        private void StartUpdateTimer()
        {
            updateTimer = new System.Timers.Timer();
            updateTimer.AutoReset = true;
            updateTimer.Interval = 33.333;
            updateTimer.Elapsed += updateTimer_Elapsed;
            updateTimer.Start();
        }
        //Updates the acceleration on the GUI and the server, 30 FPS may be a little fast for the GUI, but for VRPN, it probably needs to be at least that fast
        private void updateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Update the acceleration data
            bool dataValid = false;
            Vector4? acceleration = null;
            int? elevationAngle = null;
            lock (kinect)
            {
                if (kinect.IsRunning)
                {
                    //I wish these try/catch statements weren't necessary, but these two calls seemed to failed often
                    dataValid = true;
                    try
                    {
                        acceleration = kinect.AccelerometerGetCurrentReading();
                    }
                    catch
                    {
                        acceleration = null;
                        dataValid = false;
                    }

                    if (dataValid)  //We can't even try to calculate the elevation angle if the accelerometer doesn't read right
                    {
                        try
                        {
                            elevationAngle = kinect.ElevationAngle;
                        }
                        catch
                        {
                            elevationAngle = null;
                            dataValid = false;
                        }
                    }
                }
            }

            //Update the GUI
            if (dataValid)
            {
                //Transmits the acceleration data using an event
                lastAcceleration = acceleration.Value;
                KinectBase.AccelerationEventArgs accelE = new KinectBase.AccelerationEventArgs();
                accelE.kinectID = this.kinectID;
                accelE.acceleration = new Vector3D(acceleration.Value.X, acceleration.Value.Y, acceleration.Value.Z);
                accelE.elevationAngle = elevationAngle.Value;
                OnAccelerationChanged(accelE);
            }
            else
            {
                KinectBase.AccelerationEventArgs accelE = new KinectBase.AccelerationEventArgs();
                accelE.kinectID = this.kinectID;

                //Send the acceleration, if it is valid
                if (acceleration.HasValue)
                {
                    lastAcceleration = acceleration.Value;
                    accelE.acceleration = new Vector3D(acceleration.Value.X, acceleration.Value.Y, acceleration.Value.Z);
                }
                else
                {
                    accelE.acceleration = null;
                }

                //Send the Kinect angle if it is valid
                if (elevationAngle.HasValue)
                {
                    accelE.elevationAngle = elevationAngle.Value;
                }
                else
                {
                    accelE.elevationAngle = null;
                }

                OnAccelerationChanged(accelE);
            }
        }

        private void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skelFrame = e.OpenSkeletonFrame())
            {
                if (skelFrame != null && masterSettings.kinectOptionsList.Count > kinectID && 
                    (((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).mergeSkeletons || ((KinectV1Core.KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).sendRawSkeletons))
                {
                    Skeleton[] skeletons = new Skeleton[skelFrame.SkeletonArrayLength];
                    skelFrame.CopySkeletonDataTo(skeletons);

                    if (interactStream != null && lastAcceleration != null)
                    {
                        interactStream.ProcessSkeleton(skeletons, lastAcceleration, skelFrame.Timestamp);
                    }

                    //Generate the transformation matrix for the skeletons
                    double kinectYaw = ((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).kinectYaw;
                    Point3D kinectPosition = ((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).kinectPosition;
                    Matrix3D gravityBasedKinectRotation = findRotation(new Vector3D(lastAcceleration.X, lastAcceleration.Y, lastAcceleration.Z), new Vector3D(0, -1, 0));
                    AxisAngleRotation3D yawRotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), -kinectYaw);
                    RotateTransform3D tempTrans = new RotateTransform3D(yawRotation);
                    TranslateTransform3D transTrans = new TranslateTransform3D((Vector3D)kinectPosition);
                    Matrix3D masterMatrix = Matrix3D.Multiply(Matrix3D.Multiply(tempTrans.Value, gravityBasedKinectRotation), transTrans.Value);
                    skeletonTransformation = masterMatrix;

                    //Convert from Kinect v1 skeletons to KVR skeletons
                    KinectBase.KinectSkeleton[] kvrSkeletons = new KinectBase.KinectSkeleton[skeletons.Length];
                    for (int i = 0; i < kvrSkeletons.Length; i++)
                    {
                        //Set the tracking ID numbers for the hand grab data
                        int grabID = -1;
                        for (int j = 0; j < skeletonHandGrabData.Count; j++)
                        {
                            if (skeletonHandGrabData[j].skeletonTrackingID == skeletons[i].TrackingId)
                            {
                                grabID = j;
                                break;
                            }
                        }
                        if (grabID < 0)
                        {
                            skeletonHandGrabData.Add(new HandGrabInfo(skeletons[i].TrackingId));
                            grabID = skeletonHandGrabData.Count - 1;
                        }

                        kvrSkeletons[i] = new KinectBase.KinectSkeleton();
                        kvrSkeletons[i].Position = new Point3D(skeletons[i].Position.X, skeletons[i].Position.Y, skeletons[i].Position.Z);
                        kvrSkeletons[i].SkeletonTrackingState = convertTrackingState(skeletons[i].TrackingState);
                        kvrSkeletons[i].TrackingId = skeletons[i].TrackingId;
                        kvrSkeletons[i].utcSampleTime = DateTime.UtcNow;
                        kvrSkeletons[i].sourceKinectID = kinectID;
                        for (int j = 0; j < skeletons[i].Joints.Count; j++)
                        {
                            KinectBase.Joint newJoint = new KinectBase.Joint();
                            newJoint.Confidence = KinectBase.TrackingConfidence.Unknown; //The Kinect 1 doesn't support the confidence property
                            newJoint.JointType = convertJointType(skeletons[i].Joints[(JointType)j].JointType);
                            Vector4 tempQuat = skeletons[i].BoneOrientations[(JointType)j].AbsoluteRotation.Quaternion;
                            newJoint.Orientation = new Quaternion(tempQuat.X, tempQuat.Y, tempQuat.Z, tempQuat.W);
                            SkeletonPoint tempPos = skeletons[i].Joints[(JointType)j].Position;
                            newJoint.Position = new Point3D(tempPos.X, tempPos.Y, tempPos.Z);
                            newJoint.TrackingState = convertTrackingState(skeletons[i].Joints[(JointType)j].TrackingState);
                            kvrSkeletons[i].skeleton[newJoint.JointType] = newJoint; //Skeleton doesn't need to be initialized because it is done in the KinectSkeleton constructor
                        }

                        //Get the hand states from the hand grab data array
                        kvrSkeletons[i].rightHandClosed = skeletonHandGrabData[grabID].rightHandClosed;
                        kvrSkeletons[i].leftHandClosed = skeletonHandGrabData[grabID].leftHandClosed;
                    }

                    //Add the skeleton data to the event handler and throw the event
                    KinectBase.SkeletonEventArgs skelE = new KinectBase.SkeletonEventArgs();
                    skelE.skeletons = kvrSkeletons;
                    skelE.kinectID = kinectID;

                    OnSkeletonChanged(skelE);
                }
            }
        }
        private void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame != null)
                {
                    //Pass the data to the interaction frame for processing
                    if (interactStream != null && frame.Format == DepthImageFormat.Resolution640x480Fps30)
                    {
                        interactStream.ProcessDepth(frame.GetRawPixelData(), frame.Timestamp);
                    }

                    KinectBase.DepthFrameEventArgs depthE = new KinectBase.DepthFrameEventArgs();
                    depthE.kinectID = this.kinectID;
                    depthE.pixelFormat = PixelFormats.Gray16;
                    depthE.width = frame.Width;
                    depthE.height = frame.Height;
                    depthE.bytesPerPixel = frame.BytesPerPixel;
                    depthE.timeStamp = frame.Timestamp;
                    depthE.image = new short[frame.PixelDataLength];
                    frame.CopyPixelDataTo(depthE.image);
                    OnDepthFrameReceived(depthE);

                    //TODO: Subscribe the server to this event to transmit the depth data using the imager
                }
            }
        }
        private void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    KinectBase.ColorFrameEventArgs colorE = new KinectBase.ColorFrameEventArgs();
                    colorE.kinectID = this.kinectID;
                    if (frame.Format == ColorImageFormat.InfraredResolution640x480Fps30)
                    {
                        colorE.pixelFormat = PixelFormats.Gray16;
                    }
                    else
                    {
                        colorE.pixelFormat = PixelFormats.Bgr32;
                    }
                    colorE.width = frame.Width;
                    colorE.height = frame.Height;
                    colorE.bytesPerPixel = frame.BytesPerPixel;
                    colorE.timeStamp = frame.Timestamp;
                    colorE.image = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(colorE.image);
                    OnColorFrameReceived(colorE);

                    //TODO: Subscribe the server to this to send the color frame over VRPN
                }
            }
        }
        private void interactStream_InteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            using (InteractionFrame interactFrame = e.OpenInteractionFrame())
            {
                if (interactFrame != null && ((KinectV1Settings)masterSettings.kinectOptionsList[kinectID]).mergeSkeletons)
                {
                    UserInfo[] tempUserInfo = new UserInfo[6];
                    interactFrame.CopyInteractionDataTo(tempUserInfo);

                    foreach (UserInfo interactionInfo in tempUserInfo)
                    {
                        foreach (InteractionHandPointer hand in interactionInfo.HandPointers)
                        {
                            if (hand.HandEventType == InteractionHandEventType.Grip)
                            {
                                for (int i = 0; i < skeletonHandGrabData.Count; i++)
                                {
                                    if (skeletonHandGrabData[i].skeletonTrackingID == interactionInfo.SkeletonTrackingId)
                                    {
                                        if (hand.HandType == InteractionHandType.Left)
                                        {
                                            skeletonHandGrabData[i].leftHandClosed = true;
                                        }
                                        else if (hand.HandType == InteractionHandType.Right)
                                        {
                                            skeletonHandGrabData[i].rightHandClosed = true;
                                        }
                                        break;
                                    }
                                }
                            }
                            else if (hand.HandEventType == InteractionHandEventType.GripRelease)
                            {
                                for (int i = 0; i < skeletonHandGrabData.Count; i++)
                                {
                                    if (skeletonHandGrabData[i].skeletonTrackingID == interactionInfo.SkeletonTrackingId)
                                    {
                                        if (hand.HandType == InteractionHandType.Left)
                                        {
                                            skeletonHandGrabData[i].leftHandClosed = false;
                                        }
                                        else if (hand.HandType == InteractionHandType.Right)
                                        {
                                            skeletonHandGrabData[i].rightHandClosed = false;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        void AudioSource_SoundSourceAngleChanged(object sender, SoundSourceAngleChangedEventArgs e)
        {
            KinectBase.AudioPositionEventArgs audioE = new KinectBase.AudioPositionEventArgs();
            audioE.kinectID = this.kinectID;
            audioE.audioAngle = e.Angle;
            audioE.confidence = e.ConfidenceLevel;

            OnAudioPositionChanged(audioE);
        }

        #region Methods to transform the skeletons
        private Skeleton makeTransformedSkeleton(Skeleton inputSkel, Matrix3D transformationMatrix)
        {
            Skeleton adjSkel = new Skeleton();

            //Make sure the ancillary properties are copied over
            adjSkel.TrackingState = inputSkel.TrackingState;
            adjSkel.ClippedEdges = inputSkel.ClippedEdges;
            adjSkel.TrackingId = inputSkel.TrackingId;
            //Don't copy bone orientations, it appears they are calculated on the fly from the joint positions
            
            //Transform the skeleton position
            SkeletonPoint tempPosition = transform(inputSkel.Position, transformationMatrix);
            //tempPosition.X += (float)kinectLocation.X;
            //tempPosition.Y += (float)kinectLocation.Y;
            //tempPosition.Z += (float)kinectLocation.Z;
            adjSkel.Position = tempPosition;

            //Transform all the joint positions
            for (int j = 0; j < inputSkel.Joints.Count; j++)
            {
                Joint tempJoint = adjSkel.Joints[(JointType)j];
                tempJoint.TrackingState = inputSkel.Joints[(JointType)j].TrackingState;
                SkeletonPoint tempPoint = transform(inputSkel.Joints[(JointType)j].Position, transformationMatrix);
                //tempPoint.X += (float)kinectLocation.X;
                //tempPoint.Y += (float)kinectLocation.Y;
                //tempPoint.Z += (float)kinectLocation.Z;
                tempJoint.Position = tempPoint;
                adjSkel.Joints[(JointType)j] = tempJoint;
            }

            return adjSkel;
        }
        private Matrix3D findRotation(Vector3D u, Vector3D v)
        {
            Matrix3D rotationMatrix = new Matrix3D();
            Quaternion rotationQuat = new Quaternion();

            Vector3D cross = Vector3D.CrossProduct(u, v);
            rotationQuat.X = cross.X;
            rotationQuat.Y = cross.Y;
            rotationQuat.Z = cross.Z;
            rotationQuat.W = Math.Sqrt(u.LengthSquared * v.LengthSquared) + Vector3D.DotProduct(u, v);
            rotationQuat.Normalize();

            QuaternionRotation3D tempRotation = new QuaternionRotation3D(rotationQuat);
            RotateTransform3D tempTransform = new RotateTransform3D(tempRotation);
            rotationMatrix = tempTransform.Value;  //Going through RotateTransform3D is kind of a hacky way to do this...

            return rotationMatrix;
        }
        //private Vector3D transformAndConvert(SkeletonPoint position, Matrix3D rotation)
        //{
        //    Vector3D adjustedVector = new Vector3D(position.X, position.Y, position.Z);
        //    adjustedVector = Vector3D.Multiply(adjustedVector, rotation);
        //    return adjustedVector;
        //}
        private SkeletonPoint transform(SkeletonPoint position, Matrix3D rotation)
        {
            Point3D adjustedVector = new Point3D(position.X, position.Y, position.Z);
            adjustedVector = Point3D.Multiply(adjustedVector, rotation);
            SkeletonPoint adjustedPoint = new SkeletonPoint();
            adjustedPoint.X = (float)adjustedVector.X;
            adjustedPoint.Y = (float)adjustedVector.Y;
            adjustedPoint.Z = (float)adjustedVector.Z;
            return adjustedPoint;
        }
        #endregion

        //Misc Methods
        private KinectBase.TrackingState convertTrackingState(SkeletonTrackingState trackingState)
        {
            if (trackingState == SkeletonTrackingState.PositionOnly)
            {
                //The position only state is out of order, so we have to set it manually
                return KinectBase.TrackingState.PositionOnly;
            }
            else
            {
                //All the rest are numbered the same, so we can do a direct cast
                return (KinectBase.TrackingState)trackingState;
            }
        }
        private KinectBase.TrackingState convertTrackingState(JointTrackingState trackingState)
        {
            //These both have the tracking states numbered the same, so we can do a straight cast
            return (KinectBase.TrackingState)trackingState;
        }
        private KinectBase.JointType convertJointType(JointType jointType)
        {
            //The joint types are all numbered the same for the Kinect v1, so we can just do a straight cast
            return (KinectBase.JointType)jointType;
        }

        //Methods to fire the events
        protected virtual void OnSkeletonChanged(KinectBase.SkeletonEventArgs e)
        {
            if (SkeletonChanged != null)
            {
                SkeletonChanged(this, e);
            }
        }
        protected virtual void OnDepthFrameReceived(KinectBase.DepthFrameEventArgs e)
        {
            if (DepthFrameReceived != null)
            {
                DepthFrameReceived(this, e);
            }
        }
        protected virtual void OnColorFrameReceived(KinectBase.ColorFrameEventArgs e)
        {
            if (ColorFrameReceived != null)
            {
                ColorFrameReceived(this, e);
            }
        }
        protected virtual void OnAudioPositionChanged(KinectBase.AudioPositionEventArgs e)
        {
            if (AudioPositionChanged != null)
            {
                AudioPositionChanged(this, e);
            }
        }
        protected virtual void OnAccelerationChanged(KinectBase.AccelerationEventArgs e)
        {
            if (AccelerationChanged != null)
            {
                AccelerationChanged(this, e);
            }
        }
        protected virtual void OnLogMessageGenerated(KinectBase.LogMessageEventArgs e)
        {
            if (LogMessageGenerated != null)
            {
                LogMessageGenerated(this, e);
            }
        }

        private delegate void launchKinectDelegate();
        private delegate void startAudioDelegate();
    }

    internal class HandGrabInfo
    {
        internal HandGrabInfo(int trackingID)
        {
            skeletonTrackingID = trackingID;
        }

        internal int skeletonTrackingID;
        internal bool rightHandClosed = false;
        internal bool leftHandClosed = false;
    }

    //This dummy class is required to get the hand grab information from the Kinect
    public class DummyInteractionClient : IInteractionClient
    {
        public InteractionInfo GetInteractionInfoAtLocation(int skeletonTrackingId, InteractionHandType handType, double x, double y)
        {
            InteractionInfo result = new InteractionInfo();
            result.IsGripTarget = true;
            result.IsPressTarget = true;
            result.PressAttractionPointX = 0.5;
            result.PressAttractionPointY = 0.5;
            result.PressTargetControlId = 1;
            return result;
        }
    }
}