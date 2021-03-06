﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using KinectBase;

namespace KinectWithVRServer
{
    internal class SkeletonMerger
    {
        //TODO: Figure out how to destroy unused skeletons on the fly without causing concurrency issues
        //List<FilteredSkeleton> filteredSkeletons;
        ConcurrentSkeletonCollection filteredSkeletons;

        internal SkeletonMerger()
        {
            //filteredSkeletons = new List<FilteredSkeleton>();
            filteredSkeletons = new ConcurrentSkeletonCollection();
        }

        internal void MergeSkeleton(KinectSkeleton skeleton)
        {
            //Only merge skeletons that have some sort of useful information
            if (skeleton.SkeletonTrackingState == TrackingState.Tracked || skeleton.SkeletonTrackingState == TrackingState.Inferred)
            {
                filteredSkeletons.HoldUpdates();

                bool isReversed = false;
                int filteredSkeletonIndex = FindSkeletonNumber(skeleton, out isReversed);
                IntegrateSkeleton(skeleton, filteredSkeletonIndex, isReversed);

                filteredSkeletons.ReleaseForUpdates();
            }
        }

        //internal KinectSkeleton GetPredictedSkeleton(int skeletonNumber, double msAheadOfNow)
        //{
        //    return new KinectSkeleton();
        //}

        internal KinectSkeleton[] GetAllPredictedSkeletons(double msAheadOfNow)
        {
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

            filteredSkeletons.HoldUpdates();

            List<KinectSkeleton> skeletons = new List<KinectSkeleton>();

            //TODO: This is crashing due to a concurrancy issue
            //This seems to be caused by the delete happening in line 61 and the add in line 136
            //To fix this, someone we have to mark the array to hang onto the skeleton until this function can be cleared
            //I think it will have to be wrapped in a class
            for (int i = 0; i < filteredSkeletons.Count; i++)
            {
                if (filteredSkeletons[i].AgeMS > 5000)
                {
                    filteredSkeletons.RemoveAt(i);
                }
                else
                {
                    skeletons.Add(filteredSkeletons[i].PredictSkeleton(msAheadOfNow));
                }
            }

            filteredSkeletons.ReleaseForUpdates();

            watch.Stop();
            System.Diagnostics.Debug.WriteLine("Predict all took {0} ms.", watch.ElapsedMilliseconds);

            return skeletons.ToArray();
        }

        private int FindSkeletonNumber(KinectSkeleton skeleton, out bool isReversed)
        {
            List<double> averageForwardDistance = new List<double>();
            List<double> averageReverseDistance = new List<double>();
            isReversed = false;

            for (int i = 0; i < filteredSkeletons.Count; i++)
            {
                Point3D[] skelToCompare = filteredSkeletons[i].PredictPositionsOnly(0);
                double forwardAverage = 0;
                double reverseAverage = 0;
                int forwardN = 0;
                int reverseN = 0;

                for (int j = 0; j < skeleton.skeleton.Count; j++)
                {
                    if (skeleton.skeleton[j].TrackingState == TrackingState.Tracked)
                    {
                        #region Forward Comparison
                        //Add the X distance to the average
                        forwardAverage += ((skelToCompare[j].X - skeleton.skeleton[j].Position.X) - forwardAverage) / (forwardN + 1);
                        forwardN++;
                        //Add the Y distance to the average
                        forwardAverage += ((skelToCompare[j].Y - skeleton.skeleton[j].Position.Y) - forwardAverage) / (forwardN + 1);
                        forwardN++;
                        //Add the Z distance to the average
                        forwardAverage += ((skelToCompare[j].Z - skeleton.skeleton[j].Position.Z) - forwardAverage) / (forwardN + 1);
                        forwardN++;
                        #endregion

                        #region Reverse Comarison
                        Point3D tempPoint = skeleton.skeleton[j].Position;
                        if (((JointType)j) == JointType.ShoulderLeft)
                        {
                            tempPoint = skeleton.skeleton[JointType.ShoulderRight].Position;
                        }
                        else if (((JointType)j) == JointType.ShoulderRight)
                        {
                            tempPoint = skeleton.skeleton[JointType.ShoulderLeft].Position;
                        }
                        else if (((JointType)j) == JointType.ElbowLeft)
                        {
                            tempPoint = skeleton.skeleton[JointType.ElbowRight].Position;
                        }
                        else if (((JointType)j) == JointType.ElbowRight)
                        {
                            tempPoint = skeleton.skeleton[JointType.ElbowLeft].Position;
                        }
                        else if (((JointType)j) == JointType.WristLeft)
                        {
                            tempPoint = skeleton.skeleton[JointType.WristRight].Position;
                        }
                        else if (((JointType)j) == JointType.WristRight)
                        {
                            tempPoint = skeleton.skeleton[JointType.WristLeft].Position;
                        }
                        else if (((JointType)j) == JointType.HandLeft)
                        {
                            tempPoint = skeleton.skeleton[JointType.HandRight].Position;
                        }
                        else if (((JointType)j) == JointType.HandRight)
                        {
                            tempPoint = skeleton.skeleton[JointType.HandLeft].Position;
                        }
                        else if (((JointType)j) == JointType.HandTipLeft)
                        {
                            tempPoint = skeleton.skeleton[JointType.HandTipRight].Position;
                        }
                        else if (((JointType)j) == JointType.HandTipRight)
                        {
                            tempPoint = skeleton.skeleton[JointType.HandTipLeft].Position;
                        }
                        else if (((JointType)j) == JointType.ThumbLeft)
                        {
                            tempPoint = skeleton.skeleton[JointType.ThumbRight].Position;
                        }
                        else if (((JointType)j) == JointType.ThumbRight)
                        {
                            tempPoint = skeleton.skeleton[JointType.ThumbLeft].Position;
                        }
                        else if (((JointType)j) == JointType.HipLeft)
                        {
                            tempPoint = skeleton.skeleton[JointType.HipRight].Position;
                        }
                        else if (((JointType)j) == JointType.HipRight)
                        {
                            tempPoint = skeleton.skeleton[JointType.HipLeft].Position;
                        }
                        else if (((JointType)j) == JointType.KneeLeft)
                        {
                            tempPoint = skeleton.skeleton[JointType.KneeRight].Position;
                        }
                        else if (((JointType)j) == JointType.KneeRight)
                        {
                            tempPoint = skeleton.skeleton[JointType.KneeLeft].Position;
                        }
                        else if (((JointType)j) == JointType.AnkleLeft)
                        {
                            tempPoint = skeleton.skeleton[JointType.AnkleRight].Position;
                        }
                        else if (((JointType)j) == JointType.AnkleRight)
                        {
                            tempPoint = skeleton.skeleton[JointType.AnkleLeft].Position;
                        }
                        else if (((JointType)j) == JointType.FootLeft)
                        {
                            tempPoint = skeleton.skeleton[JointType.FootRight].Position;
                        }
                        else if (((JointType)j) == JointType.FootRight)
                        {
                            tempPoint = skeleton.skeleton[JointType.FootLeft].Position;
                        }

                        //Add the X distance to the average
                        reverseAverage += ((skelToCompare[j].X - skeleton.skeleton[j].Position.X) - reverseAverage) / (reverseN + 1);
                        reverseN++;
                        //Add the Y distance to the average
                        reverseAverage += ((skelToCompare[j].Y - skeleton.skeleton[j].Position.Y) - reverseAverage) / (reverseN + 1);
                        reverseN++;
                        //Add the Z distance to the average
                        reverseAverage += ((skelToCompare[j].Z - skeleton.skeleton[j].Position.Z) - reverseAverage) / (reverseN + 1);
                        reverseN++;
                        #endregion
                    }
                }

                //If any points were compared, add the average to the list, otherwise mark it as uncompared with NaN
                if (forwardN > 0)
                {
                    averageForwardDistance.Add(forwardAverage);
                }
                else
                {
                    averageForwardDistance.Add(double.NaN);
                }
                if (reverseN > 0)
                {
                    averageReverseDistance.Add(reverseAverage);
                }
                else
                {
                    averageReverseDistance.Add(double.NaN);
                }
            }

            //Go through the lists of averages and find the lowest
            double absLowest = double.MaxValue;
            int lowestIdx = -1;  //Initialize to -1 to mark that no matching skeleton was found
            for (int i = 0; i < averageForwardDistance.Count; i++)
            {
                if (!double.IsNaN(averageForwardDistance[i]))
                {
                    double abs = Math.Abs(averageForwardDistance[i]);
                    if (abs < absLowest)
                    {
                        absLowest = abs;
                        lowestIdx = i;
                    }
                }
            }
            for (int i = 0; i < averageReverseDistance.Count; i++)
            {
                if (!double.IsNaN(averageReverseDistance[i]))
                {
                    double abs = Math.Abs(averageReverseDistance[i]);
                    if (abs < absLowest)
                    {
                        absLowest = abs;
                        lowestIdx = i;
                        isReversed = true;
                    }
                }
            }

            //TODO: is 0.3 m the best threshold to use?
            //If the best comparison has an average joint distance of < 0.3 m
            if (lowestIdx >= 0 && absLowest < 0.3)
            {
                return lowestIdx;
            }
            else
            {
                return -1;  //-1 will be our error case to indicate that a matching filtered skeleton doesn't yet exist
            }
        }

