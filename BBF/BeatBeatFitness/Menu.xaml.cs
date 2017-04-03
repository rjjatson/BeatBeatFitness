using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using System.Linq;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BeatBeatFitness
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Menu : Page
    {
        public enum MenuPage { playMenu, helpMenu, settingMenu, creditMenu, shopMenu }
        private Player activePlayer;
        private PlayerPrefStorageManager playerPrefStorageManager;

        public ObservableCollection<AvatarSource> headAvaList { get; set; }
        public ObservableCollection<AvatarSource> topAvaList { get; set; }
        public ObservableCollection<AvatarSource> bottomAvaList { get; set; }
        public string selectedHeadAvaPath { get; set; }
        public string selectedTopAvaPath { get; set; }
        public string selectedBottomAvaPath { get; set; }
        public Menu()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            headAvaList = new ObservableCollection<AvatarSource>();
            topAvaList = new ObservableCollection<AvatarSource>();
            bottomAvaList = new ObservableCollection<AvatarSource>();
            selectedHeadAvaPath = "";
            selectedTopAvaPath = "";
            selectedBottomAvaPath = "";
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //init player pref
            playerPrefStorageManager = new PlayerPrefStorageManager();


            //init ava source TODO : FIND a better way doing this !!!!!!!!!!!!!!!!!!!!!!!!!
            headAvaList.Add(new AvatarSource("None", "None", 0, true));
            headAvaList.Add(new AvatarSource("Head000", "Neko Ears", 3092, true));
            headAvaList.Add(new AvatarSource("Head001", "Neko Ears", 3092, true));
            headAvaList.Add(new AvatarSource("Head002", "Neko Ears", 3092, true));
            headAvaList.Add(new AvatarSource("Head003", "Neko Ears", 3092, true));

            topAvaList.Add(new AvatarSource("None", "None", 0, true));
            topAvaList.Add(new AvatarSource("HandUp000", "Neko Paws", 678, true));
            topAvaList.Add(new AvatarSource("HandUp002", "Neko Paws", 678, true));
            topAvaList.Add(new AvatarSource("HandUp003", "Neko Paws", 678, true));
            topAvaList.Add(new AvatarSource("HandUp004", "Neko Paws", 678, true));

            bottomAvaList.Add(new AvatarSource("None", "None", 0, true));
            bottomAvaList.Add(new AvatarSource("Foot001", "Neko Legs", 678, true));
            bottomAvaList.Add(new AvatarSource("Foot000", "Neko Legs", 678, true));
            bottomAvaList.Add(new AvatarSource("Foot002", "Neko Legs", 678, true));
            bottomAvaList.Add(new AvatarSource("Foot003", "Neko Legs", 678, true));

            HeadAvaListView.DataContext = this;
            TopAvaListView.DataContext = this;
            BottomAvaListView.DataContext = this;

            //init player pref dependencies
            activePlayer = new Player();
            PlayerPrefInit();
            AvaGrid.DataContext = this;

            //init UI
            Window.Current.Content.Visibility = Windows.UI.Xaml.Visibility.Visible;
            BackButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            var destinationMenu = e.Parameter as Nullable<MenuPage>;
            switch (destinationMenu)
            {
                case MenuPage.playMenu:
                    OnClickPlay(null, null);
                    break;
                case MenuPage.helpMenu:
                    OnClickHelp(null, null);
                    break;
                case MenuPage.settingMenu:
                    OnClickSetting(null, null);
                    break;
                case MenuPage.creditMenu:
                    OnClickCredit(null, null);
                    break;
                case MenuPage.shopMenu:
                    OnClickShop(null, null);
                    break;
            }
        }

        #region 0. main menu
        private void OnClickPlay(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Visible;
            PlayMenuPanel.Visibility = Visibility.Visible;
        }
        private void OnClickSetting(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Visible;
            SettingMenuPanel.Visibility = Visibility.Visible;
        }
        private void OnClickHelp(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Visible;
            HelpMenuPanel.Visibility = Visibility.Visible;
        }
        private void OnClickCredit(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Visible;
            CreditMenuPanel.Visibility = Visibility.Visible;
        }
        private void OnClickShop(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Visible;
            ShopMenuPanel.Visibility = Visibility.Visible;
        }
        #endregion

        #region 1. play navigate
        private void OnClickPlayExcercise(object sender, RoutedEventArgs e)
        {
            NavigateToMusicSelect(MusicSelect.GameMode.Exercise);
        }
        private void OnClickPlayChallenge(object sender, RoutedEventArgs e)
        {
            NavigateToMusicSelect(MusicSelect.GameMode.Challenge);
        }
        private void OnClickPlayMultiplayer(object sender, RoutedEventArgs e)
        {
            NavigateToMusicSelect(MusicSelect.GameMode.Multiplayer);
        }
        private void NavigateToMusicSelect(MusicSelect.GameMode gameMode)
        {
            Frame.Navigate(typeof(MusicSelect), gameMode);
        }
        #endregion

        #region 2. Shop menu
        private void OnClickAvaHeadList(object sender, RoutedEventArgs e)
        {
            selectedHeadAvaPath = ((AvatarSource)HeadAvaListView.SelectedItem).imgPath;

            AvaGrid.DataContext = null;
            AvaGrid.DataContext = this;
        }
        private void OnClickAvaTopList(object sender, RoutedEventArgs e)
        {
            selectedTopAvaPath = ((AvatarSource)TopAvaListView.SelectedItem).imgPath;

            AvaGrid.DataContext = null;
            AvaGrid.DataContext = this;
        }
        private void OnClickAvaBottomList(object sender, RoutedEventArgs e)
        {
            selectedBottomAvaPath = ((AvatarSource)BottomAvaListView.SelectedItem).imgPath;

            AvaGrid.DataContext = null;
            AvaGrid.DataContext = this;
        }
        private void OnClickBuy(object sender, RoutedEventArgs e)
        {
            activePlayer.headAvaIndex = ((AvatarSource)HeadAvaListView.SelectedItem).avaIndex;
            activePlayer.topAvaIndex = ((AvatarSource)TopAvaListView.SelectedItem).avaIndex;
            activePlayer.bottomAvaIndex = ((AvatarSource)BottomAvaListView.SelectedItem).avaIndex;

            activePlayer.coin -= (((AvatarSource)HeadAvaListView.SelectedItem).avaCost + ((AvatarSource)TopAvaListView.SelectedItem).avaCost + ((AvatarSource)BottomAvaListView.SelectedItem).avaCost);
            PlayerCoin.DataContext = null;
            PlayerCoin.DataContext = activePlayer;

            ((AvatarSource)HeadAvaListView.SelectedItem).avaCost = 0;
            ((AvatarSource)TopAvaListView.SelectedItem).avaCost = 0;
            ((AvatarSource)BottomAvaListView.SelectedItem).avaCost = 0;

            HeadAvaListView.DataContext = null;
            TopAvaListView.DataContext = null;
            BottomAvaListView.DataContext = null;

            HeadAvaListView.DataContext = this;
            TopAvaListView.DataContext = this;
            BottomAvaListView.DataContext = this;

            PlayerPrefUpdate();
        }
        #endregion

        #region 2. another menu
        #endregion

        #region 2. another menu
        #endregion

        #region 2. another menu
        #endregion
        #region 2. another menu
        #endregion

        #region 99. misc
        private void OnClickBack(object sender, RoutedEventArgs e)
        {
            //colapse all sub menu
            PlayMenuPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            SettingMenuPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            HelpMenuPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            CreditMenuPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            ShopMenuPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            //visible main menu
            MainMenuPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //collapse diri sendiri
            BackButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
        private void OnClickInputMode(object sender, RoutedEventArgs e)
        {
            Music dummyMusic = new Music();
            dummyMusic.selectedMusicMode = Music.MusicMode.InputMode;
            Frame.Navigate(typeof(MainPage), dummyMusic);
        }
        private void VolumeAdjusting(object sender, RangeBaseValueChangedEventArgs e)
        {
            Definitions.musicVolume = ((double)e.NewValue) * 0.1;
            BGMMenu.Volume = Definitions.musicVolume;
            //save volume
            activePlayer.volumePref = (int)e.NewValue;
            PlayerPrefUpdate();
        }

        //list click

        private async void PlayerPrefUpdate()
        {
            var x = await playerPrefStorageManager.SavePlayerPref(activePlayer);
        }
        async void PlayerPrefInit()
        {
            try
            {
                Player loadedPlayer = await playerPrefStorageManager.LoadPlayerPref();
                if (loadedPlayer != null) activePlayer = loadedPlayer;
                BGMMenu.Volume = (double)activePlayer.volumePref * 0.1;
                VolumeSlider.Value = activePlayer.volumePref;
                PlayerCoin.DataContext = activePlayer;

                //select opening list view (currently wore)
                //var selectedQuery = (AvatarSource)(from selectedAva in headAvaList
                //                                   where selectedAva.avaIndex == activePlayer.headAvaIndex
                //                                   select selectedAva).FirstOrDefault();
                HeadAvaListView.SelectedItem = (AvatarSource)(from selectedAva in headAvaList
                                                              where selectedAva.avaIndex == activePlayer.headAvaIndex
                                                              select selectedAva).FirstOrDefault();
                selectedHeadAvaPath = ((AvatarSource)HeadAvaListView.SelectedItem).imgPath;

                TopAvaListView.SelectedItem = (AvatarSource)(from selectedAva in topAvaList
                                                             where selectedAva.avaIndex == activePlayer.topAvaIndex
                                                             select selectedAva).FirstOrDefault();
                selectedTopAvaPath = ((AvatarSource)TopAvaListView.SelectedItem).imgPath;
                

                BottomAvaListView.SelectedItem = (AvatarSource)(from selectedAva in bottomAvaList
                                                                where selectedAva.avaIndex == activePlayer.bottomAvaIndex
                                                                select selectedAva).FirstOrDefault();
                selectedBottomAvaPath = ((AvatarSource)BottomAvaListView.SelectedItem).imgPath;
                
            }
            catch (Exception e)
            {
                Debug.WriteLine("fail opening player pref >" + e.Message);
            }
        }
        #endregion

        

        

    }
    public class AvatarSource
    {
        public string avaIndex { get; set; }
        public string imgPath { get; set; }
        public string avaName { get; set; }
        public int avaCost { get; set; }
        public bool isOwned { get; set; }

        public AvatarSource(string _imgPath, string _avaName, int _avaCost, bool _isOwned)
        {
            avaIndex = "";
            for (int i = 0; i < _imgPath.Length; i++)
            {
                if (char.IsNumber(_imgPath[i])) avaIndex += _imgPath[i];
            }
            imgPath = "Assets/Avatar/" + _imgPath + ".png";
            avaName = _avaName;
            avaCost = _avaCost;
            isOwned = _isOwned;
        }
    }
}
