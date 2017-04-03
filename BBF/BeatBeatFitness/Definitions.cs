
using Windows.UI;
namespace BeatBeatFitness
{
    public class Definitions
    {
        #region KinectDefinitions
        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        public const double HighConfidenceHandSize = 40;

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        public const double LowConfidenceHandSize = 20;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        public const double JointThickness = 8.0;

        /// <summary>
        /// Thickness of seen bone lines
        /// </summary>
        public const double TrackedBoneThickness = 4.0;

        /// <summary>
        /// Thickness of inferred joint lines
        /// </summary>
        public const double InferredBoneThickness = 1.0;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        public const double ClipBoundsThickness = 5;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        public const float InferredZPositionClamp = 0.1f;
        #endregion

        public const int AsyncDelayMax = 25000;

        public const double GameFPS = 25;

        public const double PunchKeyLifeTime = 2.0; //lamanya(beat) note bertahan di layar
        public const double PunchKeyOffset = 0.25;

        public const double PauseHandDistanceMinimal = 35;

        public const double PunchTimerStrokeThickness = 3;

        public const double RatioMiss = 0.10;
        public const double RatioBad = 0.25;
        public const double RatioCool = 0.45;
        public const double RatioGreat = 0.80;
        public const double RatioPerfect = 1.00;

        public const int ScoreMiss = 0;
        public const int ScoreBad = 10;
        public const int ScoreCool = 30;
        public const int ScoreGreat = 80;
        public const int ScorePerfect = 100;
        public const int EasyChallengeFullGauge = 3000;
        public const int NormalChallengeFullGauge = 2000;
        public const int HardChallengeFullGauge = 1000;

        public const int PunchKeyDiameter = 25;
        public const int PunchTimerDiameter = 60;
        public const double PunchEffectiveArea = 0.7;

        public const int canvasWidth = 512;
        public const int canvasHeight = 414;

        public const int maxJointNumber = 25;

        public static double musicVolume = 0.3;

        public enum PredicateLetter { F, E, D, C, B, A, S };

        public const int scoreMultiplierInterval = 10;
        public const double scoreMultipliarAddition = 0.25;

        public const double headToHeadAvaRatio = 10.2 / 10.0;
        public const double handToAvaRatio = 7.8 / 10.0;
        public const double footToAvaRatio = 7.4 / 10.0;

        public const double handFlipAngleTreshold = 130;

        public static Color timerColor = Colors.DeepPink;
        public static Color rightPunchColor = Colors.Purple;
        public static Color leftPunchColor = Colors.Purple;
        public static Color headPunchColor = Colors.Purple;
        public static Color lineColor = Colors.Purple;

        public const double effectPredicateWidth = 150;
        public const double effectPredicateHeight = effectPredicateWidth/4;

        public const int punchEffectDuration = 1200;
        public const int effectFPS = 11;

        public const double sfxVolume = 1;

        public const bool usingPlayerPref = true;

        public const int temporaryNoteLenght = 120;
    }
}