        private void IntegrateSkeleton(KinectSkeleton skeleton, int number, bool isReversed)
        {
            if (number >= 0)
            {
                filteredSkeletons[number].IntegrateSkeleton(skeleton, isReversed);
            }
            else
            {
                FilteredSkeleton tempSkel = new FilteredSkeleton();
                tempSkel.IntegrateSkeleton(skeleton, isReversed);
                filteredSkeletons.Add(tempSkel);
            }
        }
    }

    internal class FilteredSkeleton
    {
        private JerkConst3DFilter[] filteredJoints;
        private DateTime[] lastTrackedTime;
        private DateTime[] lastInferredTime;
        private HandFilter rhFilter;
        private HandFilter lhFilter;
        internal double AgeMS
        {
            get { return (DateTime.UtcNow - lastTrackedTime.Max()).TotalMilliseconds; }
        }

        internal FilteredSkeleton()
        {
            filteredJoints = new JerkConst3DFilter[KinectBase.HelperMethods.TotalJointCount];
            lastTrackedTime = new DateTime[KinectBase.HelperMethods.TotalJointCount];
            lastInferredTime = new DateTime[KinectBase.HelperMethods.TotalJointCount];
            for (int i = 0; i < KinectBase.HelperMethods.TotalJointCount; i++)
            {
                filteredJoints[i] = new JerkConst3DFilter();
                lastTrackedTime[i] = DateTime.MinValue;
                lastInferredTime[i] = DateTime.MinValue;
            }
            rhFilter = new HandFilter();
            lhFilter = new HandFilter();
        }

        internal void IntegrateSkeleton(KinectSkeleton skeleton, bool isReversed)
        {
            for (int i = 0; i < KinectBase.HelperMethods.TotalJointCount; i++)
            {
                int j = GetJointIndex(i, isReversed);  //Swap the left and righ joints, if needed
                //The indexer i is for the filtered joints (it is non-reversed)
                //The indexer j is for the input joints (it reverses left-right, if needed)
                if (skeleton.skeleton[j].TrackingState == TrackingState.Tracked)
                {
                    filteredJoints[i].IntegrateMeasurement(PointToObMatrix(skeleton.skeleton[j].Position), skeleton.skeleton[j].utcTime, PointToEigVector(skeleton.skeleton[j].spatialErrorStdDev));
                    if (skeleton.skeleton[j].utcTime > lastTrackedTime[i])
                    {
                        lastTrackedTime[i] = skeleton.skeleton[j].utcTime;
                    }
                }
                else if (skeleton.skeleton[j].TrackingState == TrackingState.Inferred)
                {
                    filteredJoints[i].IntegrateMeasurement(PointToObMatrix(skeleton.skeleton[j].Position), skeleton.skeleton[j].utcTime, PointToEigVector(skeleton.skeleton[j].spatialErrorStdDev));
                    if (skeleton.skeleton[j].utcTime > lastInferredTime[i])
                    {
                        lastInferredTime[i] = skeleton.skeleton[j].utcTime;
                    }
                }
            }

            IntegrateLeftHandGrabState(skeleton, isReversed);
            IntegrateRightHandGrabState(skeleton, isReversed);
 
        }

        internal KinectSkeleton PredictSkeleton(double msAheadOfNow)
        {
            KinectSkeleton newSkeleton = new KinectSkeleton();
            Joint[] tempJoints = new Joint[KinectBase.HelperMethods.TotalJointCount];
            for (int i = 0; i < KinectBase.HelperMethods.TotalJointCount; i++)
            {
                Joint newJoint = new Joint();
                EigenWrapper.Matrix covariance;
                newJoint.JointType = newSkeleton.skeleton[i].JointType;
                EigenWrapper.Matrix state = filteredJoints[i].PredictAndDiscardFromNow(msAheadOfNow, out covariance);
                newJoint.Position = FilteredMatrixToPoint(state);
                newJoint.TrackingState = GetJointTrackingState(covariance, lastTrackedTime[i]);
                tempJoints[i] = newJoint;
            }

            //Calculate the orientations for all the skeletons
            Matrix3D[] orientations = CalculateOrientations(tempJoints);

            //Add the orientations to the joints and pass them into the newSkeleton object
            for (int i = 0; i < KinectBase.HelperMethods.TotalJointCount; i++)
            {
                tempJoints[i].Orientation.orientationMatrix = orientations[i];
                newSkeleton.skeleton[i] = tempJoints[i];
            }

            //TODO: Handle all the per skeleton stuff here (e.g. skeleton tracking state, tracking ID, etc)
            newSkeleton.leftHandClosed = lhFilter.PredictMeasurement();
            newSkeleton.rightHandClosed = rhFilter.PredictMeasurement();
            newSkeleton.Position = newSkeleton.skeleton[JointType.HipCenter].Position;
            newSkeleton.SkeletonTrackingState = GetSkeletonTrackingState(newSkeleton);

            return newSkeleton;
        }

        //This method only gets the predicted positions.  It is used for the skeleton comparison to save CPU power.
        internal Point3D[] PredictPositionsOnly(double msAheadOfNow)
        {
            Point3D[] tempPositions = new Point3D[KinectBase.HelperMethods.TotalJointCount];
            for (int i = 0; i < KinectBase.HelperMethods.TotalJointCount; i++)
            {
                EigenWrapper.Matrix state = filteredJoints[i].PredictAndDiscardFromNow(msAheadOfNow);
                tempPositions[i] = FilteredMatrixToPoint(state);
            }

            return tempPositions;
        }

        private EigenWrapper.Matrix PointToObMatrix(System.Windows.Media.Media3D.Point3D point)
        {
            EigenWrapper.Matrix matrix = new EigenWrapper.Matrix(3, 1);
            matrix[0, 0] = point.X;
            matrix[1, 0] = point.Y;
            matrix[2, 0] = point.Z;

            return matrix;
        }

        private EigenWrapper.Vector PointToEigVector(System.Windows.Media.Media3D.Point3D point)
        {
            EigenWrapper.Vector vector = new EigenWrapper.Vector(3);
            vector[0] = point.X;
            vector[1] = point.Y;
            vector[2] = point.Z;

            return vector;
        }

        private System.Windows.Media.Media3D.Point3D FilteredMatrixToPoint(EigenWrapper.Matrix matrix)
        {
            System.Windows.Media.Media3D.Point3D point = new System.Windows.Media.Media3D.Point3D();
            point.X = matrix[0, 0];
            point.Y = matrix[3, 0];
            point.Z = matrix[6, 0];
            return point;
        }

