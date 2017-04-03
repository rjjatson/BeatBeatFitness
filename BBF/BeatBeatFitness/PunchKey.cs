using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using WindowsPreview.Kinect;
using System.Xml.Serialization;
using System.Diagnostics;
using System.IO;
using Windows.Foundation;

namespace BeatBeatFitness
{
    /// <summary>
    /// cuma urutan di music punchkey untuk manggil note di notelib
    /// </summary>
    [XmlType("punchKey")]
    public class PunchKey
    {
        [XmlArray("noteSequence")]
        [XmlArrayItem("noteItem")]
        public List<int> NoteSequence { get; set; }
        public int punchKeyHighScore { get; set; }
        public int PunchKeyMode { get; set; }
        public PunchKey()
        {

        }
        public async Task<bool> GenerateNoteSequence(int musicMode)
        {

            //load available punching notes
            PunchKeySotrageManager punchStorageManager = new PunchKeySotrageManager();
            NoteLibrary availableNotebrary = await punchStorageManager.LoadNoteLibrary();

            List<int>[] noteArray = new List<int>[4] { new List<int>(), new List<int>(), new List<int>(), new List<int>() }; //indexing -> 0 : 1 key----1: 2 key----2: 3 key----3: 4 key

            foreach (Note note in availableNotebrary.noteLibrary)
            {
                int difficulty = note.serPunchingJoints.Count - 1;
                if (difficulty >= 0 && difficulty < 4)
                {
                    noteArray[difficulty].Add(note.noteId);
                }
                
            }

            //difficulty probability array 
            int[] difficultyProbability;
            PunchKeyMode = musicMode;
            switch (PunchKeyMode)
            {
                case (int)Music.MusicMode.Exercise:
                    difficultyProbability = new int[5] {50, 35, 10, 5, 65 };
                    break;
                case (int)Music.MusicMode.EasyChallenge:
                    difficultyProbability = new int[5] { 65, 35, 5, 0, 60 };
                    break;
                case (int)Music.MusicMode.NormalChallenge:
                    difficultyProbability = new int[5] { 35, 50, 10, 5, 45 };
                    break;
                case (int)Music.MusicMode.HardChallenge:
                    difficultyProbability = new int[5] { 25, 55, 10, 10, 30 };
                    break;
                default:
                    difficultyProbability = new int[5];
                    break;
            }
            //Generate sequence based on probability 
            NoteSequence = new List<int>();
            for (int i = 0; i < Definitions.temporaryNoteLenght; i++)
            {
                //create distribution function
                int rnd = fastRandom(i, 1, 100);
                int cummuativeDistribution = 0;
                for (int j = 0; j < noteArray.Length; j++)
                {
                    cummuativeDistribution += difficultyProbability[j]; //kumulatif cuma dari index 0 sampai 3, 1note, 2note, 3note, 4note
                    if (rnd < cummuativeDistribution)
                    {
                        if (j > 2) NoteSequence.Add(0);
                        NoteSequence.Add( (noteArray[j])[fastRandom(i + j, 0, noteArray[j].Count)]   ); //random dari daftar note sesuai kumulatif
                        break;
                    }
                }

                //create break function
                rnd = fastRandom(i, 1, 100);
                if (rnd < difficultyProbability[4])
                {
                    NoteSequence.Add(0);
                }
            }

            return true;
        }

        private int fastRandom(int realSeed, int minVal, int maxVal)
        {
            Random rndSeed = new Random(realSeed);
            Random rnd = new Random(rndSeed.Next(1, 9874));
            return rnd.Next(minVal, maxVal);
        }
    }


    /// <summary>
    /// isi semua note yang tersedia
    /// </summary>
    [XmlRoot("NoteLibrary")]
    [XmlInclude(typeof(Note))]
    public class NoteLibrary
    {
        [XmlArray("noteLibrary")]
        [XmlArrayItem("noteLibraryItem")]
        public List<Note> noteLibrary { get; set; }

        /// <summary>
        /// Buat library baru
        /// </summary>
        public NoteLibrary()
        {

        }

