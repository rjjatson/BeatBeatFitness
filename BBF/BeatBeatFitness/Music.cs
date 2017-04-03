using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.IO;
using System.Xml.Serialization;
using System.Collections.ObjectModel;



namespace BeatBeatFitness
{
    /// <summary>
    /// Managing every load/saving music library
    /// </summary>
    public class MusicStorageManager
    {
        private StorageFolder saveFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        private string musicLibraryFile = "MusicLib.xml";

        public MusicStorageManager() { }

        /// <summary>
        /// save inputted music lirary obj to specified file location
        /// </summary>
        /// <param name="_musicLibrary"></param>
        /// <returns></returns>
        public async Task<bool> SaveMusicLibrary(MusicLibrary _musicLibrary)
        {
            try
            {
                XmlSerializer _serializer = new XmlSerializer(typeof(MusicLibrary));
                StringWriter _stringWriter = new StringWriter();
                _serializer.Serialize(_stringWriter, _musicLibrary);
                string _serializedString = _stringWriter.ToString();
                StorageFile _saveFile = await this.saveFolder.CreateFileAsync(this.musicLibraryFile, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(_saveFile, _serializedString);
                Debug.WriteLine("success saving library > " + _saveFile);
                return true;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("serializtion and saving music library failed >" + exc.InnerException.Message.ToString());
                return false;
            }
        }

        public async Task<bool> UpdateMusicScore(Music _music, Music.MusicMode _mode, int _highScore)
        {
            try
            {
                MusicLibrary _musicLibrary = new MusicLibrary();
                _musicLibrary = await LoadMusicLibrary();

                if (_musicLibrary != null)
                {
                    for (int i = 0; i < _musicLibrary.musicLibrary.Count; i++)
                    {
                        if (_musicLibrary.musicLibrary[i].fileAccessToken == _music.fileAccessToken)
                        {
                            _musicLibrary.musicLibrary[i].musicPunchKey[(int)_mode].punchKeyHighScore = _highScore;
                            break;
                        }
                    }

                    await SaveMusicLibrary(_musicLibrary);
                }
                Debug.WriteLine("success updating score >");
            }
            catch (Exception exc)
            {
                Debug.WriteLine("fail updating score >" + exc.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Load music lib from sepcified storage location and returning its object
        /// </summary>
        /// <returns></returns>
        public async Task<MusicLibrary> LoadMusicLibrary()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MusicLibrary));
                string loadedString = string.Empty;
                try
                {
                    StorageFile saveFile = await this.saveFolder.GetFileAsync(this.musicLibraryFile);
                    loadedString = await FileIO.ReadTextAsync(saveFile);
                    Debug.WriteLine("success loading library > " + saveFile);
                }
                catch (Exception exc)
                {
                    Debug.WriteLine("loading music failed > " + exc.InnerException.Message.ToString());
                }

                if (loadedString != string.Empty)
                {
                    MusicLibrary loadedMusicLibrary = (MusicLibrary)serializer.Deserialize(new StringReader(loadedString));
                    return loadedMusicLibrary;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exct)
            {
                Debug.WriteLine("deserialization music lib fail > " + exct.InnerException.Message.ToString());
            }
            return null;
        }
    }

    /// <summary>
    /// MVVM- MODEL
    /// Containing all music collection
    /// </summary>
    [XmlRoot("musicLibrary")]
    [XmlInclude(typeof(Music))]
    public class MusicLibrary
    {
        [XmlArray("musicList")]
        [XmlArrayItem("musicEntry")]
        public ObservableCollection<Music> musicLibrary { get; set; }

        public MusicLibrary()
        {
            this.musicLibrary = new ObservableCollection<Music>();
        }

        /// <summary>
        /// Adding musiclibrary element
        /// </summary>
        /// <param name="_newMusic"></param>
        public void AddMusic(Music _newMusic)
        {
            this.musicLibrary.Add(_newMusic);
        }
    }

    /// <summary>
    /// MVVM-VIEWMODEL
    /// Base class of music
    /// </summary>
    [XmlType("music")]
    [XmlInclude(typeof(PunchKey))]
    public class Music
    {
        [XmlIgnore]
        public MusicMode selectedMusicMode { get; set; }

        [XmlIgnore]
        public ImageSource musicThumbnail { get; set; }

        [XmlArray("punchKeyArray")]
        [XmlArrayItem("punchKey")]
        public List<PunchKey> musicPunchKey { get; set; }


        public float musicBPM { get; set; }
        public string musicDuration { get; set; }
        public string musicName { get; set; }
        public string musicArtist { get; set; }
        public string fileAccessToken { get; set; }
        private static MediaElement musicControler { get; set; }
        public enum MusicFlags { Unavailable, Available, Loaded, Played, Paused, Stopped };
        public MusicFlags musicStatus { get; set; }
        public enum MusicMode : int { Exercise, EasyChallenge, NormalChallenge, HardChallenge, EasyMultiplayer, NormalMultiplayer, HardMultiplayer, InputMode };