        #region Joint Orientation Calculation Stuff
        //There are a couple different methods of calculating the orientations that I've been playing with
        //This will call the correct one and return the results
        internal Matrix3D[] CalculateOrientations(Joint[] skeleton)
        {
            //return CalculateOrientationsKV1Method(ref skeleton);
            return CalculateOrientationsImprovedKV1Method(skeleton);
        }

        //This calculates the orientations in the same way as the Kinect v1
        private Matrix3D[] CalculateOrientationsKV1Method(Joint[] skeleton)
        {
            Matrix3D[] orientations = new Matrix3D[KinectBase.HelperMethods.TotalJointCount];

            #region Hip Center [AKA Spine Base] Joint Orientation
            Point3D HC = skeleton[(int)JointType.HipCenter].Position;
            Point3D HR = skeleton[(int)JointType.HipRight].Position;
            Point3D HL = skeleton[(int)JointType.HipLeft].Position;
            Vector3D HC2HR = HC - HR;
            HC2HR.Normalize();
            Vector3D HC2HL = HC - HL;
            HC2HL.Normalize();
            Vector3D zHC = Vector3D.CrossProduct(HC2HR, HC2HL);
            zHC.Normalize();
            Vector3D HR2HL = HR - HL;
            Vector3D yHC = Vector3D.CrossProduct(HR2HL, zHC);
            yHC.Normalize();
            Vector3D xHC = Vector3D.CrossProduct(yHC, zHC);
            xHC.Normalize();
            Matrix3D hcMat = RotationMatFromRowVecs(xHC, yHC, zHC);
            //orientations[(int)JointType.HipCenter] = MatrixToQuat(hcMat);
            orientations[(int)JointType.HipCenter] = hcMat;
            #endregion
            #region Spine [AKA Spine Mid] Joint Orientation
            Point3D SP = skeleton[(int)JointType.Spine].Position;
            Point3D SR = skeleton[(int)JointType.ShoulderRight].Position;
            Point3D SL = skeleton[(int)JointType.ShoulderLeft].Position;
            Vector3D SP2HC = SP - HC;
            SP2HC.Normalize();
            Vector3D SL2SR = SL - SR;
            SL2SR.Normalize();
            Vector3D zSP = Vector3D.CrossProduct(SL2SR, SP2HC);
            zSP.Normalize();
            Vector3D xSP = Vector3D.CrossProduct(SP2HC, zSP);
            xSP.Normalize();
            Matrix3D spMat = RotationMatFromRowVecs(xSP, SP2HC, zSP);
            //orientations[(int)JointType.Spine] = MatrixToQuat(spMat);
            orientations[(int)JointType.Spine] = spMat;
            #endregion
            #region Shoulder Center [AKA Spine Shoulder] Joint Orientation
            Point3D SC = skeleton[(int)JointType.ShoulderCenter].Position;
            Vector3D SC2SP = SC - SP;
            SC2SP.Normalize();
            Vector3D zSC = Vector3D.CrossProduct(SL2SR, SC2SP);
            zSC.Normalize();
            Vector3D xSC = Vector3D.CrossProduct(SC2SP, zSC);
            xSC.Normalize();
            Matrix3D scMat = RotationMatFromRowVecs(xSC, SC2SP, zSC);
            //orientations[(int)JointType.ShoulderCenter] = MatrixToQuat(scMat);
            orientations[(int)JointType.ShoulderCenter] = scMat;
            #endregion
            #region Head Joint Orientation
            Point3D HD = skeleton[(int)JointType.Head].Position;
            Vector3D HD2SC = HD - SC;
            HD2SC.Normalize();
            Vector3D zHD = Vector3D.CrossProduct(xSC, HD2SC);
            zHD.Normalize();
            Vector3D xHD = Vector3D.CrossProduct(HD2SC, zHD);
            xHD.Normalize();
            Matrix3D hdMat = RotationMatFromRowVecs(xHD, HD2SC, zHD);
            //orientations[(int)JointType.Head] = MatrixToQuat(hdMat);
            orientations[(int)JointType.Head] = hdMat;
            #endregion
            #region Shoulder Left Joint Orientation
            Vector3D SL2SC = SL - SC;
            SL2SC.Normalize();
            Vector3D xSL = Vector3D.CrossProduct(SL2SC, zSC);
            xSL.Normalize();
            Vector3D zSL = Vector3D.CrossProduct(xSL, SL2SC);
            zSL.Normalize();
            Matrix3D slMat = RotationMatFromRowVecs(xSL, SL2SC, zSL);
            //orientations[(int)JointType.ShoulderLeft] = MatrixToQuat(slMat);
            orientations[(int)JointType.ShoulderLeft] = slMat;
            #endregion
            #region Shoulder Right Joint Orientation
            Vector3D SR2SC = SR - SC;
            SR2SC.Normalize();
            Vector3D xSR = Vector3D.CrossProduct(SR2SC, zSC);
            xSR.Normalize();
            Vector3D zSR = Vector3D.CrossProduct(xSR, SR2SC);
            zSR.Normalize();
            Matrix3D srMat = RotationMatFromRowVecs(xSR, SR2SC, zSR);
            //orientations[(int)JointType.ShoulderRight] = MatrixToQuat(srMat);
            orientations[(int)JointType.ShoulderRight] = srMat;
            #endregion
            #region Elbow Left Joint Orientation
            Point3D WL = skeleton[(int)JointType.WristLeft].Position;
            Point3D EL = skeleton[(int)JointType.ElbowLeft].Position;
            Vector3D EL2SL = EL - SL;
            EL2SL.Normalize();
            Vector3D WL2EL = WL - EL;
            WL2EL.Normalize();
            Vector3D xEL;
            Vector3D zEL;
            double cosAngleEL = Vector3D.DotProduct(EL2SL, WL2EL);  //Because these are both unit vectors, the dot product of the two equals cos(angle)
            if (Math.Abs(cosAngleEL) < 0.94)  //The cosine of the angular thresholds are precalculated, this is roughly an angle of 19.5 degrees (160.5 on the other side)
            {
                Vector3D preXel = Vector3D.CrossProduct(EL2SL, WL2EL);
                preXel.Normalize();
                zEL = Vector3D.CrossProduct(preXel, EL2SL);
                zEL.Normalize();
                xEL = Vector3D.CrossProduct(EL2SL, zEL);
                xEL.Normalize();
            }
            else
            {
                double cosAngleEL2 = Vector3D.DotProduct(SC2SP, EL2SL);
                if (cosAngleEL2 <= 0)  //This is equivalent to an angle >= 90 degrees
                {
                    zEL = Vector3D.CrossProduct(EL2SL, xSC);
                    zEL.Normalize();
                    xEL = Vector3D.CrossProduct(EL2SL, zEL);
                    xEL.Normalize();
                }
                else
                {
                    zEL = Vector3D.CrossProduct(EL2SL, SC2SP);
                    zEL.Normalize();
                    xEL = Vector3D.CrossProduct(EL2SL, zEL);
                    xEL.Normalize();
                }
            }
            Matrix3D elMat = RotationMatFromRowVecs(xEL, EL2SL, zEL);
            //orientations[(int)JointType.ElbowLeft] = MatrixToQuat(elMat);
            orientations[(int)JointType.ElbowLeft] = elMat;
            #endregion
            #region Wrist Left Joint Orientation
            Vector3D zWL = Vector3D.CrossProduct(xEL, WL2EL);
            zWL.Normalize();
            Vector3D xWL = Vector3D.CrossProduct(WL2EL, zWL);
            xWL.Normalize();
            Matrix3D wlMat = RotationMatFromRowVecs(xWL, WL2EL, zWL);
            //orientations[(int)JointType.WristLeft] = MatrixToQuat(wlMat);
            orientations[(int)JointType.WristLeft] = wlMat;
            #endregion
            #region Hand Left Joint Orientation
            Point3D HDL = skeleton[(int)JointType.HandLeft].Position;
            Vector3D HDL2WL = HDL - WL;
            HDL2WL.Normalize();
            Vector3D xHDL = Vector3D.CrossProduct(HDL2WL, zWL);
            xHDL.Normalize();
            Vector3D zHDL = Vector3D.CrossProduct(xHDL, HDL2WL);
            zHDL.Normalize();
            Matrix3D hdlMat = RotationMatFromRowVecs(xHDL, HDL2WL, zHDL);
            //orientations[(int)JointType.HandLeft] = MatrixToQuat(hdlMat);
            orientations[(int)JointType.HandLeft] = hdlMat;
            #endregion
            #region Elbow Right Joint Orientation
            Point3D WR = skeleton[(int)JointType.WristRight].Position;
            Point3D ER = skeleton[(int)JointType.ElbowRight].Position;
            Vector3D ER2SR = ER - WR;
            ER2SR.Normalize();
            Vector3D WR2ER = WR - ER;
            WR2ER.Normalize();
            Vector3D xER;
            Vector3D zER;
            double cosAngleER = Vector3D.DotProduct(ER2SR, WR2ER);  //Because these are both unit vectors, the dot product of the two equals cos(angle)
            if (Math.Abs(cosAngleER) < 0.94)  //The cosine of the angular thresholds are precalculated, this is roughly an angle of 19.5 degrees (160.5 on the other side)
            {
                Vector3D preXer = Vector3D.CrossProduct(ER2SR, WR2ER);
                preXer.Normalize();
                zER = Vector3D.CrossProduct(preXer, ER2SR);
                zER.Normalize();
                xER = Vector3D.CrossProduct(ER2SR, zER);
                xER.Normalize();
            }
            else
            {
                double cosAngleER2 = Vector3D.DotProduct(SC2SP, ER2SR);
                if (cosAngleER2 <= 0)  //This is equivalent to an angle >= 90 degrees
                {
                    zER = Vector3D.CrossProduct(ER2SR, xSC);
                    zER.Normalize();
                    xER = Vector3D.CrossProduct(ER2SR, zER);
                    xER.Normalize();
                }
                else
                {
                    zER = Vector3D.CrossProduct(SC2SP, ER2SR);
                    zER.Normalize();
                    xER = Vector3D.CrossProduct(ER2SR, zER);
                    xER.Normalize();
                }
            }
            Matrix3D erMat = RotationMatFromRowVecs(xER, ER2SR, zER);
            //orientations[(int)JointType.ElbowRight] = MatrixToQuat(erMat);
            orientations[(int)JointType.ElbowRight] = erMat;
            #endregion
            #region Wrist Right Joint Orientation
            Vector3D zWR = Vector3D.CrossProduct(xER, WR2ER);
            zWR.Normalize();
            Vector3D xWR = Vector3D.CrossProduct(WR2ER, zWR);
            xWR.Normalize();
            Matrix3D wrMat = RotationMatFromRowVecs(xWR, WR2ER, zWR);
            //orientations[(int)JointType.WristRight] = MatrixToQuat(wrMat);
            orientations[(int)JointType.WristRight] = wrMat;
            #endregion
            #region  Joint Orientation
            Point3D HDR = skeleton[(int)JointType.HandRight].Position;
            Vector3D HDR2WR = HDR - WR;
            HDR2WR.Normalize();
            Vector3D xHDR = Vector3D.CrossProduct(HDR2WR, zWR);
            xHDR.Normalize();
            Vector3D zHDR = Vector3D.CrossProduct(xHDR, HDR2WR);
            zHDR.Normalize();
            Matrix3D hdrMat = RotationMatFromRowVecs(xHDR, HDR2WR, zHDR);
            //orientations[(int)JointType.HandRight] = MatrixToQuat(hdrMat);
            orientations[(int)JointType.HandRight] = hdrMat;
            #endregion
            #region Hip Left Joint Orientation
            Vector3D HL2HC = HL - HC;
            HL2HC.Normalize();
            Vector3D zHL = Vector3D.CrossProduct(xHC, HC2HL);
            zHL.Normalize();
            Vector3D xHL = Vector3D.CrossProduct(zHL, HC2HL);
            xHL.Normalize();
            Matrix3D hlMat = RotationMatFromRowVecs(xHL, HL2HC, zHL);
            //orientations[(int)JointType.HipLeft] = MatrixToQuat(hlMat);
            orientations[(int)JointType.HipLeft] = hlMat;
            #endregion
            #region Knee Left and Ankle Left Joint Orientations
            //Note, these need to be done together as the knee sometimes depends on the ankle
            Point3D KL = skeleton[(int)JointType.KneeLeft].Position;
            Point3D AL = skeleton[(int)JointType.AnkleLeft].Position;
            Vector3D KL2HL = KL - HL;
            KL2HL.Normalize();
            Vector3D AL2KL = AL - KL;
            AL2KL.Normalize();
            Vector3D xKL;
            Vector3D zKL;
            Vector3D xAL;
            Vector3D zAL;
            if (skeleton[(int)JointType.KneeLeft].TrackingState == TrackingState.Tracked)
            {
                double cosAngleKL = Vector3D.DotProduct(KL2HL, AL2KL);
                if (cosAngleKL < 0.972) //This is roughly an angle of 13.5 degrees
                {
                    //Ankle orientation calculation
                    zAL = Vector3D.CrossProduct(AL2KL, xHC);
                    zAL.Normalize();
                    xAL = Vector3D.CrossProduct(AL2KL, zAL);
                    xAL.Normalize();

                    //Knee orientation calculation
                    zKL = Vector3D.CrossProduct(xAL, KL2HL);
                    zKL.Normalize();
                    xKL = Vector3D.CrossProduct(KL2HL, zKL);
                    xKL.Normalize();
                }
                else
                {
                    //Knee orientation calculation
                    zKL = Vector3D.CrossProduct(KL2HL, xHC);
                    zKL.Normalize();
                    xKL = Vector3D.CrossProduct(KL2HL, zKL);
                    xKL.Normalize();

                    //Ankle orientation calculation
                    zAL = Vector3D.CrossProduct(xKL, AL2KL);
                    zAL.Normalize();
                    xAL = Vector3D.CrossProduct(AL2KL, zAL);
                    xAL.Normalize();
                }
            }
            else
            {
                //Knee orientation calculation
                zKL = Vector3D.CrossProduct(KL2HL, xHC);
                zKL.Normalize();
                xKL = Vector3D.CrossProduct(KL2HL, zKL);
                xKL.Normalize();

                //Ankle orientation calculation
                zAL = Vector3D.CrossProduct(xKL, AL2KL);
                zAL.Normalize();
                xAL = Vector3D.CrossProduct(AL2KL, zAL);
                xAL.Normalize();
            }
            Matrix3D klMat = RotationMatFromRowVecs(xKL, KL2HL, zKL);
            //orientations[(int)JointType.KneeLeft] = MatrixToQuat(klMat);
            orientations[(int)JointType.KneeLeft] = klMat;
            Matrix3D alMat = RotationMatFromRowVecs(xAL, AL2KL, zAL);
            //orientations[(int)JointType.AnkleLeft] = MatrixToQuat(alMat);
            orientations[(int)JointType.AnkleLeft] = alMat;
            #endregion
            #region Foot Left Joint Orientation
            Point3D FL = skeleton[(int)JointType.FootLeft].Position;
            Vector3D FL2AL = FL - AL;
            Vector3D zFL = Vector3D.CrossProduct(xAL, FL2AL);
            zFL.Normalize();
            Vector3D xFL = Vector3D.CrossProduct(FL2AL, zFL);
            xFL.Normalize();
            Matrix3D flMat = RotationMatFromRowVecs(xFL, FL2AL, zFL);
            //orientations[(int)JointType.FootLeft] = MatrixToQuat(flMat);
            orientations[(int)JointType.FootLeft] = flMat;
            #endregion
            #region Hip Right Joint Orientation
            Vector3D HR2HC = HR - HC;
            HR2HC.Normalize();
            Vector3D zHR = Vector3D.CrossProduct(HR2HC, xHC);
            zHR.Normalize();
            Vector3D xHR = Vector3D.CrossProduct(HR2HC, zHR);
            xHR.Normalize();
            Matrix3D hrMat = RotationMatFromRowVecs(xHR, HR2HC, zHR);
            //orientations[(int)JointType.HipRight] = MatrixToQuat(hrMat);
            orientations[(int)JointType.HipRight] = hrMat;
            #endregion
            #region Knee Right and Ankle Right Joint Orientations
            //Note, these need to be done together as the knee sometimes depends on the ankle
            Point3D KR = skeleton[(int)JointType.KneeRight].Position;
            Point3D AR = skeleton[(int)JointType.AnkleRight].Position;
            Vector3D KR2HR = KR - HR;
            KR2HR.Normalize();
            Vector3D AR2KR = AR - KR;
            AR2KR.Normalize();
            Vector3D xKR;
            Vector3D zKR;
            Vector3D xAR;
            Vector3D zAR;
            if (skeleton[(int)JointType.KneeRight].TrackingState == TrackingState.Tracked)
            {
                double cosAngleKR = Vector3D.DotProduct(KR2HR, AR2KR);
                if (cosAngleKR < 0.972) //This is roughly an angle of 13.5 degrees
                {
                    //Ankle orientation calculation
                    zAR = Vector3D.CrossProduct(AR2KR, xHC);
                    zAR.Normalize();
                    xAR = Vector3D.CrossProduct(AR2KR, zAR);
                    xAR.Normalize();

                    //Knee orientation calculation
                    zKR = Vector3D.CrossProduct(xAR, KR2HR);
                    zKR.Normalize();
                    xKR = Vector3D.CrossProduct(KR2HR, zKR);
                    xKR.Normalize();
                }
                else
                {
                    //Knee orientation calculation
                    zKR = Vector3D.CrossProduct(KR2HR, xHC);
                    zKR.Normalize();
                    xKR = Vector3D.CrossProduct(KR2HR, zKR);
                    xKR.Normalize();

                    //Ankle orientation calculation
                    zAR = Vector3D.CrossProduct(xKR, AR2KR);
                    zAR.Normalize();
                    xAR = Vector3D.CrossProduct(AR2KR, zAR);
                    xAR.Normalize();
                }
            }
            else
            {
                //Knee orientation calculation
                zKR = Vector3D.CrossProduct(KR2HR, xHC);
                zKR.Normalize();
                xKR = Vector3D.CrossProduct(KR2HR, zKR);
                xKR.Normalize();

                //Ankle orientation calculation
                zAR = Vector3D.CrossProduct(xKR, AR2KR);
                zAR.Normalize();
                xAR = Vector3D.CrossProduct(AR2KR, zAR);
                xAR.Normalize();
            }
            Matrix3D krMat = RotationMatFromRowVecs(xKR, KR2HR, zKR);
            //orientations[(int)JointType.KneeRight] = MatrixToQuat(krMat);
            orientations[(int)JointType.KneeRight] = krMat;
            Matrix3D arMat = RotationMatFromRowVecs(xAR, AR2KR, zAR);
            //orientations[(int)JointType.AnkleRight] = MatrixToQuat(arMat);
            orientations[(int)JointType.AnkleRight] = arMat;
            #endregion
            #region Foot Right Joint Orientation
            Point3D FR = skeleton[(int)JointType.FootRight].Position;
            Vector3D FR2AR = FR - AR;
            Vector3D zFR = Vector3D.CrossProduct(xAR, FR2AR);
            zFR.Normalize();
            Vector3D xFR = Vector3D.CrossProduct(FR2AR, zFR);
            xFR.Normalize();
            Matrix3D frMat = RotationMatFromRowVecs(xFR, FR2AR, zFR);
            //orientations[(int)JointType.FootRight] = MatrixToQuat(frMat);
            orientations[(int)JointType.FootRight] = frMat;
            #endregion

            //These joint orientations are all set to identity because they don't exist on the Kinect v1
            //The improved KV1 orientation method contains code to set these in a way that is similiar to what the KV1 does for other joints
            orientations[(int)JointType.Neck] = Matrix3D.Identity;
            orientations[(int)JointType.HandTipLeft] = Matrix3D.Identity;
            orientations[(int)JointType.ThumbLeft] = Matrix3D.Identity;
            orientations[(int)JointType.HandTipRight] = Matrix3D.Identity;
            orientations[(int)JointType.ThumbRight] = Matrix3D.Identity;

            return orientations;
        }

