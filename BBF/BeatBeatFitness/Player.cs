using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace BeatBeatFitness
{
    public class Player
    {
        public int coin { get; set; }
        public int volumePref { get; set; }
        public string playerName { get; set; }
        public string headAvaIndex { get; set; }
        public string topAvaIndex { get; set; }
        public string bottomAvaIndex { get; set; }

        //important public but XML ignore properties, life is hard
        [XmlIgnore]
        public Avatar activeAvatar;
        [XmlIgnore]
        public Point headPosition { get; set; }
        [XmlIgnore]
        public TextBlock playerTextBlock { get; set; }
        [XmlIgnore]
        public int playerIndex { get; set; }
        [XmlIgnore]
        public int currentCombo { get; set; }
        [XmlIgnore]
        public int maxCombo { get; set; }
        [XmlIgnore]
        public int currentScore { get; set; }
        [XmlIgnore]
        public double scoreMultiplier { get; set; }
        [XmlIgnore]
        public TextBlock comboTextBlock { get; set; }
        [XmlIgnore]
        public TextBlock scoreTextBlock { get; set; }
        /// <summary>
        /// access element using punchkey predicate miss: 2 bad: 3 >>>> perfect: 6
        /// (-2)
        /// </summary>
        [XmlIgnore]
        public int[] predicateHistory { get; set; }
        [XmlIgnore]
        public int lastGeneratedPunchNote { get; set; }

        public enum PlayerStatus { Unavailable, Tracked, Untracked, Ended }
        public PlayerStatus playerStatus { get; set; }

        public Player()
        {
            //TODO load saved parameter here

            playerIndex = -1;
            playerTextBlock = new TextBlock();
            scoreTextBlock = new TextBlock();
            headPosition = new Point(0, 0);
            comboTextBlock = new TextBlock();

            currentCombo = 0;
            maxCombo = 0;
            currentScore = 0;
            scoreMultiplier = 1;
            predicateHistory = new int[5] { 0, 0, 0, 0, 0 };
            lastGeneratedPunchNote = 0;
            playerStatus = PlayerStatus.Unavailable;
            coin = 0;
            playerName = "Beaty Noob";
            headAvaIndex = "000";
            topAvaIndex = "000";
            bottomAvaIndex = "000";
            volumePref = 5;
            ActivingPlayerAvatar();
        }

        public void ActivingPlayerAvatar()
        {
            this.activeAvatar = new Avatar(headAvaIndex, topAvaIndex, bottomAvaIndex);
            if (this.volumePref == 0) volumePref = 5;
        }

    }

    class PlayerPrefStorageManager
    {
        private StorageFolder saveFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        private string playerPrefFile = "PlayerPref.xml";
        public PlayerPrefStorageManager()
        {

        }
        public async Task<bool> SavePlayerPref(Player _player)
        {
            try
            {
                XmlSerializer _serializer = new XmlSerializer(typeof(Player));
                StringWriter _stringWriter = new StringWriter();
                _serializer.Serialize(_stringWriter, _player);
                string _serializedString = _stringWriter.ToString();
                StorageFile _saveFile = await this.saveFolder.CreateFileAsync(this.playerPrefFile, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(_saveFile, _serializedString);
                Debug.WriteLine("success saving player pref > " + playerPrefFile);
                return true;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("serializtion and saving player pref failed >" + exc.InnerException.Message.ToString());
                return false;
            }
        }
        public async Task<Player> LoadPlayerPref()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Player));
                string loadedString = string.Empty;
                try
                {
                    StorageFile saveFile = await this.saveFolder.GetFileAsync(this.playerPrefFile);
                    loadedString = await FileIO.ReadTextAsync(saveFile);
                    Debug.WriteLine("success loading player pref > " + playerPrefFile);
                }
                catch (Exception exc)
                {
                    Debug.WriteLine("loading player pref failed > " + exc.InnerException.Message.ToString());
                }

                if (loadedString != string.Empty)
                {
                    Player loadedPlayer = (Player)serializer.Deserialize(new StringReader(loadedString));
                    return loadedPlayer;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exct)
            {
                Debug.WriteLine("deserialization player pref fail > " + exct.InnerException.Message.ToString());
            }
            return null;
        }
    }

    class ScoreExtractor
    {
        public string maxCombo { get; set; }
        public string missNumber { get; set; }
        public string badNumber { get; set; }
        public string coolNumber { get; set; }
        public string greatNumber { get; set; }
        public string perfectNumber { get; set; }
        public string currentScore { get; set; }
        public string predicateLetter { get; set; }
        public string isNewHighScore { get; set; }
        public string playerName { get; set; }
        public ScoreExtractor(Player player)
        {
            maxCombo = player.maxCombo.ToString();
            missNumber = player.predicateHistory[0].ToString();
            badNumber = player.predicateHistory[1].ToString();
            coolNumber = player.predicateHistory[2].ToString();
            greatNumber = player.predicateHistory[3].ToString();
            perfectNumber = player.predicateHistory[4].ToString();
            currentScore = player.currentScore.ToString();

            //init outside class
            isNewHighScore = "null";
            playerName = "null";
            predicateLetter = "null";
        }
    }
}
