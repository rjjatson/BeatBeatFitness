using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Navigation;
using WindowsPreview.Kinect;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Kinect.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace BeatBeatFitness
{
    public sealed partial class MainPage : Page
    {
        #region kinect property
        /// <summary>
        /// C kinect yang aktif
        /// </summary>
        public KinectSensor activeKinect = null;

        /// <summary>
        ///  C konverter titik
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// C frame reader untuk badan
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// M lebar frame depth
        /// </summary>
        private float jointSpaceWidth { get; set; }

        /// <summary>
        /// M tinggi frame depth
        /// </summary>
        private float jointSpaceHeight { get; set; }

        /// <summary>
        /// M array body yang terdaftar
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// M informasi dari setiap M bodies
        /// </summary>
        private BodyInfo[] BodyInfos;

        /// <summary>
        /// Warna dari setiap body (kalo ada banyak)
        /// </summary>
        private List<Color> BodyColors;

        /// <summary>
        /// Clipped edges rectangles
        /// </summary>
        //private Rectangle LeftClipEdge;
        //private Rectangle RightClipEdge;
        //private Rectangle TopClipEdge;
        //private Rectangle BottomClipEdge;

        /// <summary>
        /// C canvas utama dari kinect
        /// </summary>
        private Canvas kinectCanvas;

        /// <summary>
        /// canvas layer diatas kinectcanvas
        /// </summary>
        private Canvas gameCanvas;

        /// <summary>
        /// Jumlah bidy yang terdeteksi
        /// </summary>
        private int BodyCount
        {
            set
            {
                if (value == 0)
                {
                    this.BodyInfos = null;
                    return;
                }

                // creates instances of BodyInfo objects for potential number of bodies
                if (this.BodyInfos == null || this.BodyInfos.Length != value)
                {
                    this.BodyInfos = new BodyInfo[value];

                    for (int bodyIndex = 0; bodyIndex < this.bodies.Length; bodyIndex++)
                    {
                        this.BodyInfos[bodyIndex] = new BodyInfo(this.BodyColors[bodyIndex]);
                    }
                }
            }

            get { return this.BodyInfos == null ? 0 : this.BodyInfos.Length; }
        }

        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private readonly uint bytesPerPixel;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap bitmap = null;

        /// <summary>
        /// Intermediate storage for receiving frame data from the sensor
        /// </summary>
        private byte[] colorPixels = null;

        #endregion

        #region game property
        public Music activeMusic { get; set; }
        public enum GameState { Unavaliable, Init, Play, Pause, Input, End, Over }
        public GameState gameState { get; set; }

        private TimeSpan startGameTimeSpan;
        private TimeSpan pausedGameTimeSpan;
        private double runningGameTime;
        private double pausedGameTime;
        private PunchManager punchManager;
        private PunchKeySotrageManager punchKeyStorageManager;
        private MusicStorageManager musicStorageManager;
        private PlayerPrefStorageManager playerPrefStorageManager;
        private int highScore;
        private bool[] bodyTrackingStatus;

        //TODO : add more dict for multi player
        private Dictionary<JointType, Point> jointPointDict;
        private List<JointType> activejointInput;
        private bool isListeningPause = false;

        //Multiplayering properties
        private Player[] activePlayer;
        private int activePlayerIndex;
        private int activeBodyIndex;

        //challenge properties
        private int challengeGauge;
        private int challengeFullGauge;

        private Dictionary<string, JointType> jointTypeStringInput = new Dictionary<string, JointType>()
        {
            {"rightHand", JointType.HandRight},
            {"leftHand", JointType.HandLeft},
            {"rightElbow", JointType.ElbowRight},
            {"leftElbow", JointType.ElbowLeft},
            {"rightKnee", JointType.KneeRight},
            {"leftKnee", JointType.KneeLeft},
            {"rightAnkle", JointType.AnkleRight},
            {"leftAnkle", JointType.AnkleLeft},            
            {"head", JointType.Head},
        };
        private List<CheckBox> checkedBox = new List<CheckBox>();
        private MusicSelect.GameMode selectedGameMode = new MusicSelect.GameMode();

        #endregion

        public MainPage()
        {
            #region game setup
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            #endregion

            #region kinect setup

            //inits
            this.activeKinect = KinectSensor.GetDefault();
            this.coordinateMapper = this.activeKinect.CoordinateMapper;

            //set up spec       
            FrameDescription depthFrameDescription = this.activeKinect.DepthFrameSource.FrameDescription;
            this.jointSpaceHeight = depthFrameDescription.Height;
            this.jointSpaceWidth = depthFrameDescription.Width;

            //setup body
            this.bodies = new Body[this.activeKinect.BodyFrameSource.BodyCount];

            //set up frame reader
            this.bodyFrameReader = this.activeKinect.BodyFrameSource.OpenReader();
            this.bodyFrameReader.FrameArrived += this.FrameArrivedEvent;

            //set up kinect availability 
            this.activeKinect.IsAvailableChanged += this.SensorAvailableChanged;

            this.BodyColors = new List<Color>
            {
               Colors.Red,
                Colors.Orange,
                Colors.Green,
                Colors.Blue,
                Colors.Indigo,
                Colors.Violet
            };

            //SETUP color frame

            // open the reader for the color frames
            this.colorFrameReader = this.activeKinect.ColorFrameSource.OpenReader();

            // wire handler for frame arrival
            this.colorFrameReader.FrameArrived += this.FrameColorArrivedEvent;

            // create the colorFrameDescription from the ColorFrameSource using rgba format
            FrameDescription colorFrameDescription = this.activeKinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);

            // rgba is 4 bytes per pixel
            this.bytesPerPixel = colorFrameDescription.BytesPerPixel;

            // allocate space to put the pixels to be rendered
            this.colorPixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * this.bytesPerPixel];

            // create the bitmap to display
            this.bitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height);

            //set image source
            this.colorImage.Source = this.bitmap;

            //kasih tahu kinect ada berapa body
            this.BodyCount = this.activeKinect.BodyFrameSource.BodyCount;
            this.kinectCanvas = new Canvas();
            this.gameCanvas = new Canvas();
            this.activeKinect.Open();
            this.DataContext = this;
            this.PopulateVisualObjects();
            this.DisplayGrid.Children.Add(this.kinectCanvas);
            this.DisplayGrid.Children.Add(this.gameCanvas);
            #endregion
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            //smart filtering system
            bodyTrackingStatus = new bool[] { false, false, false, false, false, false };

            activeMusic = e.Parameter as Music;
            switch (activeMusic.selectedMusicMode)
            {
                case Music.MusicMode.EasyChallenge:
                case Music.MusicMode.NormalChallenge:
                case Music.MusicMode.HardChallenge:
                    selectedGameMode = MusicSelect.GameMode.Challenge;
                    break;
                case Music.MusicMode.EasyMultiplayer:
                case Music.MusicMode.NormalMultiplayer:
                case Music.MusicMode.HardMultiplayer:
                    selectedGameMode = MusicSelect.GameMode.Multiplayer;
                    break;
                case Music.MusicMode.Exercise:
                    selectedGameMode = MusicSelect.GameMode.Exercise;
                    break;
            }

            //game reset (harus dipanggil sesudah ACTIVE MUSIC di assign)
            GameInit();
        }

        #region kinect operation
        /// <summary>
        /// P check status sensor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SensorAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (!this.activeKinect.IsAvailable)
            {
                RuntimeLogPrint("sensor NOT available");
            }
            else
            {
                RuntimeLogPrint("sensor ready");
            }
        }

        /// <summary>
        /// E frame datang dari kinect
        /// </summary>
        /// <param name="sender">object event itu</param>
        /// <param name="e">parameter si objek</param>
        private void FrameArrivedEvent(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            bool hasTrackedBody = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    RuntimeLogPrint("sensor available > body frame available");
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
                else
                {
                    RuntimeLogPrint("body frame null");
                }
            }
            if (dataReceived)
            {
                this.ResetBody();
                for (int bodyIndex = 0; bodyIndex < this.bodies.Length; bodyIndex++)
                {
                    Body body = this.bodies[bodyIndex];
                    if (bodyTrackingStatus[bodyIndex] != body.IsTracked)
                    {
                        bodyTrackingStatus[bodyIndex] = body.IsTracked;
                        Debug.WriteLine("body index change happended");
                        Debug.WriteLine("body index " + bodyIndex + " is now " + body.IsTracked);
                    }

                    if (body.IsTracked)
                    {
                        activeBodyIndex = bodyIndex;
                        this.UpdateBody(body, bodyIndex);
                        //HACK : selip fungsi game --> jointPoinDict selalu siap , tapi hanya yang aktif yang di proses
                        if (gameState != GameState.Play || bodyIndex == activePlayer[0].playerIndex || bodyIndex == activePlayer[1].playerIndex)
                        {
                            GameLogic();
                        }
                        hasTrackedBody = true;
                    }
                    else
                    {
                        this.ClearBody(bodyIndex);
                    }
                }
                if (!hasTrackedBody)
                {
                    this.ClearClippedEdges();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrameColorArrivedEvent(object sender, ColorFrameArrivedEventArgs e)
        {
            bool colorFrameProcessed = false;

            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    // verify data and write the new color frame data to the Writeable bitmap
                    if ((colorFrameDescription.Width == this.bitmap.PixelWidth) && (colorFrameDescription.Height == this.bitmap.PixelHeight))
                    {
                        if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                        {
                            colorFrame.CopyRawFrameDataToBuffer(this.bitmap.PixelBuffer);
                        }
                        else
                        {
                            colorFrame.CopyConvertedFrameDataToBuffer(this.bitmap.PixelBuffer, ColorImageFormat.Bgra);
                        }

                        colorFrameProcessed = true;
                    }
                }
            }

            // we got a frame, render
            if (colorFrameProcessed)
            {
                this.bitmap.Invalidate();
            }
        }

        /// <summary>
        /// Hapus data status semua bodies di awal
        /// </summary>
        internal void ResetBody()
        {
            if (this.BodyInfos != null)
            {
                foreach (var bodyInfo in this.BodyInfos)
                {
                    bodyInfo.Updated = false;
                }
            }
        }

        /// <summary>
        /// update data status masing2 body di bodies
        /// </summary>
        /// <param name="body">body yang mau diupdate</param>
        /// <param name="bodyIndex">index body dari bodies yang sedang diupdate</param>
        internal void UpdateBody(Body body, int bodyIndex)
        {
            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
            var jointPointsInDepthSpace = new Dictionary<JointType, Point>();
            var bodyInfo = this.BodyInfos[bodyIndex];
            CoordinateMapper coordinateMapper = this.activeKinect.CoordinateMapper;

            JointLogger.Text = string.Empty;

            foreach (var jointType in body.Joints.Keys)
            {
                CameraSpacePoint position = body.Joints[jointType].Position;
                if (position.Z < 0)
                {
                    position.Z = Definitions.InferredZPositionClamp;
                }
                DepthSpacePoint depthSpacePoint = coordinateMapper.MapCameraPointToDepthSpace(position);
                jointPointsInDepthSpace[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);

                //update data joint
                if (bodyIndex == activePlayer[0].playerIndex || bodyIndex == activePlayer[1].playerIndex ||true) this.UpdateJoint(bodyInfo.JointPoints[jointType], joints[jointType], jointPointsInDepthSpace[jointType]);

                //TODO : updateHand

            }
            //HACK joint point, selalu diambil 
            jointPointDict = jointPointsInDepthSpace;

            //DEBUG : show joint angle
            if (punchManager != null)
            {
                var jointAngle = punchManager.JointPointToAngle(jointPointsInDepthSpace);
                JointLogger.Text = "";
                foreach (KeyValuePair<JointType, double> angleKVP in jointAngle)
                {
                    JointLogger.Text += angleKVP.Key + " : " + angleKVP.Value * (180.0 / Math.PI) + "\n";
                }
            }

        }

        /// <summary>
        /// hapus body dari kanvas
        /// </summary>
        /// <param name="bodyIndex">body yang mau dihapus</param>
        private void ClearBody(int bodyIndex)
        {
            var bodyInfo = this.BodyInfos[bodyIndex];

            //clearing joints
            foreach (var joint in bodyInfo.JointPoints)
            {
                joint.Value.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            //TODO : clear hans and bones too !
        }

        /// <summary>
        /// update elipse tangan
        /// </summary>
        /// <param name="ellipse"></param>
        /// <param name="handState"></param>
        /// <param name="trackingConfidence"></param>
        /// <param name="point"></param>
        private void UpdateHand(Ellipse ellipse, HandState handState, TrackingConfidence trackingConfidence, Point point)
        {

        }

        /// <summary>
        /// print sendi ke layar
        /// </summary>
        /// <param name="ellipse"></param>
        /// <param name="joint"></param>
        /// <param name="point"></param>
        private void UpdateJoint(Ellipse ellipse, Joint joint, Point point)
        {
            try
            {
                TrackingState trackingState = joint.TrackingState;
                if (trackingState != TrackingState.NotTracked)
                {
                    if (trackingState == TrackingState.Tracked)
                    {
                        ellipse.Fill = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        ellipse.Fill = new SolidColorBrush(Colors.Red);
                    }
                    Canvas.SetLeft(ellipse, point.X - Definitions.JointThickness / 2);
                    Canvas.SetTop(ellipse, point.Y - Definitions.JointThickness / 2);

                    ellipse.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    ellipse.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("fail updating joint > " + exc.Message.ToString());
            }
        }

        /// <summary>
        /// Update ruas tulang
        /// </summary>
        /// <param name="line"></param>
        /// <param name="startJoint"></param>
        /// <param name="endJoint"></param>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        private void UpdateBone(Line line, Joint startJoint, Joint endJoint, Point startPoint, Point endPoint)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <param name="hasTrackedBody"></param>
        private void UpdateClippedEdges(Body body, bool hasTrackedBody)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        private void ClearClippedEdges()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handState"></param>
        /// <returns></returns>
        private Color HandStateToColor(HandState handState)
        {

            return new Color();
        }

        /// <summary>
        /// 
        /// </summary>
        private void PopulateVisualObjects()
        {
            foreach (var bodyInfo in this.BodyInfos)
            {
                foreach (var joint in bodyInfo.JointPoints)
                {
                    this.kinectCanvas.Children.Add(joint.Value);
                }
            }
        }

        private void RuntimeLogPrint(string message)
        {
            //runtimeLog.Text = message;
        }

        #endregion

        #region main game operation
        /// <summary>
        /// Hacking frame arrived
        /// called everytime frame from kinect arrived, despite there's body or not
        /// </summary>
        private void GameLogic()
        {
            this.GameUpdate();

            if (gameState == GameState.Unavaliable)
            {
                //enter input mode
                if (activeMusic.selectedMusicMode == Music.MusicMode.InputMode)
                {
                    //input punching system
                    InitInput();
                }
                //game start leave in different thread, changing gamestate to play when come back
                else if (punchManager.CheckGesture(jointPointDict) == PunchManager.GestureType.raisehand && (jointPointDict[JointType.Head].X<=Definitions.canvasWidth*0.75 && jointPointDict[JointType.Head].X>=Definitions.canvasWidth*0.25) )
                {
                    if (activePlayer[0].playerIndex == -1)
                    {
                        activePlayer[0].playerIndex = activeBodyIndex;
                        activePlayer[0].headPosition = jointPointDict[JointType.Head];
                        activePlayer[0].playerStatus = Player.PlayerStatus.Tracked;
                    }
                    else if (selectedGameMode == MusicSelect.GameMode.Multiplayer && activeBodyIndex != activePlayer[0].playerIndex)
                    {
                        activePlayer[1].playerIndex = activeBodyIndex;
                        activePlayer[1].headPosition = jointPointDict[JointType.Head];
                        activePlayer[1].playerStatus = Player.PlayerStatus.Tracked;

                        //swap player if the firstplayer is on the left 
                        if (activePlayer[1].headPosition.X > activePlayer[0].headPosition.X)
                        {
                            int temp = activePlayer[0].playerIndex;
                            activePlayer[0].playerIndex = activePlayer[1].playerIndex;
                            activePlayer[1].playerIndex = activePlayer[0].playerIndex;
                        }
                    }
                    else if ((selectedGameMode == MusicSelect.GameMode.Multiplayer && activePlayer[0].playerIndex != -1 && activePlayer[1].playerIndex != -1) || (selectedGameMode != MusicSelect.GameMode.Multiplayer && activePlayer[0].playerIndex != -1))
                    {
                        InitLayer.Visibility = Visibility.Collapsed;
                        gameState = GameState.Init;
                        GameStart();
                    }
                }
            }

            if (gameState == GameState.Play)
            {
                this.GameRender();

                //two step gesture pausing
                if (punchManager.CheckGesture(jointPointDict) == PunchManager.GestureType.pause)
                {
                    isListeningPause = true;
                }
                if (isListeningPause && punchManager.CheckGesture(jointPointDict) != PunchManager.GestureType.pause)
                {
                    GamePause(true);
                    isListeningPause = false;
                }
            }

            if (gameState == GameState.Pause)
            {
                //two step gesture resuming
                if (punchManager.CheckGesture(jointPointDict) == PunchManager.GestureType.pause)
                {
                    isListeningPause = true;
                }
                if (isListeningPause && punchManager.CheckGesture(jointPointDict) != PunchManager.GestureType.pause)
                {
                    GamePause(false);
                    isListeningPause = false;
                }
            }

            if (gameState == GameState.End)
            {
                GameEnd();
            }
        }
        private async void GameInit()
        {
            punchManager = new PunchManager();
            punchKeyStorageManager = new PunchKeySotrageManager();
            musicStorageManager = new MusicStorageManager();
            playerPrefStorageManager = new PlayerPrefStorageManager();
            gameState = GameState.Unavaliable;

            //UI
            InitLayer.Visibility = Visibility.Visible;
            LayerMain.Visibility = Visibility.Visible;
            PauseLayer.Visibility = Visibility.Collapsed;
            EndLayer.Visibility = Visibility.Collapsed;
            InputPanel.Visibility = Visibility.Collapsed;
            ChallengeHud.Visibility = Visibility.Collapsed;
            MultiplayerHud.Visibility = Visibility.Collapsed;
           

            //multiplayer
            activePlayer = new Player[] { new Player(), new Player() };
            
            //loading player 
            Player loadedPlayer = await playerPrefStorageManager.LoadPlayerPref();
            if(selectedGameMode!=MusicSelect.GameMode.Multiplayer && Definitions.usingPlayerPref)
            {
                if (loadedPlayer != null) activePlayer[0] = loadedPlayer;
                activePlayer[0].ActivingPlayerAvatar();
            }
            
            BGMPlayer.Volume = (double)activePlayer[0].volumePref * 0.1;

            activePlayer[0].comboTextBlock = FirstPlayerComboText;
            activePlayer[0].scoreTextBlock = FirstPlayerScoreText;
            activePlayer[0].comboTextBlock.Text = "";
            activePlayer[0].scoreTextBlock.Text = activePlayer[0].currentScore.ToString();


            if (selectedGameMode == MusicSelect.GameMode.Multiplayer)
            {
                activePlayer[1].comboTextBlock = SecondPlayerComboText;
                activePlayer[1].scoreTextBlock = SecondPlayerScoreText;
                activePlayer[1].comboTextBlock.Text = "";
                activePlayer[1].scoreTextBlock.Text = activePlayer[1].currentScore.ToString();
                SecondPlayerHud.Visibility = Visibility.Visible;
                MultiplayerHud.Visibility = Visibility.Visible;
                //MultiplayerHud.DataContext = this;

                //foreach (var avaImage in activePlayer[1].activeAvatar.avaImages)
                //{
                //    gameCanvas.Children.Add(avaImage);
                //}
            }
            else
            {
                SecondPlayerHud.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

            if (selectedGameMode == MusicSelect.GameMode.Challenge)
            {
                ChallengeHud.Visibility = Visibility.Visible;
                challengeFullGauge = 0;
                if (activeMusic.selectedMusicMode == Music.MusicMode.EasyChallenge)
                {
                    challengeFullGauge = Definitions.EasyChallengeFullGauge;
                }
                else if (activeMusic.selectedMusicMode == Music.MusicMode.NormalChallenge)
                {
                    challengeFullGauge = Definitions.NormalChallengeFullGauge;
                }
                else if (activeMusic.selectedMusicMode == Music.MusicMode.HardChallenge)
                {
                    challengeFullGauge = Definitions.HardChallengeFullGauge;
                }

                challengeGauge = challengeFullGauge;
                //ChallengeHud.DataContext = this;
            }

            if (activeMusic.selectedMusicMode == Music.MusicMode.InputMode)
            {
                FirstPlayerHud.Visibility = Visibility.Collapsed;
            }
        }
        private async void GameStart()
        {
            App.ToggleKinectControl(false);

            //timing system
            startGameTimeSpan = DateTime.Now.TimeOfDay;
            runningGameTime = 0;
            pausedGameTime = 0;

            //punching system
            punchManager.activeMusicBPM = activeMusic.musicBPM;
            punchManager.activeNoteLibrary = await punchKeyStorageManager.LoadNoteLibrary();
            int selectedNoteIndex = (int)activeMusic.selectedMusicMode;

            //HACK copy challenge note to multiplayer note
            if (activeMusic.selectedMusicMode == Music.MusicMode.EasyMultiplayer) selectedNoteIndex = (int)Music.MusicMode.EasyChallenge;
            else if (activeMusic.selectedMusicMode == Music.MusicMode.NormalMultiplayer) selectedNoteIndex = (int)Music.MusicMode.NormalChallenge;
            else if (activeMusic.selectedMusicMode == Music.MusicMode.HardMultiplayer) selectedNoteIndex = (int)Music.MusicMode.HardChallenge;
            punchManager.activeNoteSequence = activeMusic.musicPunchKey[selectedNoteIndex].NoteSequence;

            //Music System
            BGMPlayer.MediaEnded += MusicEnded;
            BGMPlayer.IsLooping = false;
            VolumeSlider.Value = (int)(Definitions.musicVolume * 10);
            activeMusic.setMusicController(BGMPlayer);
            await activeMusic.LoadExistingAsync();
            activeMusic.Play();

            //sfx system
            SFXPlayer.Volume = Definitions.sfxVolume;
            SFXPlayer.IsLooping = false;

            //scoring
            if (selectedGameMode != MusicSelect.GameMode.Multiplayer)
            {
                highScore = activeMusic.musicPunchKey[(int)activeMusic.selectedMusicMode].punchKeyHighScore;
            }

            //avatars
            foreach (var avaImage in activePlayer[0].activeAvatar.avaImages)
            {
                gameCanvas.Children.Add(avaImage);
            }

            if (selectedGameMode == MusicSelect.GameMode.Multiplayer)
            {
                foreach (var avaImage in activePlayer[1].activeAvatar.avaImages)
                {
                    gameCanvas.Children.Add(avaImage);
                }
            }


            gameState = GameState.Play;
        }
        private void MusicEnded(object sender, RoutedEventArgs e)
        {
            this.gameState = GameState.End;
        }
        private void GameUpdate()
        {
            //update time
            if (gameState == GameState.Play) runningGameTime = Math.Abs((startGameTimeSpan - DateTime.Now.TimeOfDay).TotalMilliseconds) - pausedGameTime;

            //update player active
            if (activeBodyIndex == activePlayer[0].playerIndex)
            {
                activePlayerIndex = 0;
            }
            else if (activeBodyIndex == activePlayer[1].playerIndex)
            {
                activePlayerIndex = 1;
            }
        }
        private void GameRender()
        {
            PunchKeyRender();
            PunchingEventRender();
            AvatarRender();
        }
        private void PunchKeyRender()
        {
            List<Ellipse> activeEllipses = new List<Ellipse>();
            try
            {
                activeEllipses = punchManager.PopulatePunchKey(runningGameTime, jointPointDict, activePlayerIndex);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("fail rendering punch key >" + exc.Message);
            }
            if (activeEllipses != null)
            {
                foreach (var ellipse in activeEllipses)
                {
                    if (ellipse != null) gameCanvas.Children.Add(ellipse);
                }
                activeEllipses.Clear();
            }

            if(punchManager.activeLine!=null)
            {
                foreach(var helpingLine in punchManager.activeLine)
                {
                    if (helpingLine != null) gameCanvas.Children.Add(helpingLine);
                }
                punchManager.activeLine.Clear();
            }
        }
        public void PunchingEventRender()
        {
            //cek apakah ada punch yang terjadi
            if (punchManager.afterPunchHistory.Count > 0)
            {
                foreach (var afterPunch in punchManager.afterPunchHistory)
                {
                    PunchingEllipse.EllipseFlags keyStat = afterPunch.ellipseStatus;
                    //breaking combo
                    if (keyStat == PunchingEllipse.EllipseFlags.miss)
                    {
                        activePlayer[activePlayerIndex].scoreMultiplier = 1;
                        if (activePlayer[activePlayerIndex].currentCombo > activePlayer[activePlayerIndex].maxCombo) activePlayer[activePlayerIndex].maxCombo = activePlayer[activePlayerIndex].currentCombo;
                        activePlayer[activePlayerIndex].comboTextBlock.Text = "";
                        activePlayer[activePlayerIndex].currentCombo = 0;
                    }
                    else
                    {
                        activePlayer[activePlayerIndex].currentCombo++;
                        if (activePlayer[activePlayerIndex].currentCombo > activePlayer[activePlayerIndex].maxCombo) activePlayer[activePlayerIndex].maxCombo = activePlayer[activePlayerIndex].currentCombo;
                        activePlayer[activePlayerIndex].scoreMultiplier = 1 + (((int)(activePlayer[activePlayerIndex].currentCombo / Definitions.scoreMultiplierInterval)) * Definitions.scoreMultipliarAddition);
                        if (activePlayer[activePlayerIndex].scoreMultiplier > 1)
                        {
                            activePlayer[activePlayerIndex].comboTextBlock.Text = "combo " + activePlayer[activePlayerIndex].currentCombo + "( x" + activePlayer[activePlayerIndex].scoreMultiplier + " )";
                        }
                        else
                        {
                            activePlayer[activePlayerIndex].comboTextBlock.Text = "combo " + activePlayer[activePlayerIndex].currentCombo;
                        }
                    }

                    //adding number history by key stat
                    activePlayer[activePlayerIndex].predicateHistory[((int)keyStat) - 2]++;

                    int acquiredScore = 0;
                    switch (keyStat)
                    {
                        case PunchingEllipse.EllipseFlags.miss:
                            acquiredScore += Definitions.ScoreMiss;
                            break;
                        case PunchingEllipse.EllipseFlags.bad:
                            acquiredScore += Definitions.ScoreBad;
                            break;
                        case PunchingEllipse.EllipseFlags.cool:
                            acquiredScore += Definitions.ScoreCool;
                            break;
                        case PunchingEllipse.EllipseFlags.great:
                            acquiredScore += Definitions.ScoreGreat;
                            break;
                        case PunchingEllipse.EllipseFlags.perfect:
                            acquiredScore += Definitions.ScorePerfect;
                            break;
                    }
                    activePlayer[activePlayerIndex].currentScore += (int)(acquiredScore * activePlayer[activePlayerIndex].scoreMultiplier);
                    activePlayer[activePlayerIndex].scoreTextBlock.Text = activePlayer[activePlayerIndex].currentScore.ToString();
                    PunchEffect(afterPunch);
                    gameCanvas.Children.Remove(afterPunch.timerEllipse);
                    gameCanvas.Children.Remove(afterPunch.ellipse);
                    gameCanvas.Children.Remove(afterPunch.helpingLine);


                    //HACK : update multiplayer percentage
                    if (selectedGameMode == MusicSelect.GameMode.Multiplayer && activePlayer[1].currentScore != 0)
                    {
                        MultiPlayerSlider.Value = (int)(100 * ((double)activePlayer[1].currentScore / ((double)activePlayer[1].currentScore + (double)activePlayer[0].currentScore)));
                    }

                    //HACK : udpdate challenge percentage
                    if (selectedGameMode == MusicSelect.GameMode.Challenge)
                    {
                        challengeGauge -= Definitions.ScoreGreat;
                        challengeGauge += acquiredScore;
                        if (challengeGauge > challengeFullGauge) challengeGauge = challengeFullGauge;
                        int gaugePercentage = (int)(100 * ((double)challengeGauge / (double)challengeFullGauge));
                        if (gaugePercentage == 0)
                        {
                            Debug.WriteLine("challenge gauge error > " + gaugePercentage);
                        }
                        ChallengeSlider.Value = gaugePercentage;
                        Debug.WriteLine("challenge gauge > " + gaugePercentage);

                        if (challengeGauge <= 0) gameState = GameState.End;
                    }
                }
                punchManager.afterPunchHistory.Clear();
            }
        }
        private async void PunchEffect(PunchingEllipse punchingEllipse)
        {
            //do some cool effect stuff
            try
            {
                
                //print predicate
                BitmapImage effectSource = new BitmapImage();

                string imagePath = "";
                string sfxPath = "";
                switch (punchingEllipse.ellipseStatus)
                {
                    case PunchingEllipse.EllipseFlags.miss:
                        imagePath = "ms-appx:///Assets/Effect/missEffect.png";
                        sfxPath = "ms-appx:///Assets/SFX/missSFX.wav";
                        break;
                    case PunchingEllipse.EllipseFlags.bad:
                         imagePath ="ms-appx:///Assets/Effect/badEffect.png";
                         sfxPath = "ms-appx:///Assets/SFX/badSFX.wav";
                        break;
                    case PunchingEllipse.EllipseFlags.cool:
                         imagePath ="ms-appx:///Assets/Effect/coolEffect.png";
                         sfxPath = "ms-appx:///Assets/SFX/coolSFX.wav";
                        break;
                    case PunchingEllipse.EllipseFlags.great:
                         imagePath ="ms-appx:///Assets/Effect/greatEffect.png";
                         sfxPath = "ms-appx:///Assets/SFX/greatSFX.wav";
                        break;
                    case PunchingEllipse.EllipseFlags.perfect:
                         imagePath ="ms-appx:///Assets/Effect/perfectEffect.png";
                         sfxPath = "ms-appx:///Assets/SFX/perfectSFX.wav";
                        break;
                }

                SFXPlayer.Source = new Uri(sfxPath);
                effectSource = new BitmapImage(new Uri(imagePath));

                //apply audio effect
                SFXPlayer.Play();

                //apply visual evect
                Image effectImage = new Image();
                effectImage.Source = effectSource;
                effectImage.Width = Definitions.effectPredicateWidth;
                effectImage.Height = Definitions.effectPredicateHeight;
                Canvas.SetLeft(effectImage, punchingEllipse.ellipseSpawnPoint.X - effectImage.Width / 2);
                Canvas.SetTop(effectImage, punchingEllipse.ellipseSpawnPoint.Y - effectImage.Height / 2);
                gameCanvas.Children.Add(effectImage);
                PopInThemeAnimation predicateEffectAnim = new PopInThemeAnimation();
                await Task.Delay(Definitions.punchEffectDuration);
                gameCanvas.Children.Remove(effectImage);
                effectImage = null;
            }
            catch(Exception exc)
            {
                Debug.WriteLine("rendering effect fail >" + exc.InnerException.Message);
            }            
        }
        private void AvatarRender()
        {

            if (activePlayer[activePlayerIndex].playerStatus == Player.PlayerStatus.Tracked) activePlayer[activePlayerIndex].activeAvatar.UpdateAvatar(jointPointDict);
        }
        private void ManualDispose()
        {
            //clearing canvas
            gameCanvas.Children.Clear();
        }
        /// <summary>
        /// splitting string duration ke double (satuan menit)
        /// </summary>
        /// <param name="sDuration"></param>
        /// <returns></returns>
        private double StringToDoubleDuration(string sDuration)
        {
            string[] splittedDur = sDuration.Split(':');
            double convertedDouble = Convert.ToDouble(splittedDur[0]) + (Convert.ToDouble(splittedDur[1]) / 60);
            return convertedDouble;
        }
        #endregion

        #region pause game operation
        private void GamePause(bool isPaused)
        {
            //starting of pause
            if (isPaused && PauseLayer.Visibility == Visibility.Collapsed)
            {
                pausedGameTimeSpan = DateTime.Now.TimeOfDay;
                App.ToggleKinectControl(true);
                gameState = GameState.Pause;
                activeMusic.Pause();
                PauseLayer.Visibility = Visibility.Visible;
            }
            //ending pause
            else if (!isPaused && PauseLayer.Visibility == Visibility.Visible)
            {
                pausedGameTime += Math.Abs((pausedGameTimeSpan - DateTime.Now.TimeOfDay).TotalMilliseconds);
                App.ToggleKinectControl(false);
                gameState = GameState.Play;
                activeMusic.Play();
                PauseLayer.Visibility = Visibility.Collapsed;
            }
        }
        private void OnKeyboardUp(object sender, RoutedEventArgs e)
        {
            if (PauseLayer.Visibility == Visibility.Collapsed) { GamePause(true); }
            else { GamePause(false); }
        }
        private void VolumeAdjusting(object sender, RangeBaseValueChangedEventArgs e)
        {
            Definitions.musicVolume = ((double)e.NewValue) * 0.1;
            BGMPlayer.Volume = Definitions.musicVolume;
        }
        private void OnClickExit(object sender, RoutedEventArgs e)
        {
            GamePause(false);
            App.ToggleKinectControl(true);
            ManualDispose();
            Frame.Navigate(typeof(MusicSelect), selectedGameMode);
        }
        private void OnClickRestart(object sender, RoutedEventArgs e)
        {
            GamePause(false);
            App.ToggleKinectControl(false);
            ManualDispose();
            this.Frame.Navigate(typeof(MainPage), activeMusic);
        }
        #endregion

        #region end game operation
        private async void GameEnd()
        {
            //hitung kalori
            double gameDiff=0;
            if(activeMusic.selectedMusicMode == Music.MusicMode.EasyChallenge || activeMusic.selectedMusicMode == Music.MusicMode.EasyMultiplayer)
            {
                gameDiff = 3;
            }
            else if(activeMusic.selectedMusicMode == Music.MusicMode.NormalChallenge|| activeMusic.selectedMusicMode == Music.MusicMode.NormalMultiplayer|| activeMusic.selectedMusicMode == Music.MusicMode.Exercise)
            {
                gameDiff = 3.2;
            }
            else if(activeMusic.selectedMusicMode == Music.MusicMode.HardChallenge|| activeMusic.selectedMusicMode == Music.MusicMode.HardMultiplayer)
            {
                gameDiff = 3.5;
            }
            calBurned.Text = "Calories Burned : " + Math.Round(((runningGameTime/60000) * gameDiff),2).ToString();

            //Ekstrak informasi dari class player ke all string
            ScoreExtractor activeContext = new ScoreExtractor(activePlayer[activePlayerIndex]);
            EndLayer.Visibility = Visibility.Visible;
            PauseLayer.Visibility = Visibility.Collapsed;

            //verifying game state
            App.ToggleKinectControl(true);
            activeMusic.Stop();
            ManualDispose();

            //updating game end UI
            MusicDetailText.Text = activeMusic.musicName + " (" + activeMusic.musicBPM + " bpm) " + GameModeSplitter(activeMusic.selectedMusicMode.ToString());

            int predicateIndex = (int)((activePlayer[activePlayerIndex].currentScore / (activeMusic.musicBPM * Definitions.ScoreGreat * StringToDoubleDuration(activeMusic.musicDuration))) * 7);
            if (predicateIndex > 6) predicateIndex = 6;
            if (predicateIndex < 0) predicateIndex = 0;
            activeContext.predicateLetter = ((Definitions.PredicateLetter)predicateIndex).ToString();

            activeContext.isNewHighScore = "Score";
            if (activePlayerIndex == 0)
            {
                SFXPlayer.Source = new Uri("ms-appx:///Assets/SFX/applasueSFX.wav");
                SFXPlayer.Play();

                if (activePlayer[1].playerStatus == Player.PlayerStatus.Ended || selectedGameMode != MusicSelect.GameMode.Multiplayer) gameState = GameState.Over;
                Debug.WriteLine("reporting first player ");

                if (selectedGameMode == MusicSelect.GameMode.Challenge && activePlayer[0].playerStatus != Player.PlayerStatus.Ended)
                {
                    activePlayer[0].coin += (int)(activePlayer[0].currentScore / 100);
                    PlayerPrefUpdate();
                }

                activePlayer[0].playerStatus = Player.PlayerStatus.Ended;
                FirstPlayerScoreReport.Visibility = Visibility.Visible;
                FirstPlayerScoreReport.DataContext = activeContext;
                if ((activePlayer[activePlayerIndex].currentScore > highScore && selectedGameMode == MusicSelect.GameMode.Exercise) || (selectedGameMode == MusicSelect.GameMode.Challenge && challengeGauge >= 0 && activePlayer[activePlayerIndex].currentScore > highScore))
                {
                    await musicStorageManager.UpdateMusicScore(activeMusic, activeMusic.selectedMusicMode, activePlayer[activePlayerIndex].currentScore);
                    activeContext.isNewHighScore = "New High Score";
                }

                
            }

            if (selectedGameMode == MusicSelect.GameMode.Multiplayer && activePlayerIndex == 1)
            {
                if (activePlayer[0].playerStatus == Player.PlayerStatus.Ended) gameState = GameState.Over;
                Debug.WriteLine("reporting second player ");
                activePlayer[1].playerStatus = Player.PlayerStatus.Ended;
                SecondPlayerScoreReport.Visibility = Visibility.Visible;
                SecondPlayerScoreReport.DataContext = activeContext;
            }

            //if (selectedGameMode != MusicSelect.GameMode.Multiplayer && Definitions.usingPlayerPref)
            //{
            //    if (loadedPlayer != null) activePlayer[0] = loadedPlayer;
            //    activePlayer[0].ActivingPlayerAvatar();
            //}
        }

        private async void PlayerPrefUpdate()
        {
            var x = await playerPrefStorageManager.SavePlayerPref(activePlayer[0]);
        }

        private string GameModeSplitter(string s)
        {
            string combinedString = s;
            string[] sGameMode = s.Split('C');
            if (sGameMode.Length != 1)
            {
                combinedString = sGameMode[0] + " C" + sGameMode[1];
            }
            else
            {
                sGameMode = s.Split('M');
                if (sGameMode.Length != 1)
                {
                    combinedString = sGameMode[0] + " M" + sGameMode[1];
                }
            }

            return combinedString;
        }

        private void OnClickFinishCheat(object sender, RoutedEventArgs e)
        {
            gameState = GameState.End;
        }
        #endregion

        #region input note operation
        private async void InitInput()
        {
            activePlayer[0].playerIndex = activeBodyIndex;
            InputPanel.Visibility = Visibility.Visible;
            InitLayer.Visibility = Visibility.Collapsed;
            punchManager.activeNoteLibrary = await punchKeyStorageManager.LoadNoteLibrary();
            //set last Id
            Note.lastId = 0;
            if (punchManager.activeNoteLibrary.noteLibrary.Count > 0) Note.lastId = punchManager.activeNoteLibrary.noteLibrary[punchManager.activeNoteLibrary.noteLibrary.Count - 1].noteId;
            activejointInput = new List<JointType>();
            gameState = GameState.Input;
        }
        private void PunchingJointChecked(object sender, RoutedEventArgs e)
        {
            CheckBox activeJointInputCB = sender as CheckBox;
            activejointInput.Add(jointTypeStringInput[activeJointInputCB.Name]);
            checkedBox.Add(activeJointInputCB);
        }
        private void PunchingJointUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox activeJointDeleteCB = sender as CheckBox;
            activejointInput.Remove(jointTypeStringInput[activeJointDeleteCB.Name]);
            checkedBox.Remove(activeJointDeleteCB);
        }
        private async void OnClickInputNote(object sender, RoutedEventArgs e)
        {
            try
            {
                //collect all clicked data
                punchManager.activeNoteLibrary.InputNewNote(punchManager.JointPointToAngle(jointPointDict), activejointInput);
                await punchKeyStorageManager.SaveNoteLibrary(punchManager.activeNoteLibrary);
                activejointInput = new List<JointType>();
                foreach (CheckBox cb in checkedBox.ToArray())
                {
                    cb.IsChecked = false;
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("fail inputting note > " + exc.Message.ToString());
            }

        }

        private void OnClickFinishInput(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Menu));
        }
        #endregion

        private void OnClickBack(object sender, RoutedEventArgs e)
        {

            Frame.Navigate(typeof(MusicSelect), selectedGameMode);
        }

    }

}
