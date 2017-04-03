using System;
using System.Collections.Generic;
using System.Linq;
using WindowsPreview.Kinect;
using Windows.Foundation;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI;

using System.Diagnostics;

namespace BeatBeatFitness
{
    class PunchManager
    {
        public NoteLibrary activeNoteLibrary { get; set; }
        public List<int> activeNoteSequence { get; set; }
        public float activeMusicBPM { get; set; }
        public List<PunchingEllipse> afterPunchHistory { get; set; }
        public List<Line> activeLine { get; set; }

        private List<PunchingNote> activePunchingNotes;
        private int[] lastGeneratedPunchNote;
        private Misc misc;

        /// <summary>
        /// <para>JointSequence </para>
        /// <para>array urutan sendi anggota gerak biar gampang ngitung berantai</para>
        /// <para>baris->anggota gerak (0 rightarm, 1 leftarm, 2 right leg, 3 left leg)</para>
        /// <para>kolom->urutan sendi  (dari dalam keluar)</para>
        /// </summary>
        private JointType[,] jointSequence = new JointType[,]{ { JointType.SpineShoulder, JointType.ShoulderRight, JointType.ElbowRight, JointType.HandRight }, 
                                                   { JointType.SpineShoulder, JointType.ShoulderLeft, JointType.ElbowLeft, JointType.HandLeft }, 
                                                   {JointType.SpineBase, JointType.HipRight, JointType.KneeRight,JointType.AnkleRight }, 
                                                   {JointType.SpineBase, JointType.HipLeft, JointType.KneeLeft,JointType.AnkleLeft } };

        /// <summary>
        /// sendi2 yang dihitung sudutnya
        /// </summary>
        public static JointType[] observedJoints =   {JointType.ShoulderRight, 
                                                   JointType.ShoulderLeft, 
                                                   JointType.ElbowRight, 
                                                   JointType.ElbowLeft,
                                                   JointType.HipLeft,
                                                   JointType.HipRight,
                                                   JointType.KneeLeft,
                                                   JointType.KneeRight};

        enum LimbNumber : int { rightArm = 0, leftArm = 1, rightLeg = 2, leftLeg = 3, head = 4 }
        private double[] predicateRatioLimit = { Definitions.RatioMiss, Definitions.RatioBad, Definitions.RatioCool, Definitions.RatioGreat, Definitions.RatioPerfect };

        public enum GestureType { none, pause, select, raisehand }

        public PunchManager()
        {
            afterPunchHistory = new List<PunchingEllipse>();
            activePunchingNotes = new List<PunchingNote>();
            activeLine = new List<Line>();
            lastGeneratedPunchNote = new int[] { 0, 0 };
            misc = new Misc();
        }