        //This calculates the orientations similiar to the Kinect v1, but handles some error cases the KV1 doesn't
        private Matrix3D[] CalculateOrientationsImprovedKV1Method(Joint[] skeleton)
        {
            //TODO: Update this to handle the KV2 joints and to handle degenerate cases
            Matrix3D[] orientations = new Matrix3D[KinectBase.HelperMethods.TotalJointCount];

            #region Hip Center [AKA Spine Base] Joint Orientation
            Point3D HC = skeleton[(int)JointType.HipCenter].Position;
            Point3D HR = skeleton[(int)JointType.HipRight].Position;
            Point3D HL = skeleton[(int)JointType.HipLeft].Position;
            Vector3D HC2HR = HC - HR;
            HC2HR.Normalize();
            Vector3D HC2HL = HC - HL;
            HC2HL.Normalize();
            Vector3D zHC = Vector3D.CrossProduct(HC2HR, HC2HL);
            zHC.Normalize();
            Vector3D HR2HL = HR - HL;
            Vector3D yHC = Vector3D.CrossProduct(HR2HL, zHC);
            yHC.Normalize();
            Vector3D xHC = Vector3D.CrossProduct(yHC, zHC);
            xHC.Normalize();
            Matrix3D hcMat = RotationMatFromRowVecs(xHC, yHC, zHC);
            //orientations[(int)JointType.HipCenter] = MatrixToQuat(hcMat);
            orientations[(int)JointType.HipCenter] = hcMat;
            #endregion
            #region Spine [AKA Spine Mid] Joint Orientation
            Point3D SP = skeleton[(int)JointType.Spine].Position;
            Point3D SR = skeleton[(int)JointType.ShoulderRight].Position;
            Point3D SL = skeleton[(int)JointType.ShoulderLeft].Position;
            Vector3D SP2HC = SP - HC;
            SP2HC.Normalize();
            Vector3D SL2SR = SL - SR;
            SL2SR.Normalize();
            Vector3D zSP = Vector3D.CrossProduct(SL2SR, SP2HC);
            zSP.Normalize();
            Vector3D xSP = Vector3D.CrossProduct(SP2HC, zSP);
            xSP.Normalize();
            Matrix3D spMat = RotationMatFromRowVecs(xSP, SP2HC, zSP);
            //orientations[(int)JointType.Spine] = MatrixToQuat(spMat);
            orientations[(int)JointType.Spine] = spMat;
            #endregion
            #region Shoulder Center [AKA Spine Shoulder] Joint Orientation
            Point3D SC = skeleton[(int)JointType.ShoulderCenter].Position;
            Vector3D SC2SP = SC - SP;
            SC2SP.Normalize();
            Vector3D zSC = Vector3D.CrossProduct(SL2SR, SC2SP);
            zSC.Normalize();
            Vector3D xSC = Vector3D.CrossProduct(SC2SP, zSC);
            xSC.Normalize();
            Matrix3D scMat = RotationMatFromRowVecs(xSC, SC2SP, zSC);
            //orientations[(int)JointType.ShoulderCenter] = MatrixToQuat(scMat);
            orientations[(int)JointType.ShoulderCenter] = scMat;
            #endregion
            #region Head Joint Orientation
            Point3D HD = skeleton[(int)JointType.Head].Position;
            Vector3D HD2SC = HD - SC;
            HD2SC.Normalize();
            Vector3D zHD = Vector3D.CrossProduct(xSC, HD2SC);
            zHD.Normalize();
            Vector3D xHD = Vector3D.CrossProduct(HD2SC, zHD);
            xHD.Normalize();
            Matrix3D hdMat = RotationMatFromRowVecs(xHD, HD2SC, zHD);
            //orientations[(int)JointType.Head] = MatrixToQuat(hdMat);
            orientations[(int)JointType.Head] = hdMat;
            #endregion
            #region Shoulder Left Joint Orientation
            Vector3D SL2SC = SL - SC;
            SL2SC.Normalize();
            Vector3D xSL = Vector3D.CrossProduct(SL2SC, zSC);
            xSL.Normalize();
            Vector3D zSL = Vector3D.CrossProduct(xSL, SL2SC);
            zSL.Normalize();
            Matrix3D slMat = RotationMatFromRowVecs(xSL, SL2SC, zSL);
            //orientations[(int)JointType.ShoulderLeft] = MatrixToQuat(slMat);
            orientations[(int)JointType.ShoulderLeft] = slMat;
            #endregion
            #region Shoulder Right Joint Orientation
            Vector3D SR2SC = SR - SC;
            SR2SC.Normalize();
            Vector3D xSR = Vector3D.CrossProduct(SR2SC, zSC);
            xSR.Normalize();
            Vector3D zSR = Vector3D.CrossProduct(xSR, SR2SC);
            zSR.Normalize();
            Matrix3D srMat = RotationMatFromRowVecs(xSR, SR2SC, zSR);
            //orientations[(int)JointType.ShoulderRight] = MatrixToQuat(srMat);
            orientations[(int)JointType.ShoulderRight] = srMat;
            #endregion
            #region Elbow Left Joint Orientation
            Point3D WL = skeleton[(int)JointType.WristLeft].Position;
            Point3D EL = skeleton[(int)JointType.ElbowLeft].Position;
            Vector3D EL2SL = EL - SL;
            EL2SL.Normalize();
            Vector3D WL2EL = WL - EL;
            WL2EL.Normalize();
            Vector3D xEL;
            Vector3D zEL;
            double cosAngleEL = Vector3D.DotProduct(EL2SL, WL2EL);  //Because these are both unit vectors, the dot product of the two equals cos(angle)
            if (Math.Abs(cosAngleEL) < 0.94)  //The cosine of the angular thresholds are precalculated, this is roughly an angle of 19.5 degrees (160.5 on the other side)
            {
                Vector3D preXel = Vector3D.CrossProduct(EL2SL, WL2EL);
                preXel.Normalize();
                zEL = Vector3D.CrossProduct(preXel, EL2SL);
                zEL.Normalize();
                xEL = Vector3D.CrossProduct(EL2SL, zEL);
                xEL.Normalize();
            }
            else
            {
                double cosAngleEL2 = Vector3D.DotProduct(SC2SP, EL2SL);
                if (cosAngleEL2 <= 0)  //This is equivalent to an angle >= 90 degrees
                {
                    zEL = Vector3D.CrossProduct(EL2SL, xSC);
                    zEL.Normalize();
                    xEL = Vector3D.CrossProduct(EL2SL, zEL);
                    xEL.Normalize();
                }
                else
                {
                    zEL = Vector3D.CrossProduct(EL2SL, SC2SP);
                    zEL.Normalize();
                    xEL = Vector3D.CrossProduct(EL2SL, zEL);
                    xEL.Normalize();
                }
            }
            Matrix3D elMat = RotationMatFromRowVecs(xEL, EL2SL, zEL);
            //orientations[(int)JointType.ElbowLeft] = MatrixToQuat(elMat);
            orientations[(int)JointType.ElbowLeft] = elMat;
            #endregion
            #region Wrist Left Joint Orientation
            Vector3D zWL = Vector3D.CrossProduct(xEL, WL2EL);
            zWL.Normalize();
            Vector3D xWL = Vector3D.CrossProduct(WL2EL, zWL);
            xWL.Normalize();
            Matrix3D wlMat = RotationMatFromRowVecs(xWL, WL2EL, zWL);
            //orientations[(int)JointType.WristLeft] = MatrixToQuat(wlMat);
            orientations[(int)JointType.WristLeft] = wlMat;
            #endregion
            #region Hand Left Joint Orientation
            Point3D HDL = skeleton[(int)JointType.HandLeft].Position;
            Vector3D HDL2WL = HDL - WL;
            HDL2WL.Normalize();
            Vector3D xHDL = Vector3D.CrossProduct(HDL2WL, zWL);
            xHDL.Normalize();
            Vector3D zHDL = Vector3D.CrossProduct(xHDL, HDL2WL);
            zHDL.Normalize();
            Matrix3D hdlMat = RotationMatFromRowVecs(xHDL, HDL2WL, zHDL);
            //orientations[(int)JointType.HandLeft] = MatrixToQuat(hdlMat);
            orientations[(int)JointType.HandLeft] = hdlMat;
            #endregion
            #region Elbow Right Joint Orientation
            Point3D WR = skeleton[(int)JointType.WristRight].Position;
            Point3D ER = skeleton[(int)JointType.ElbowRight].Position;
            Vector3D ER2SR = ER - WR;
            ER2SR.Normalize();
            Vector3D WR2ER = WR - ER;
            WR2ER.Normalize();
            Vector3D xER;
            Vector3D zER;
            double cosAngleER = Vector3D.DotProduct(ER2SR, WR2ER);  //Because these are both unit vectors, the dot product of the two equals cos(angle)
            if (Math.Abs(cosAngleER) < 0.94)  //The cosine of the angular thresholds are precalculated, this is roughly an angle of 19.5 degrees (160.5 on the other side)
            {
                Vector3D preXer = Vector3D.CrossProduct(ER2SR, WR2ER);
                preXer.Normalize();
                zER = Vector3D.CrossProduct(preXer, ER2SR);
                zER.Normalize();
                xER = Vector3D.CrossProduct(ER2SR, zER);
                xER.Normalize();
            }
            else
            {
                double cosAngleER2 = Vector3D.DotProduct(SC2SP, ER2SR);
                if (cosAngleER2 <= 0)  //This is equivalent to an angle >= 90 degrees
                {
                    zER = Vector3D.CrossProduct(ER2SR, xSC);
                    zER.Normalize();
                    xER = Vector3D.CrossProduct(ER2SR, zER);
                    xER.Normalize();
                }
                else
                {
                    zER = Vector3D.CrossProduct(SC2SP, ER2SR);
                    zER.Normalize();
                    xER = Vector3D.CrossProduct(ER2SR, zER);
                    xER.Normalize();
                }
            }
            Matrix3D erMat = RotationMatFromRowVecs(xER, ER2SR, zER);
            //orientations[(int)JointType.ElbowRight] = MatrixToQuat(erMat);
            orientations[(int)JointType.ElbowRight] = erMat;
            #endregion
            #region Wrist Right Joint Orientation
            Vector3D zWR = Vector3D.CrossProduct(xER, WR2ER);
            zWR.Normalize();
            Vector3D xWR = Vector3D.CrossProduct(WR2ER, zWR);
            xWR.Normalize();
            Matrix3D wrMat = RotationMatFromRowVecs(xWR, WR2ER, zWR);
            //orientations[(int)JointType.WristRight] = MatrixToQuat(wrMat);
            orientations[(int)JointType.WristRight] = wrMat;
            #endregion
            #region  Joint Orientation
            Point3D HDR = skeleton[(int)JointType.HandRight].Position;
            Vector3D HDR2WR = HDR - WR;
            HDR2WR.Normalize();
            Vector3D xHDR = Vector3D.CrossProduct(HDR2WR, zWR);
            xHDR.Normalize();
            Vector3D zHDR = Vector3D.CrossProduct(xHDR, HDR2WR);
            zHDR.Normalize();
            Matrix3D hdrMat = RotationMatFromRowVecs(xHDR, HDR2WR, zHDR);
            //orientations[(int)JointType.HandRight] = MatrixToQuat(hdrMat);
            orientations[(int)JointType.HandRight] = hdrMat;
            #endregion
            #region Hip Left Joint Orientation
            Vector3D HL2HC = HL - HC;
            HL2HC.Normalize();
            Vector3D zHL = Vector3D.CrossProduct(xHC, HC2HL);
            zHL.Normalize();
            Vector3D xHL = Vector3D.CrossProduct(zHL, HC2HL);
            xHL.Normalize();
            Matrix3D hlMat = RotationMatFromRowVecs(xHL, HL2HC, zHL);
            //orientations[(int)JointType.HipLeft] = MatrixToQuat(hlMat);
            orientations[(int)JointType.HipLeft] = hlMat;
            #endregion
            #region Knee Left and Ankle Left Joint Orientations
            //Note, these need to be done together as the knee sometimes depends on the ankle
            Point3D KL = skeleton[(int)JointType.KneeLeft].Position;
            Point3D AL = skeleton[(int)JointType.AnkleLeft].Position;
            Vector3D KL2HL = KL - HL;
            KL2HL.Normalize();
            Vector3D AL2KL = AL - KL;
            AL2KL.Normalize();
            Vector3D xKL;
            Vector3D zKL;
            Vector3D xAL;
            Vector3D zAL;
            if (skeleton[(int)JointType.KneeLeft].TrackingState == TrackingState.Tracked)
            {
                double cosAngleKL = Vector3D.DotProduct(KL2HL, AL2KL);
                if (cosAngleKL < 0.972) //This is roughly an angle of 13.5 degrees
                {
                    //Ankle orientation calculation
                    zAL = Vector3D.CrossProduct(AL2KL, xHC);
                    zAL.Normalize();
                    xAL = Vector3D.CrossProduct(AL2KL, zAL);
                    xAL.Normalize();

                    //Knee orientation calculation
                    zKL = Vector3D.CrossProduct(xAL, KL2HL);
                    zKL.Normalize();
                    xKL = Vector3D.CrossProduct(KL2HL, zKL);
                    xKL.Normalize();
                }
                else
                {
                    //Knee orientation calculation
                    zKL = Vector3D.CrossProduct(KL2HL, xHC);
                    zKL.Normalize();
                    xKL = Vector3D.CrossProduct(KL2HL, zKL);
                    xKL.Normalize();

                    //Ankle orientation calculation
                    zAL = Vector3D.CrossProduct(xKL, AL2KL);
                    zAL.Normalize();
                    xAL = Vector3D.CrossProduct(AL2KL, zAL);
                    xAL.Normalize();
                }
            }
            else
            {
                //Knee orientation calculation
                zKL = Vector3D.CrossProduct(KL2HL, xHC);
                zKL.Normalize();
                xKL = Vector3D.CrossProduct(KL2HL, zKL);
                xKL.Normalize();

                //Ankle orientation calculation
                zAL = Vector3D.CrossProduct(xKL, AL2KL);
                zAL.Normalize();
                xAL = Vector3D.CrossProduct(AL2KL, zAL);
                xAL.Normalize();
            }
            Matrix3D klMat = RotationMatFromRowVecs(xKL, KL2HL, zKL);
            //orientations[(int)JointType.KneeLeft] = MatrixToQuat(klMat);
            orientations[(int)JointType.KneeLeft] = klMat;
            Matrix3D alMat = RotationMatFromRowVecs(xAL, AL2KL, zAL);
            //orientations[(int)JointType.AnkleLeft] = MatrixToQuat(alMat);
            orientations[(int)JointType.AnkleLeft] = alMat;
            #endregion
            #region Foot Left Joint Orientation
            Point3D FL = skeleton[(int)JointType.FootLeft].Position;
            Vector3D FL2AL = FL - AL;
            Vector3D zFL = Vector3D.CrossProduct(xAL, FL2AL);
            zFL.Normalize();
            Vector3D xFL = Vector3D.CrossProduct(FL2AL, zFL);
            xFL.Normalize();
            Matrix3D flMat = RotationMatFromRowVecs(xFL, FL2AL, zFL);
            //orientations[(int)JointType.FootLeft] = MatrixToQuat(flMat);
            orientations[(int)JointType.FootLeft] = flMat;
            #endregion
            #region Hip Right Joint Orientation
            Vector3D HR2HC = HR - HC;
            HR2HC.Normalize();
            Vector3D zHR = Vector3D.CrossProduct(HR2HC, xHC);
            zHR.Normalize();
            Vector3D xHR = Vector3D.CrossProduct(HR2HC, zHR);
            xHR.Normalize();
            Matrix3D hrMat = RotationMatFromRowVecs(xHR, HR2HC, zHR);
            //orientations[(int)JointType.HipRight] = MatrixToQuat(hrMat);
            orientations[(int)JointType.HipRight] = hrMat;
            #endregion
            #region Knee Right and Ankle Right Joint Orientations
            //Note, these need to be done together as the knee sometimes depends on the ankle
            Point3D KR = skeleton[(int)JointType.KneeRight].Position;
            Point3D AR = skeleton[(int)JointType.AnkleRight].Position;
            Vector3D KR2HR = KR - HR;
            KR2HR.Normalize();
            Vector3D AR2KR = AR - KR;
            AR2KR.Normalize();
            Vector3D xKR;
            Vector3D zKR;
            Vector3D xAR;
            Vector3D zAR;
            if (skeleton[(int)JointType.KneeRight].TrackingState == TrackingState.Tracked)
            {
                double cosAngleKR = Vector3D.DotProduct(KR2HR, AR2KR);
                if (cosAngleKR < 0.972) //This is roughly an angle of 13.5 degrees
                {
                    //Ankle orientation calculation
                    zAR = Vector3D.CrossProduct(AR2KR, xHC);
                    zAR.Normalize();
                    xAR = Vector3D.CrossProduct(AR2KR, zAR);
                    xAR.Normalize();

                    //Knee orientation calculation
                    zKR = Vector3D.CrossProduct(xAR, KR2HR);
                    zKR.Normalize();
                    xKR = Vector3D.CrossProduct(KR2HR, zKR);
                    xKR.Normalize();
                }
                else
                {
                    //Knee orientation calculation
                    zKR = Vector3D.CrossProduct(KR2HR, xHC);
                    zKR.Normalize();
                    xKR = Vector3D.CrossProduct(KR2HR, zKR);
                    xKR.Normalize();

                    //Ankle orientation calculation
                    zAR = Vector3D.CrossProduct(xKR, AR2KR);
                    zAR.Normalize();
                    xAR = Vector3D.CrossProduct(AR2KR, zAR);
                    xAR.Normalize();
                }
            }
            else
            {
                //Knee orientation calculation
                zKR = Vector3D.CrossProduct(KR2HR, xHC);
                zKR.Normalize();
                xKR = Vector3D.CrossProduct(KR2HR, zKR);
                xKR.Normalize();

                //Ankle orientation calculation
                zAR = Vector3D.CrossProduct(xKR, AR2KR);
                zAR.Normalize();
                xAR = Vector3D.CrossProduct(AR2KR, zAR);
                xAR.Normalize();
            }
            Matrix3D krMat = RotationMatFromRowVecs(xKR, KR2HR, zKR);
            //orientations[(int)JointType.KneeRight] = MatrixToQuat(krMat);
            orientations[(int)JointType.KneeRight] = krMat;
            Matrix3D arMat = RotationMatFromRowVecs(xAR, AR2KR, zAR);
            //orientations[(int)JointType.AnkleRight] = MatrixToQuat(arMat);
            orientations[(int)JointType.AnkleRight] = arMat;
            #endregion
            #region Foot Right Joint Orientation
            Point3D FR = skeleton[(int)JointType.FootRight].Position;
            Vector3D FR2AR = FR - AR;
            Vector3D zFR = Vector3D.CrossProduct(xAR, FR2AR);
            zFR.Normalize();
            Vector3D xFR = Vector3D.CrossProduct(FR2AR, zFR);
            xFR.Normalize();
            Matrix3D frMat = RotationMatFromRowVecs(xFR, FR2AR, zFR);
            //orientations[(int)JointType.FootRight] = MatrixToQuat(frMat);
            orientations[(int)JointType.FootRight] = frMat;
            #endregion

            return orientations;
        }