        public Music() { }

        /// <summary>
        /// Setting music class properties (active music should
        /// </summary>
        /// <returns></returns>
        public async Task<Music> ImportNewAsnyc()
        {
            try
            {
                Music _newMusic = new Music();
                //open file
                FileOpenPicker fileOpen = new FileOpenPicker();
                fileOpen.SuggestedStartLocation = PickerLocationId.Downloads;
                fileOpen.FileTypeFilter.Add(".MP3");
                fileOpen.FileTypeFilter.Add(".WAV");
                StorageFile filePicked = await fileOpen.PickSingleFileAsync();
                _newMusic.fileAccessToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(filePicked);


                //extract music property
                Windows.Storage.FileProperties.MusicProperties musicProp = await filePicked.Properties.GetMusicPropertiesAsync();



                //var x = await musicProp.RetrievePropertiesAsync("Beats-per-minute");

                _newMusic.musicName = musicProp.Title;
                _newMusic.musicArtist = musicProp.Artist;
                _newMusic.musicDuration = ConvertDuration((double)musicProp.Duration.TotalSeconds);

                //add beat tag reading
                IDictionary<string, object> musicTags = await musicProp.RetrievePropertiesAsync(new string[] { "System.Music.BeatsPerMinute" });
                string bpmFromTags = musicTags["System.Music.BeatsPerMinute"].ToString();
                float floatBpm=105;
                float.TryParse(bpmFromTags, out floatBpm);
                _newMusic.musicBPM = floatBpm;

                //TagLib.File tagFile = TagLib.File.Create(filePicked.Path);


                //replace with PCG to fill note sequence
                _newMusic.musicPunchKey = new List<PunchKey>();
                foreach (MusicMode mMode in Enum.GetValues(typeof(MusicMode)))
                {
                    if (mMode == MusicMode.Exercise || mMode == MusicMode.EasyChallenge || mMode == MusicMode.NormalChallenge || mMode == MusicMode.HardChallenge)
                    {
                        var a = (int)mMode;
                        PunchKey newPunchKey = new PunchKey();
                        //hanya generate untuk challenge, multiplayer nanti join dengan challenge
                        await newPunchKey.GenerateNoteSequence((int)mMode);
                        _newMusic.musicPunchKey.Add(newPunchKey);
                    }
                }

                //Change flag
                _newMusic.musicStatus = MusicFlags.Available;
                Debug.WriteLine("succes importing > " + _newMusic.musicName);
                return _newMusic;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("importing music fail > " + exc.Message.ToString());
                return null;
            }
        }

        private string ConvertDuration(double totalSecond)
        {
            string stringDuration = "";
            if ((int)(totalSecond / 60) < 10)
            {
                stringDuration += 0;
            }
            stringDuration += ((int)(totalSecond / 60)).ToString();
            stringDuration += " : ";
            if (((int)totalSecond) % 60 < 10)
            {
                stringDuration += 0;
            }
            stringDuration += (((int)totalSecond) % 60).ToString();
            return stringDuration;
        }

        /// <summary>
        /// Load this music to the music controler
        /// </summary>
        /// <returns></returns>
        public async Task<bool> LoadExistingAsync()
        {
            if (musicControler != null)
            {
                try
                {
                    //reopen music
                    StorageFile retrievedMusicFile = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync(this.fileAccessToken);
                    var musicStream = await retrievedMusicFile.OpenAsync(FileAccessMode.Read);
                    musicControler.SetSource(musicStream, retrievedMusicFile.ContentType);
                    musicStatus = MusicFlags.Loaded;

                    //create a thumb
                    var bmpThumb = new BitmapImage();
                    bmpThumb.SetSource(await retrievedMusicFile.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.MusicView, 256, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail));
                    this.musicThumbnail = (ImageSource)bmpThumb;

                    Debug.WriteLine("success loading >" + this.musicName);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("loading active music failed >" + e.Message.ToString());
                    return false;
                }
            }
            else
            {
                Debug.WriteLine("unable to locate music controler >");
                return false;
            }


        }

        #region music control function
        /// <summary>
        /// setting music controller, always call in MAIN THREAD before loading music to controller
        /// </summary>
        /// <param name="_musicControler"></param>
        public void setMusicController(MediaElement _musicControler)
        {
            //assign music controler so that other method can access
            musicControler = _musicControler;
        }

        public void Play()
        {
            try
            {
                musicControler.Play();
                musicStatus = MusicFlags.Played;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message.ToString());
            }
        }

        public void Pause()
        {
            try
            {
                musicControler.Pause();
                musicStatus = MusicFlags.Paused;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message.ToString());
            }
        }

        public void Stop()
        {
            try
            {
                musicControler.Stop();
                musicStatus = MusicFlags.Stopped;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message.ToString());
            }
        }
        #endregion

    }
}