        /// <summary>
        /// masukin note baru ke lib note activ
        /// </summary>
        /// <param name="_activeObservedJointDict">sudut sudut sendi observed</param>
        /// <param name="_activePunchingJoints">sudut sendi  punchong</param>
        public void InputNewNote(Dictionary<JointType, double> _activeObservedJointDict, List<JointType> _activePunchingJoints)
        {
            Note inputNote = new Note();
            inputNote.ParseDict(_activeObservedJointDict, _activePunchingJoints);
            noteLibrary.Add(inputNote);
        }
    }

    /// <summary>
    /// isi kondisi sudut observable joint dan punching joint yang mau dijadikan acuan
    /// </summary>
    [XmlType("Note")]
    [XmlInclude(typeof(JointType))]
    public class Note
    {
        /// <summary>
        /// besar sudut sendi yang dihitung 
        /// </summary>
        [XmlArray("jointAngleList")]
        [XmlArrayItem("jointAngle")]
        public List<double> serObservedJointAngles { get; set; }

        /// <summary>
        /// sendi yang dibuat acuan pukulan
        /// </summary>
        [XmlArray("activeJointList")]
        [XmlArrayItem("activeJoint")]
        public List<JointType> serPunchingJoints { get; set; }

        public static int lastId { get; set; }
        public int noteId { get; set; }
        //TODO : add more note property for PCG weigh

        public Note()
        {
            this.noteId = Note.lastId + 1;
            Note.lastId++;
        }

        /// <summary>
        /// load serializable prop menjadi dict 
        /// </summary>
        public Dictionary<JointType, double> UnParseDict()
        {
            Dictionary<JointType, double> _activeObservedJointDict = new Dictionary<JointType, double>();
            for (int i = 0; i < PunchManager.observedJoints.Length; i++)
            {
                _activeObservedJointDict.Add(PunchManager.observedJoints[i], serObservedJointAngles[i]);
            }
            return _activeObservedJointDict;
        }

        /// <summary>
        /// isi serializable property dari dict
        /// </summary>
        /// <param name="_activeObservedJointDict">Dictionary , URUTANYA HARUS SAMA dengan NoteLibrary.observedJoints[]</param>
        /// <param name="_activePunchingJoints"></param>
        public void ParseDict(Dictionary<JointType, double> _activeObservedJointDict, List<JointType> _activePunchingJoints)
        {
            serPunchingJoints = _activePunchingJoints;
            serObservedJointAngles = new List<double>();
            foreach (var jointAngle in _activeObservedJointDict)
            {
                serObservedJointAngles.Add(jointAngle.Value);
            }
        }
    }



    /// <summary>
    /// save-load punchkey lib
    /// </summary>
    public class PunchKeySotrageManager
    {
        public async Task<NoteLibrary> LoadNoteLibrary()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NoteLibrary));
                string loadedString = string.Empty;
                try
                {
                    StorageFile saveFile = await this.saveFolder.GetFileAsync(this.musicLibraryFile);
                    loadedString = await FileIO.ReadTextAsync(saveFile);
                    Debug.WriteLine("success loading key punch library > " + saveFile);
                }
                catch (Exception exc)
                {
                    Debug.WriteLine("loading keypunch library failed > " + exc.InnerException.Message.ToString());
                }

                if (loadedString != string.Empty)
                {
                    NoteLibrary loadedNoteLibrary = (NoteLibrary)serializer.Deserialize(new StringReader(loadedString));
                    return loadedNoteLibrary;
                }
                else
                {
                    Debug.WriteLine("keypunch unavailable> ");
                    return null;
                }

            }
            catch (Exception exct)
            {
                Debug.WriteLine("keypunch deserialization fail > " + exct.InnerException.Message.ToString());
            }
            return null;
        }
        private StorageFolder saveFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        private string musicLibraryFile = "KeyPunchLib.xml";

        public async Task<bool> SaveNoteLibrary(NoteLibrary _noteLibrary)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NoteLibrary));
                StringWriter stringWriter = new StringWriter();
                serializer.Serialize(stringWriter, _noteLibrary);
                string serializedString = stringWriter.ToString();
                StorageFile saveFile = await this.saveFolder.CreateFileAsync(this.musicLibraryFile, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(saveFile, serializedString);
                Debug.WriteLine("success saving punch key library > " + saveFile);
                return true;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("serializtion and saving punch key library failed >" + exc.InnerException.Message.ToString());
                return false;
            }
        }
    }
}