        //Creates a rotation matrix from three vectors that represent the first three rows of the matrix
        private Matrix3D RotationMatFromRowVecs(Vector3D x, Vector3D y, Vector3D z)
        {
            Matrix3D m = new Matrix3D();

            m.M11 = x.X;
            m.M12 = x.Y;
            m.M13 = x.Z;
            m.M14 = 0;

            m.M21 = y.X;
            m.M22 = y.Y;
            m.M23 = y.Z;
            m.M24 = 0;

            m.M31 = z.X;
            m.M32 = z.Y;
            m.M33 = z.Z;
            m.M34 = 0;

            m.OffsetX = 0;
            m.OffsetY = 0;
            m.OffsetZ = 0;
            m.M44 = 1;

            return m;
        }
        #endregion

        private TrackingState GetSkeletonTrackingState(KinectSkeleton skeleton)
        {
            bool anyTracked = false;
            bool anyInferred = false;
            TrackingState state = TrackingState.NotTracked;

            for (int i = 0; i < skeleton.skeleton.Count; i++)
            {
                if (skeleton.skeleton[i].TrackingState == TrackingState.Tracked)
                {
                    anyTracked = true;
                    state = TrackingState.Tracked;
                    break;
                }
                else if (skeleton.skeleton[i].TrackingState == TrackingState.Inferred)
                {
                    anyInferred = true;
                }
            }

            if (!anyTracked && anyInferred)
            {
                state = TrackingState.Inferred;
            }
            

            return state;
        }

