using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using WindowsPreview.Kinect;
using System.Diagnostics;

namespace BeatBeatFitness
{
    class Misc
    {
        public Misc()
        {

        }

        /// <summary>
        /// Menentukan jarak abs 2 point
        /// </summary>
        /// <param name="a">point 1</param>
        /// <param name="b"> point 2</param>
        /// <returns></returns>
        public double TwoPointDistance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        }

        /// <summary>
        /// return sudut antara 3 titik
        /// </summary>
        /// <param name="p">titik pivot</param>
        /// <param name="a">titik ujung dengan joint sequence paling dalam, patokan</param>
        /// <param name="b">titik ujung denan joint sequence paling luar</param>
        /// <returns> sudut di titik p , searah jarum jam positif, berlawanan negatif</returns>
        public double AngleFromThreePoints(Point p, Point a, Point b)
        {
            try
            {
                double sideA = TwoPointDistance(a, p);
                double sideB = TwoPointDistance(b, p);
                double sideC = TwoPointDistance(a, b);

                double angleDirection = Math.Sign(((a.X - p.X) * (b.Y - p.Y)) - ((a.Y - p.Y) * (b.X - p.X)));
                if (angleDirection == 0) angleDirection = 1;

                return angleDirection * Math.Acos((Math.Pow(sideA, 2) + Math.Pow(sideB, 2) - Math.Pow(sideC, 2)) / (2 * sideA * sideB));
            }
            catch (Exception exc)
            {
                Debug.WriteLine("angle from 3 pts fail > :" + exc.Message.ToString());
                return 1;
            }
        }
    }
    public class Avatar
    {
        //TODO : save it to xml
        public string[] avaSourceIndex;

        //ignore xml
        BitmapImage[] imageSource;
        public enum ImageSourceType { head, handUp, handDown, foot }
        public Image[] avaImages;
        public enum AvaImageType { head, handleft, handright, footright, footleft }

        private Misc misc;
        private Dictionary<JointType, Point> activeJointPointDict;
        private double AvaScale; 

        public Avatar(string hatAvaIndex, string topAvaIndex, string bottomAvaIndex)
        {
            misc = new Misc();

            //load from string later
            avaSourceIndex = new string[] { hatAvaIndex, topAvaIndex, bottomAvaIndex };
            imageSource = new BitmapImage[]{ 
            new BitmapImage(new Uri("ms-appx:///Assets/Avatar/Head"+avaSourceIndex[0]+".png", UriKind.Absolute)),
            new BitmapImage(new Uri("ms-appx:///Assets/Avatar/HandUp"+avaSourceIndex[1]+".png", UriKind.Absolute)),
            new BitmapImage(new Uri("ms-appx:///Assets/Avatar/HandDown"+avaSourceIndex[1]+".png", UriKind.Absolute)),
            new BitmapImage(new Uri("ms-appx:///Assets/Avatar/Foot"+avaSourceIndex[2]+".png", UriKind.Absolute))
            };
            //generate appropriate image sing bitmap image
            Image headImage = new Image();
            headImage.Source = imageSource[(int)ImageSourceType.head];

            Image handRightImage = new Image();
            handRightImage.Source = imageSource[(int)ImageSourceType.handDown];

            Image handLeftImage = new Image();
            handLeftImage.Source = imageSource[(int)ImageSourceType.handDown];

            Image footRightImage = new Image();
            footRightImage.Source = imageSource[(int)ImageSourceType.foot];

            Image footLeftImage = new Image();
            footLeftImage.Source = imageSource[(int)ImageSourceType.foot];

            //insert image to avaImage
            avaImages = new Image[] { headImage, handRightImage, handLeftImage, footRightImage, footLeftImage };
        }
        public void UpdateAvatar(Dictionary<JointType, Point> jointPointDict)
        {
            //update head
            activeJointPointDict = jointPointDict;
            AvaScale = misc.TwoPointDistance(jointPointDict[JointType.ShoulderLeft], jointPointDict[JointType.ShoulderRight]);
            try
            {
                UpdateHead();
                UpdateHand();
                UpdateFoot();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            
        }
        private void UpdateHead()
        {
            double squareLength = (Definitions.headToHeadAvaRatio * AvaScale);
            avaImages[(int)AvaImageType.head].Width = squareLength;
            avaImages[(int)AvaImageType.head].Height = avaImages[(int)AvaImageType.head].Width;

            RotateTransform headRotate = new RotateTransform();
            double headAngle = misc.AngleFromThreePoints(activeJointPointDict[JointType.Neck], new Point(activeJointPointDict[JointType.Neck].X, activeJointPointDict[JointType.Neck].Y - 1), activeJointPointDict[JointType.Head]);
            headRotate.CenterX = avaImages[(int)AvaImageType.head].Width / 2;
            headRotate.CenterY = avaImages[(int)AvaImageType.head].Height / 2;
            headRotate.Angle = headAngle * (360 / (2 * Math.PI));
            avaImages[(int)AvaImageType.head].RenderTransform = headRotate;
            Canvas.SetTop(avaImages[(int)AvaImageType.head], activeJointPointDict[JointType.Head].Y - avaImages[(int)AvaImageType.head].Width / 2);
            Canvas.SetLeft(avaImages[(int)AvaImageType.head], activeJointPointDict[JointType.Head].X - avaImages[(int)AvaImageType.head].Width / 2);

            
        }
        private void UpdateHand()
        {
            #region right hand
            //decide the size
            double squareLengthRight = (Definitions.handToAvaRatio * AvaScale);
            avaImages[(int)AvaImageType.handright].Width = squareLengthRight;
            avaImages[(int)AvaImageType.handright].Height = avaImages[(int)AvaImageType.handright].Width;

            //decide the rotation
            RotateTransform handRightRotate = new RotateTransform();
            double handRightAngle = misc.AngleFromThreePoints(activeJointPointDict[JointType.WristRight], new Point(activeJointPointDict[JointType.WristRight].X, activeJointPointDict[JointType.WristRight].Y - 1), activeJointPointDict[JointType.HandRight]);
            handRightRotate.CenterX = avaImages[(int)AvaImageType.handright].Width / 2;
            handRightRotate.CenterY = avaImages[(int)AvaImageType.handright].Height / 2;
            handRightRotate.Angle = handRightAngle * (360 / (2 * Math.PI));

            //decide flipping image sprite
            avaImages[(int)AvaImageType.handright].Source = imageSource[(int)ImageSourceType.handUp];
            if (handRightRotate.Angle > Definitions.handFlipAngleTreshold || handRightRotate.Angle < Definitions.handFlipAngleTreshold - 180) avaImages[(int)AvaImageType.handright].Source = imageSource[(int)ImageSourceType.handDown];

            //decide the mirroring img
            ScaleTransform handRightScale = new ScaleTransform();
            handRightScale.CenterX = handRightRotate.CenterX;
            handRightScale.CenterY = handRightRotate.CenterY;
            handRightScale.ScaleX = 1;

            //transform according to sequence
            TransformGroup handRightTransform = new TransformGroup();
            handRightTransform.Children.Add(handRightScale);
            handRightTransform.Children.Add(handRightRotate);

            //apply transform
            avaImages[(int)AvaImageType.handright].RenderTransform = handRightTransform;

            //set position
            Canvas.SetTop(avaImages[(int)AvaImageType.handright], activeJointPointDict[JointType.HandRight].Y - avaImages[(int)AvaImageType.handright].Width / 2);
            Canvas.SetLeft(avaImages[(int)AvaImageType.handright], activeJointPointDict[JointType.HandRight].X - avaImages[(int)AvaImageType.handright].Height / 2);
            #endregion

            #region left hand
            //decide the size
            double squareLengthLeft = (Definitions.handToAvaRatio * AvaScale);
            avaImages[(int)AvaImageType.handleft].Width = squareLengthLeft;
            avaImages[(int)AvaImageType.handleft].Height = avaImages[(int)AvaImageType.handleft].Width;

            //decide the rotation
            RotateTransform handLeftRotate = new RotateTransform();
            double handLeftAngle = misc.AngleFromThreePoints(activeJointPointDict[JointType.WristLeft], new Point(activeJointPointDict[JointType.WristLeft].X, activeJointPointDict[JointType.WristLeft].Y - 1), activeJointPointDict[JointType.HandLeft]);
            handLeftRotate.CenterX = avaImages[(int)AvaImageType.handleft].Width / 2;
            handLeftRotate.CenterY = avaImages[(int)AvaImageType.handleft].Height / 2;
            handLeftRotate.Angle = handLeftAngle * (360 / (2 * Math.PI));

            //decide flipping image sprite
            avaImages[(int)AvaImageType.handleft].Source = imageSource[(int)ImageSourceType.handUp];
            if (handLeftRotate.Angle < -1* Definitions.handFlipAngleTreshold || handLeftRotate.Angle > -1* Definitions.handFlipAngleTreshold + 180) avaImages[(int)AvaImageType.handleft].Source = imageSource[(int)ImageSourceType.handDown];

            //decide the mirroring img
            ScaleTransform handLeftScale = new ScaleTransform();
            handLeftScale.CenterX = handLeftRotate.CenterX;
            handLeftScale.CenterY = handLeftRotate.CenterY;
            handLeftScale.ScaleX = -1;

            //transform according to sequence
            TransformGroup handLeftTransform = new TransformGroup();
            handLeftTransform.Children.Add(handLeftScale);
            handLeftTransform.Children.Add(handLeftRotate);

            //apply transform
            avaImages[(int)AvaImageType.handleft].RenderTransform = handLeftTransform;

            //set position
            Canvas.SetTop(avaImages[(int)AvaImageType.handleft], activeJointPointDict[JointType.HandLeft].Y - avaImages[(int)AvaImageType.handleft].Width / 2);
            Canvas.SetLeft(avaImages[(int)AvaImageType.handleft], activeJointPointDict[JointType.HandLeft].X - avaImages[(int)AvaImageType.handleft].Height / 2);
            #endregion

        }
        private void UpdateFoot()
        {
            #region right foot
            //decide the size
            double squareLengthRight = (Definitions.footToAvaRatio * AvaScale);
            avaImages[(int)AvaImageType.footright].Width = squareLengthRight;
            avaImages[(int)AvaImageType.footright].Height = avaImages[(int)AvaImageType.footright].Width;

            //decide the rotation
            RotateTransform footRightRotate = new RotateTransform();
            double footRightAngle = misc.AngleFromThreePoints(activeJointPointDict[JointType.KneeRight], new Point(activeJointPointDict[JointType.KneeRight].X, activeJointPointDict[JointType.KneeRight].Y + 1), activeJointPointDict[JointType.AnkleRight]);
            footRightRotate.CenterX = avaImages[(int)AvaImageType.footright].Width / 2;
            footRightRotate.CenterY = avaImages[(int)AvaImageType.footright].Height / 2;
            footRightRotate.Angle = footRightAngle * (360 / (2 * Math.PI));

            //decide the mirroring img
            ScaleTransform footRightScale = new ScaleTransform();
            footRightScale.ScaleX = 1;

            //transform according to sequence
            TransformGroup footRightTransform = new TransformGroup();
            footRightTransform.Children.Add(footRightScale);
            footRightTransform.Children.Add(footRightRotate);

            //apply transform
            avaImages[(int)AvaImageType.footright].RenderTransform = footRightTransform;

            //set position
            Canvas.SetTop(avaImages[(int)AvaImageType.footright], activeJointPointDict[JointType.AnkleRight].Y - avaImages[(int)AvaImageType.footright].Width / 2);
            Canvas.SetLeft(avaImages[(int)AvaImageType.footright], activeJointPointDict[JointType.AnkleRight].X - avaImages[(int)AvaImageType.footright].Width / 2);
            #endregion
            
            #region left foot
            //decide the size
            double squareLengthLeft = (Definitions.footToAvaRatio * AvaScale);
            avaImages[(int)AvaImageType.footleft].Width = squareLengthLeft;
            avaImages[(int)AvaImageType.footleft].Height = avaImages[(int)AvaImageType.footleft].Width;

            //decide the rotation
            RotateTransform footLeftRotate = new RotateTransform();
            double footLeftAngle = misc.AngleFromThreePoints(activeJointPointDict[JointType.KneeLeft], new Point(activeJointPointDict[JointType.KneeLeft].X, activeJointPointDict[JointType.KneeLeft].Y + 1), activeJointPointDict[JointType.AnkleLeft]);
            footLeftRotate.CenterX = avaImages[(int)AvaImageType.footleft].Width / 2;
            footLeftRotate.CenterY = avaImages[(int)AvaImageType.footleft].Height / 2;
            footLeftRotate.Angle = footLeftAngle * (360 / (2 * Math.PI));

            //decide the mirroring img
            ScaleTransform footLeftScale = new ScaleTransform();
            footLeftScale.CenterX = footLeftRotate.CenterX;
            footLeftScale.CenterY = footLeftRotate.CenterY;
            footLeftScale.ScaleX = -1;

            //transform according to sequence
            TransformGroup footLeftTransform = new TransformGroup();
            footLeftTransform.Children.Add(footLeftScale);
            footLeftTransform.Children.Add(footLeftRotate);

            //apply transform
            avaImages[(int)AvaImageType.footleft].RenderTransform = footLeftTransform;

            //set position
            Canvas.SetTop(avaImages[(int)AvaImageType.footleft], activeJointPointDict[JointType.AnkleLeft].Y - avaImages[(int)AvaImageType.footleft].Width / 2);
            Canvas.SetLeft(avaImages[(int)AvaImageType.footleft], activeJointPointDict[JointType.AnkleLeft].X - avaImages[(int)AvaImageType.footleft].Width / 2);
            #endregion
        }
    }

}
