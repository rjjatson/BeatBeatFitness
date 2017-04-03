using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;

namespace BeatBeatFitness
{

    public sealed partial class MusicSelect : Page
    {
        #region property
        public enum GameMode { Exercise, Challenge, Multiplayer };
        public GameMode? selectedGameMode { get; set; }

        private Music activeMusic;
        private MusicLibrary activeMusicLibrary;
        private MusicStorageManager musicStorageManager;
        #endregion

        #region page entry
        public MusicSelect()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        /// <summary>
        /// Menerima objek dari page sebelumnya
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.selectedGameMode = e.Parameter as Nullable<GameMode>;
            MultiplayerSelectLayer.Visibility = Visibility.Collapsed;
            ChallengeSelectLayer.Visibility = Visibility.Collapsed;
            ExcerciseSelectLayer.Visibility = Visibility.Collapsed;
            if (selectedGameMode == GameMode.Exercise) ExcerciseSelectLayer.Visibility = Visibility.Visible;
            else if (selectedGameMode == GameMode.Challenge) ChallengeSelectLayer.Visibility = Visibility.Visible;
            else if (selectedGameMode == GameMode.Multiplayer) MultiplayerSelectLayer.Visibility = Visibility.Visible;

            this.gameModeTitle.Text = selectedGameMode + " Mode";

            InitMusic();
        }
        #endregion

        #region music operation
        /// <summary>
        /// load lib musik
        /// </summary>
        private async void InitMusic()
        {
            this.activeMusic = new Music();
            this.activeMusicLibrary = new MusicLibrary();
            this.musicStorageManager = new MusicStorageManager();
            MusicLibrary _loadedLibrary = await this.musicStorageManager.LoadMusicLibrary();
            if (_loadedLibrary != null)
            {
                this.activeMusicLibrary = _loadedLibrary;
                int librarySize = activeMusicLibrary.musicLibrary.Count;
                if (librarySize != 0)
                {
                    Random musicRandomer = new Random();
                    this.activeMusic = activeMusicLibrary.musicLibrary[musicRandomer.Next(0, librarySize)];
                    this.PlayingActiveMusic(activeMusic);
                    Debug.WriteLine("library is exist, selecting a random music >" + this.activeMusic.musicName);
                }
                else
                {
                    Debug.WriteLine("library is empty >");
                }
                this.MusicListView.DataContext = activeMusicLibrary;
                this.MusicListView.SelectedItem = activeMusic;
            }
            else
            {
                Debug.WriteLine("unable to load library to stage >");
            }
        }

        /// <summary>
        /// hanya memainkan musik ke controler !
        /// </summary>
        private async void PlayingActiveMusic(Music _activeMusic)
        {
            //set dan load music ke controler
            BGMPlayer.IsLooping = true;
            BGMPlayer.Volume = Definitions.musicVolume;
            _activeMusic.setMusicController(this.BGMPlayer);
            await _activeMusic.LoadExistingAsync();
            _activeMusic.Play();

            //update highscore each time change music
            if (selectedGameMode == GameMode.Exercise)
            {
                hiScoreRegular.Text = _activeMusic.musicPunchKey[(int)Music.MusicMode.Exercise].punchKeyHighScore.ToString();
            }
            if(selectedGameMode==GameMode.Challenge)
            {
                hiScoreEasy.Text = _activeMusic.musicPunchKey[(int)Music.MusicMode.EasyChallenge].punchKeyHighScore.ToString();
                hiScoreNormal.Text = _activeMusic.musicPunchKey[(int)Music.MusicMode.NormalChallenge].punchKeyHighScore.ToString();
                hiScoreHard.Text = _activeMusic.musicPunchKey[(int)Music.MusicMode.HardChallenge].punchKeyHighScore.ToString();
            } 

            LeftUI.DataContext = activeMusic;
            //TODO update the left UI
        }

        /// <summary>
        /// adding new music from storage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnClickImportMusic(object sender, RoutedEventArgs e)
        {
            Music _importedMusic = await this.activeMusic.ImportNewAsnyc();
            if (_importedMusic != null)
            {
                foreach (Music _musicElement in this.activeMusicLibrary.musicLibrary)
                {
                    if (_importedMusic.fileAccessToken == _musicElement.fileAccessToken)
                    {
                        this.activeMusic = _musicElement;
                        this.PlayingActiveMusic(this.activeMusic);
                        this.MusicListView.SelectedItem = this.activeMusic;
                        Debug.WriteLine("music is already on the library > ");
                        return;
                    }
                }
                this.activeMusic = _importedMusic;
                this.activeMusicLibrary.AddMusic(this.activeMusic);
                await this.musicStorageManager.SaveMusicLibrary(this.activeMusicLibrary);
                this.PlayingActiveMusic(this.activeMusic);
                this.MusicListView.SelectedItem = this.activeMusic;
            }
        }

        /// <summary>
        /// Delete Song
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnClickDeleteMusic(object sender, RoutedEventArgs e)
        {
            this.activeMusicLibrary.musicLibrary.Remove(this.activeMusic);
            await this.musicStorageManager.SaveMusicLibrary(this.activeMusicLibrary);
            if (activeMusicLibrary.musicLibrary.Count != 0)
            {
                Random _musicRandomer = new Random();
                this.activeMusic = this.activeMusicLibrary.musicLibrary[_musicRandomer.Next(0, this.activeMusicLibrary.musicLibrary.Count)];
                this.MusicListView.SelectedItem = this.activeMusic;
                this.PlayingActiveMusic(this.activeMusic);
            }
            else
            {
                this.activeMusic = null;
            }
        }

        private void OnClickMusicList(object sender, RoutedEventArgs e)
        {
            this.activeMusic = (Music)MusicListView.SelectedItem;
            PlayingActiveMusic(this.activeMusic);
        }
        #endregion

        #region Page Naviagation
        /// <summary>
        /// UI music list is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickRegular(object sender, RoutedEventArgs e)
        {
            activeMusic.selectedMusicMode = Music.MusicMode.Exercise;
            NavigateToMain(activeMusic);
        }
        private void OnClickEasyMusic(object sender, RoutedEventArgs e)
        {
            if (selectedGameMode == GameMode.Challenge)
            {
                activeMusic.selectedMusicMode = Music.MusicMode.EasyChallenge;
            }
            else if (selectedGameMode == GameMode.Multiplayer)
            {
                activeMusic.selectedMusicMode = Music.MusicMode.EasyMultiplayer;
            }
            NavigateToMain(activeMusic);
        }

        private void OnClickNormalMusic(object sender, RoutedEventArgs e)
        {
            if (selectedGameMode == GameMode.Challenge)
            {
                activeMusic.selectedMusicMode = Music.MusicMode.NormalChallenge;
            }
            else if (selectedGameMode == GameMode.Multiplayer)
            {
                activeMusic.selectedMusicMode = Music.MusicMode.NormalMultiplayer;
            }
            NavigateToMain(activeMusic);
        }

        private void OnClickHardMusic(object sender, RoutedEventArgs e)
        {
            if (selectedGameMode == GameMode.Challenge)
            {
                activeMusic.selectedMusicMode = Music.MusicMode.HardChallenge;
            }
            else if (selectedGameMode == GameMode.Multiplayer)
            {
                activeMusic.selectedMusicMode = Music.MusicMode.HardMultiplayer;
            }
            NavigateToMain(activeMusic);
        }

       
        private void NavigateToMain(Music _selectedMusic)
        {
            Frame.Navigate(typeof(MainPage), _selectedMusic);
        }

        private void OnClickHome(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Menu), Menu.MenuPage.playMenu);
        }

        #endregion

        
    }
}