        private TrackingState GetJointTrackingState(EigenWrapper.Matrix covariance, DateTime lastTrackedTime)
        {
            double logNorm = Math.Log(covariance.Norm());
            TrackingState state = TrackingState.NotTracked;
            TimeSpan trackingAge = DateTime.UtcNow - lastTrackedTime;

            //For debugging only
            //System.Diagnostics.Debug.WriteLine("Joint has age {0} ms with norm {1}", trackingAge.TotalMilliseconds, logNorm);

            //TODO: Test to see if these thresholds need to be modified
            if (trackingAge.TotalMilliseconds > 1000)
            {
                //If the tracking age is old, but it still has a decent prediction, the joint is inferred
                if (logNorm < 4)
                {
                    state = TrackingState.Inferred;
                }
            }
            else
            {
                //If the tracking age is new, determine if it is tracked, inferred, or not tracked (default) based on how good the prediction is
                if (logNorm < 2)
                {
                    state = TrackingState.Tracked;
                }
                else if (logNorm < 4)
                {
                    state = TrackingState.Inferred;
                }
            }

            return state;
        }

        private void IntegrateLeftHandGrabState(KinectSkeleton skeleton, bool isReversed)
        {
            if (!isReversed)
            {
                if (skeleton.skeleton[JointType.HandLeft].TrackingState == TrackingState.Tracked)
                {
                    lhFilter.IntegrateMeasurement(skeleton.leftHandClosed);
                }
            }
            else
            {
                if (skeleton.skeleton[JointType.HandRight].TrackingState == TrackingState.Tracked)
                {
                    lhFilter.IntegrateMeasurement(skeleton.rightHandClosed);
                }
            }
        }