        public Dictionary<JointType, double> JointPointToAngle(Dictionary<JointType, Point> _jointPositionDict)
        {
            Dictionary<JointType, double> angleJointValueDict = new Dictionary<JointType, double>();
            try
            {
                foreach (var _observedJoint in observedJoints)
                {
                    for (int i = 0; i < jointSequence.GetUpperBound(0) + 1; i++) //untuk setiap anggota gerak.. do...
                    {
                        for (int j = 0; j < jointSequence.GetUpperBound(1) + 1; j++)
                        {
                            if (_observedJoint == jointSequence[i, j])
                            {
                                double jointAngle = misc.AngleFromThreePoints(_jointPositionDict[jointSequence[i, j]], _jointPositionDict[jointSequence[i, j - 1]], _jointPositionDict[jointSequence[i, j + 1]]);
                                angleJointValueDict.Add(_observedJoint, jointAngle);
                                break;
                            }

                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("finding joint angle fail > " + exc.Message.ToString());
            }
            return angleJointValueDict;
        }



        /// <summary>
        /// Menentuka titik ke tiga dari 2 titik dan 1 sudut
        /// CALC I
        /// </summary>
        /// <param name="pivotAngle">ambil dari note</param>
        /// <param name="butt">ambil dari joint point</param>
        /// <param name="pivot">ambil dari joint point</param>
        /// <param name="segment">hitung dari joint point</param>
        /// <returns></returns>
        private Point ThirdPointPosition(double pivotAngle, Point butt, Point pivot, double segment)
        {
            //converting trigonometry quadran to game quadran
            double offsetAngle = misc.AngleFromThreePoints(pivot, new Point(butt.X, pivot.Y), butt);
            if (butt.X - pivot.X < 0) offsetAngle += Math.PI;
            return new Point(pivot.X + (Math.Cos(offsetAngle + pivotAngle) * segment), pivot.Y + (Math.Sin(offsetAngle + pivotAngle) * segment));
        }

        /// <summary>
        /// Hitung punching pont dari joint berantai
        /// </summary>
        /// <param name="pivotAngleA">ambil dari note</param>
        /// <param name="pivotAngleB">ambil dari note</param>
        /// <param name="butt">pangkal segmen pertama, ambil dari joint point</param>
        /// <param name="pivot">pivot segmen pertama, ambil dari joinr point</param>
        /// <param name="segmentA">segmen ruas pertama, hitung dari pivot</param>
        /// <param name="segmentB">segmen ruas kedua, hitung dari joint point</param>
        /// <returns></returns>
        private Point FourthPointPosition(double pivotAngleA, double pivotAngleB, Point butt, Point pivot, double segmentA, double segmentB)
        {
            Point newPivot = ThirdPointPosition(pivotAngleA, butt, pivot, segmentA);
            Point newButt = pivot;
            return ThirdPointPosition(pivotAngleB, newButt, newPivot, segmentB);
        }

        private bool CanvasBoundCheck(Point z)
        {
            if ((z.X >= 0 && z.X <= Definitions.canvasWidth) && (z.Y >= 0 && z.Y <= Definitions.canvasHeight))
            {
                return true;
            }
            return false;
        }
        private bool JointPointValidation(Dictionary<JointType, Point> jointPoints)
        {
            if (jointPoints.Count != Definitions.maxJointNumber) return false;
            foreach (Point point in jointPoints.Values)
            {
                if (!CanvasBoundCheck(point)) return false;
            }
            return true;
        }

        /// <summary>
        /// Listing semua ellipse yang jadi punch key 
        /// </summary>
        /// <param name="_runningGameTime"></param>
        /// <param name="jointPoints"></param>
        /// <returns></returns>
        public List<Ellipse> PopulatePunchKey(double _runningGameTime, Dictionary<JointType, Point> jointPoints, int activeBodyNumber)
        {
            List<Ellipse> activeEllipses = new List<Ellipse>();
            if (!JointPointValidation(jointPoints)) return activeEllipses;

            //running game time HARUS selalu dibawah 1000 saat awal permulaan
            double beatPosition = (_runningGameTime / 1000) / (60 / activeMusicBPM);

            //iterasi sepanjang note2 yang aktif
            int startCheckedNote = (int)beatPosition;
            if (beatPosition - (double)((int)beatPosition) > Definitions.PunchKeyOffset) startCheckedNote++;
            for (int checkedNote = startCheckedNote; checkedNote < startCheckedNote + Definitions.PunchKeyLifeTime; checkedNote++)
            {
                //check if checked note has been added (converted to ellipsenote) to history 
                if (lastGeneratedPunchNote[activeBodyNumber] < checkedNote)
                {
                    lastGeneratedPunchNote[activeBodyNumber] = checkedNote;
                    try
                    {
                        #region create a new punching note
                        //the checked note sequence is on duty, and never been assigned, lets create ellipses based on the note!                      
                        if (activeNoteSequence[checkedNote % activeNoteSequence.Count] != 0)
                        {
                            PunchingNote newPunchingNote = new PunchingNote();
                            newPunchingNote.punchNoteNumber = checkedNote;
                            //extract the note from library
                            var noteQuery = from note in activeNoteLibrary.noteLibrary
                                            where note.noteId == activeNoteSequence[checkedNote % activeNoteSequence.Count]
                                            select note;
                            var selectedNote = (Note)noteQuery.FirstOrDefault();
                            var angleJointDict = selectedNote.UnParseDict();
                            foreach (var punchingJoint in selectedNote.serPunchingJoints.ToList())
                            {
                                //add an ellipses on each note (each ser punching joint correspond to one ellipse)
                                PunchingEllipse newPunchingEllipse = new PunchingEllipse();

                                #region calculating punching joint point
                                int selectedLimbNumber = 0;
                                bool expandedJoint = false;
                                Point punchingJointPoint = new Point();
                                switch (punchingJoint)
                                {
                                    case JointType.HandRight:
                                    case JointType.ElbowRight:
                                        selectedLimbNumber = (int)LimbNumber.rightArm;
                                        break;
                                    case JointType.HandLeft:
                                    case JointType.ElbowLeft:
                                        selectedLimbNumber = (int)LimbNumber.leftArm;
                                        break;
                                    case JointType.KneeRight:
                                    case JointType.AnkleRight:
                                        selectedLimbNumber = (int)LimbNumber.rightLeg;
                                        break;
                                    case JointType.KneeLeft:
                                    case JointType.AnkleLeft:
                                        selectedLimbNumber = (int)LimbNumber.leftLeg;
                                        break;
                                }
                                switch (punchingJoint)
                                {
                                    case JointType.HandRight:
                                    case JointType.HandLeft:
                                    case JointType.AnkleRight:
                                    case JointType.AnkleLeft:
                                        expandedJoint = true;
                                        break;
                                    default:
                                        expandedJoint = false;
                                        break;
                                }

                                //execute ellipse location calculation
                                if (!expandedJoint)
                                {
                                    punchingJointPoint = ThirdPointPosition(angleJointDict[jointSequence[selectedLimbNumber, 1]],
                                                                        jointPoints[jointSequence[selectedLimbNumber, 0]],
                                                                        jointPoints[jointSequence[selectedLimbNumber, 1]],
                                                                        misc.TwoPointDistance(jointPoints[jointSequence[selectedLimbNumber, 1]], jointPoints[jointSequence[selectedLimbNumber, 2]]));
                                }
                                else if (expandedJoint && punchingJoint != JointType.Head)
                                {
                                    punchingJointPoint = FourthPointPosition(angleJointDict[jointSequence[selectedLimbNumber, 1]],
                                                                        angleJointDict[jointSequence[selectedLimbNumber, 2]],
                                                                        jointPoints[jointSequence[selectedLimbNumber, 0]],
                                                                        jointPoints[jointSequence[selectedLimbNumber, 1]],
                                                                        misc.TwoPointDistance(jointPoints[jointSequence[selectedLimbNumber, 1]], jointPoints[jointSequence[selectedLimbNumber, 2]]),
                                                                        misc.TwoPointDistance(jointPoints[jointSequence[selectedLimbNumber, 2]], jointPoints[jointSequence[selectedLimbNumber, 3]])
                                                                        );
                                }

                                //generate special punch kepala (dengan clue shoulder)
                                if (punchingJoint == JointType.Head)
                                {
                                    if (angleJointDict[JointType.ShoulderRight]> 0)
                                    {
                                        punchingJointPoint = new Point(jointPoints[JointType.Head].X, jointPoints[JointType.SpineShoulder].Y);
                                    }
                                    else
                                    {
                                        punchingJointPoint = new Point(jointPoints[JointType.Head].X, jointPoints[JointType.Head].Y - misc.TwoPointDistance(jointPoints[JointType.Head], jointPoints[JointType.Neck]));
                                    }

                                }

                                //re generate special punch kaki 
                                if (punchingJoint == JointType.AnkleLeft || punchingJoint == JointType.AnkleRight)
                                {
                                    var groundJoint = JointType.FootLeft;
                                    if (punchingJoint == JointType.AnkleRight) groundJoint = JointType.FootRight;

                                    punchingJointPoint.Y = jointPoints[groundJoint].Y;
                                }

                                //TODO pastikan semua punching point masih di dalam arena kinect depth
                                if (punchingJointPoint.Y < 0) punchingJointPoint.Y = 0;
                                if (punchingJointPoint.X < 0) punchingJointPoint.X = 0;
                                #endregion

                                newPunchingEllipse.ellipseSpawnPoint = punchingJointPoint;

                                //setup punch elipse
                                switch (punchingJoint)
                                {
                                    case JointType.AnkleRight:
                                    case JointType.ElbowRight:
                                    case JointType.HandRight:
                                    case JointType.FootRight:
                                        newPunchingEllipse.ellipse.Fill = new SolidColorBrush(Definitions.rightPunchColor);
                                        break;
                                    case JointType.AnkleLeft:
                                    case JointType.ElbowLeft:
                                    case JointType.HandLeft:
                                    case JointType.FootLeft:
                                        newPunchingEllipse.ellipse.Fill = new SolidColorBrush(Definitions.leftPunchColor);
                                        break;
                                    case JointType.Head:
                                        newPunchingEllipse.ellipse.Fill = new SolidColorBrush(Definitions.headPunchColor);
                                        break;
                                }
                                newPunchingEllipse.ellipse.Width = Definitions.PunchKeyDiameter;
                                newPunchingEllipse.ellipse.Height = newPunchingEllipse.ellipse.Width;
                                Canvas.SetLeft(newPunchingEllipse.ellipse, newPunchingEllipse.ellipseSpawnPoint.X - 0.5 * Definitions.PunchKeyDiameter);
                                Canvas.SetTop(newPunchingEllipse.ellipse, newPunchingEllipse.ellipseSpawnPoint.Y - 0.5 * Definitions.PunchKeyDiameter);
                                newPunchingEllipse.ellipse.Visibility = Windows.UI.Xaml.Visibility.Visible;

                                //setup timer elipse
                                newPunchingEllipse.timerEllipse.Fill = new SolidColorBrush(Colors.Transparent);
                                newPunchingEllipse.timerEllipse.Stroke = new SolidColorBrush(Definitions.timerColor);
                                if (activeBodyNumber == 1) newPunchingEllipse.timerEllipse.Stroke = new SolidColorBrush(Colors.Red);
                                newPunchingEllipse.timerEllipse.StrokeThickness = Definitions.PunchTimerStrokeThickness;
                                newPunchingEllipse.timerEllipse.Width = Definitions.PunchTimerDiameter;
                                newPunchingEllipse.timerEllipse.Height = newPunchingEllipse.timerEllipse.Width;
                                Canvas.SetLeft(newPunchingEllipse.timerEllipse, newPunchingEllipse.ellipseSpawnPoint.X - 0.5 * Definitions.PunchTimerDiameter);
                                Canvas.SetTop(newPunchingEllipse.timerEllipse, newPunchingEllipse.ellipseSpawnPoint.Y - 0.5 * Definitions.PunchTimerDiameter);
                                newPunchingEllipse.timerEllipse.Visibility = Windows.UI.Xaml.Visibility.Visible;

                                //setup helping line
                                newPunchingEllipse.helpingLine.Stroke = new SolidColorBrush(Definitions.lineColor);
                                newPunchingEllipse.helpingLine.X1 = jointPoints[punchingJoint].X;
                                newPunchingEllipse.helpingLine.Y1 = jointPoints[punchingJoint].Y;
                                newPunchingEllipse.helpingLine.X2 = punchingJointPoint.X;
                                newPunchingEllipse.helpingLine.Y2 = punchingJointPoint.Y;
                                newPunchingEllipse.helpingLine.StrokeThickness = 2;
                                newPunchingEllipse.helpingLine.Visibility = Windows.UI.Xaml.Visibility.Visible;
                                activeLine.Add(newPunchingEllipse.helpingLine);

                                newPunchingEllipse.punchingJoint = punchingJoint;
                                newPunchingNote.punchingEllipses.Add(newPunchingEllipse);
                            }
                            activePunchingNotes.Add(newPunchingNote);
                        }
                        #endregion
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("fail creating ellipse and note >" + exc.Message.ToString());
                    }
                }

                //cleaning (nge miss in) old active ellipses
                foreach (var punchingNote in activePunchingNotes.ToList())
                {
                    try
                    {
                        if (punchingNote.punchNoteNumber + Definitions.PunchKeyOffset < beatPosition)
                        {
                            foreach (var punchingEllipse in punchingNote.punchingEllipses.ToList())
                            {
                                punchingEllipse.ellipse.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                                if (punchingEllipse.ellipseStatus == PunchingEllipse.EllipseFlags.rendered)
                                {
                                    punchingEllipse.ellipseStatus = PunchingEllipse.EllipseFlags.miss;
                                    afterPunchHistory.Add(punchingEllipse);
                                }
                            }
                            activePunchingNotes.Remove(punchingNote);
                        }
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("fail clearing old active punch note >" + exc.Message);
                    }
                }

                //evaluasi apakah terjadi scoring punching di setiap ellipse (di setiap punching note yang aktif)
                double[] predicatePositionLimit = new double[predicateRatioLimit.Length];
                for (int i = 0; i < predicatePositionLimit.Length; i++)
                {
                    try
                    {
                        predicatePositionLimit[i] = checkedNote - (Definitions.PunchKeyLifeTime - Definitions.PunchKeyOffset) * (1 - predicateRatioLimit[i]);
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("fail converting predicate limit>" + exc.Message);
                    }
                }

                foreach (var punchingNote in activePunchingNotes.ToList())
                {
                    foreach (var punchingEllipse in punchingNote.punchingEllipses.ToList())
                    {
                        try
                        {
                            #region set unpunched ellipse (to be updated)
                            if (punchingEllipse.ellipseStatus == PunchingEllipse.EllipseFlags.rendered)
                            {
                                //update timer punching
                                double lerpPercentage = (((double)punchingNote.punchNoteNumber + Definitions.PunchKeyOffset) - beatPosition) / (Definitions.PunchKeyLifeTime);
                                punchingEllipse.timerEllipse.Width = Definitions.PunchKeyDiameter + (int)(lerpPercentage * (double)(Definitions.PunchTimerDiameter - Definitions.PunchKeyDiameter));
                                punchingEllipse.timerEllipse.Height = punchingEllipse.timerEllipse.Width;
                                Canvas.SetLeft(punchingEllipse.timerEllipse, punchingEllipse.ellipseSpawnPoint.X - 0.5 * punchingEllipse.timerEllipse.Width);
                                Canvas.SetTop(punchingEllipse.timerEllipse, punchingEllipse.ellipseSpawnPoint.Y - 0.5 * punchingEllipse.timerEllipse.Height);
                                punchingEllipse.timerEllipse.Visibility = Windows.UI.Xaml.Visibility.Visible;

                                //update line
                                punchingEllipse.helpingLine.X1 = jointPoints[punchingEllipse.punchingJoint].X;
                                punchingEllipse.helpingLine.Y1 = jointPoints[punchingEllipse.punchingJoint].Y;
                                punchingEllipse.helpingLine.X2 = punchingEllipse.ellipseSpawnPoint.X;
                                punchingEllipse.helpingLine.Y2 = punchingEllipse.ellipseSpawnPoint.Y;

                                //update main punching
                                punchingEllipse.ellipse.Visibility = Windows.UI.Xaml.Visibility.Visible;
                            }
                            ////export main and timer ellipse
                            else if (punchingEllipse.ellipseStatus == PunchingEllipse.EllipseFlags.intitialized)
                            {
                                activeEllipses.Add(punchingEllipse.timerEllipse);
                                activeEllipses.Add(punchingEllipse.ellipse);
                                punchingEllipse.ellipseStatus = PunchingEllipse.EllipseFlags.rendered;
                            }
                            #endregion
                        }
                        catch (Exception exc)
                        {
                            Debug.WriteLine("fail updating ellipse > " + exc.Message);
                        }

                        try
                        {
                            #region set punched ellipse
                            //HACK , repair ankle punching position to ground
                            var punchingGround = punchingEllipse.punchingJoint;
                            if (punchingGround == JointType.AnkleRight) punchingGround = JointType.FootRight;
                            if (punchingGround == JointType.AnkleLeft) punchingGround = JointType.FootLeft;

                            if (punchingEllipse.ellipseStatus == PunchingEllipse.EllipseFlags.rendered && misc.TwoPointDistance(jointPoints[punchingGround], punchingEllipse.ellipseSpawnPoint) <= Definitions.PunchEffectiveArea * Definitions.PunchKeyDiameter)
                            {
                                punchingEllipse.ellipse.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                                punchingEllipse.timerEllipse.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                                for (int i = 0; i < predicatePositionLimit.Length; i++)
                                {
                                    if (beatPosition < predicatePositionLimit[i])
                                    {
                                        punchingEllipse.ellipseStatus = (PunchingEllipse.EllipseFlags)(i + 2);
                                        break;
                                    }
                                }
                                afterPunchHistory.Add(punchingEllipse);
                            }
                            #endregion
                        }
                        catch (Exception exc)
                        {
                            Debug.WriteLine("fail setting up punched ellipse > " + exc.Message);
                        }
                    }
                }
            }
            return activeEllipses;
        }

        public GestureType CheckGesture(Dictionary<JointType, Point> jointPoints)
        {
            //tentukan syarat switch player
            if (jointPoints[JointType.HandRight].Y < jointPoints[JointType.Head].Y && jointPoints[JointType.HandLeft].Y > jointPoints[JointType.Head].Y)
            {
                return GestureType.raisehand;
            }
            else if (jointPoints[JointType.HandRight].Y > jointPoints[JointType.Head].Y && jointPoints[JointType.HandLeft].Y < jointPoints[JointType.Head].Y)
            {
                return GestureType.raisehand;
            }

            //tentukan syarat pause
            else if (misc.TwoPointDistance(jointPoints[JointType.HandLeft], jointPoints[JointType.HandRight]) <= Definitions.PauseHandDistanceMinimal && (jointPoints[JointType.HandLeft].Y < jointPoints[JointType.Head].Y && jointPoints[JointType.HandRight].Y < jointPoints[JointType.Head].Y))
            {
                return GestureType.pause;
            }
            return GestureType.none;
        }
    }

    public class PunchingEllipse
    {
        public Point ellipseSpawnPoint { get; set; }
        public Line helpingLine { get; set; }
        public Ellipse ellipse { get; set; }
        public Ellipse timerEllipse { get; set; }
        public enum EllipseFlags { intitialized, rendered, miss, bad, cool, great, perfect }
        public EllipseFlags ellipseStatus { get; set; }
        public JointType punchingJoint { get; set; }
        public PunchingEllipse()
        {
            ellipseStatus = EllipseFlags.intitialized;
            ellipse = new Ellipse();
            timerEllipse = new Ellipse();
            ellipseSpawnPoint = new Point();
            punchingJoint = new JointType();
            helpingLine = new Line();
            
        }
    }

    public class PunchingNote
    {
        public double punchNoteNumber;
        public List<PunchingEllipse> punchingEllipses { get; set; }
        public PunchingNote()
        {
            punchingEllipses = new List<PunchingEllipse>();
            punchNoteNumber = 0;
        }
    }
}