        private void IntegrateRightHandGrabState(KinectSkeleton skeleton, bool isReversed)
        {
            if (!isReversed)
            {
                if (skeleton.skeleton[JointType.HandRight].TrackingState == TrackingState.Tracked)
                {
                    rhFilter.IntegrateMeasurement(skeleton.rightHandClosed);
                }
            }
            else
            {
                if (skeleton.skeleton[JointType.HandLeft].TrackingState == TrackingState.Tracked)
                {
                    rhFilter.IntegrateMeasurement(skeleton.rightHandClosed);
                }
            }
        }

        private int GetJointIndex(int index, bool isReversed)
        {
            if (!isReversed)
            {
                return index;
            }
            else
            {
                if (((JointType)index) == JointType.ShoulderLeft)
                {
                    return (int)JointType.ShoulderRight;
                }
                else if (((JointType)index) == JointType.ShoulderRight)
                {
                    return (int)JointType.ShoulderLeft;
                }
                else if (((JointType)index) == JointType.ElbowLeft)
                {
                    return (int)JointType.ElbowRight;
                }
                else if (((JointType)index) == JointType.ElbowRight)
                {
                    return (int)JointType.ElbowLeft;
                }
                else if (((JointType)index) == JointType.WristLeft)
                {
                    return (int)JointType.WristRight;
                }
                else if (((JointType)index) == JointType.WristRight)
                {
                    return (int)JointType.WristLeft;
                }
                else if (((JointType)index) == JointType.HandLeft)
                {
                    return (int)JointType.HandRight;
                }
                else if (((JointType)index) == JointType.HandRight)
                {
                    return (int)JointType.HandLeft;
                }
                else if (((JointType)index) == JointType.HandTipLeft)
                {
                    return (int)JointType.HandTipRight;
                }
                else if (((JointType)index) == JointType.HandTipRight)
                {
                    return (int)JointType.HandTipLeft;
                }
                else if (((JointType)index) == JointType.ThumbLeft)
                {
                    return (int)JointType.ThumbRight;
                }
                else if (((JointType)index) == JointType.ThumbRight)
                {
                    return (int)JointType.ThumbLeft;
                }
                else if (((JointType)index) == JointType.HipLeft)
                {
                    return (int)JointType.HipRight;
                }
                else if (((JointType)index) == JointType.HipRight)
                {
                    return (int)JointType.HipLeft;
                }
                else if (((JointType)index) == JointType.KneeLeft)
                {
                    return (int)JointType.KneeRight;
                }
                else if (((JointType)index) == JointType.KneeRight)
                {
                    return (int)JointType.KneeLeft;
                }
                else if (((JointType)index) == JointType.AnkleLeft)
                {
                    return (int)JointType.AnkleRight;
                }
                else if (((JointType)index) == JointType.AnkleRight)
                {
                    return (int)JointType.AnkleLeft;
                }
                else if (((JointType)index) == JointType.FootLeft)
                {
                    return (int)JointType.FootRight;
                }
                else if (((JointType)index) == JointType.FootRight)
                {
                    return (int)JointType.FootLeft;
                }
                else  //The center joints don't need to be flipped
                {
                    return index;
                }
            }
        }
    }

    //This is a degenerate scalar random constant version of the Kalman filter used to estimate the hand state
    internal class HandFilter
    {
        //Constants
        const double Qmin = 0.001;
        const double R = 0.25; //Process variance 

        //States to hold from increment to increment
        private double xLast = 0.0;
        private double pLast = 1000;
        private DateTime lastMeasurement = DateTime.MinValue;

        internal void IntegrateMeasurement(bool handClosed)
        {
            //Convert the state to a double "measurement"
            double z = 0;
            if (handClosed)
            {
                z = 1;
            }

            //Prediction step (x doesn't need to be predicted since it is assumed constant)
            double Pminus = pLast + DerateQ((DateTime.UtcNow - lastMeasurement).TotalMilliseconds);

            //Update step
            double K = Pminus / (Pminus + R);
            xLast = xLast + K * (z - xLast);
            pLast = (1 - K) * Pminus;
        }

        //Because we are assuming a constant, all we have to do the "predict" the measurement is just grab the last state and threshold it
        internal bool PredictMeasurement()
        {
            if (xLast > 0.5)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //TODO: is there a better way to do this?
        //This increases the Q as the time since the last sample gets longer to cause new measurements to be more trusted than old ones
        private double DerateQ(double ageMS)
        {
            return Qmin * ageMS;
        }
    }
}
