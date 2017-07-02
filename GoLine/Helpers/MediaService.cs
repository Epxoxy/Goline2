using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace GoLine
{
    public enum PlayMode
    {
        Loop,
        Shuffle,
        Once
    }

    public class MediaService
    {
        public PlayMode Mode { get; set; } = PlayMode.Loop;
        private MediaPlayer player;
        private MediaPlayer Player
        {
            get
            {
                if (player == null) player = new MediaPlayer();
                return player;
            }
            set { player = value; }
        }
        private List<Uri> songList;
        public string CurrentSong
        {
            get
            {
                if (songList != null && songList.Count > 0) return songList[playIndex].ToString();
                return string.Empty;
            }
            set { }
        }
        private int playIndex = 0;
        private Random random;
        private bool isPlaying;
        public bool IsPlaying => isPlaying;
        private bool isPlayCompleted;
        public bool IsPlayCompleted
        {
            get { return isPlayCompleted; }
            set
            {
                isPlayCompleted = value;
                if (value && Mode != PlayMode.Once) PlayNext();
            }
        }
        private bool enablePlay = true;
        public bool EnablePlay
        {
            get { return enablePlay; }
            set
            {
                if(enablePlay != value)
                {
                    enablePlay = value;
                    if (enablePlay) Play();
                    else Stop();
                }
            }
        }
        
        //Set volume between 0 to 100
        private double volume = 50d;
        public double Volume
        {
            get
            {
                return volume;
            }
            set
            {
                if (volume != value)
                {
                    volume = value;
                    SetVolumn(value / 100);
                }
            }
        }
        
        #region Singleton

        private static object lockhelper = new object();
        private static MediaService mediaService;
        public static MediaService GetMediaService()
        {
            lock (lockhelper)
            {
                if (mediaService == null) mediaService = new MediaService();
                return mediaService;
            }
        }
        public static void PutAway()
        {
            mediaService = null;
        }

        #endregion

        private MediaService()
        {
            InitServices();
        }

        private void InitServices()
        {
            random = new Random();
            songList = new List<Uri>();
            EventHandler openedHandler = null, endedHandler = null;
            endedHandler = (obj, args) =>
            {
                isPlaying = false;
                IsPlayCompleted = true;
            };
            openedHandler = (obj, args) =>
            {
                IsPlayCompleted = false;
                isPlaying = true;
            };
            Player.MediaEnded += endedHandler;
            Player.MediaOpened += openedHandler;
        }

        private void SetVolumn(double value)
        {
            Player.Volume = value;
        }

        public void AddSong(Uri uri)
        {
            if (songList == null) songList = new List<Uri>();
            songList.Add(uri);
            //if (!isPlayStarted) Play();
        }
        public void AddDirectory(string directory)
        {
            var dirinfo = new System.IO.DirectoryInfo(directory);
            if (!dirinfo.Exists) return;
            foreach(var file in dirinfo.GetFiles())
            {
                if(file.Extension.ToLower() == ".mp3")
                {
                    songList.Add(new Uri(file.FullName));
                }
            }
        }

        public void RemoveSong(Uri uri)
        {
            if (songList.Contains(uri)) songList.Remove(uri);
        }
        public void PlayNext()
        {
            Stop();
            MoveNextIndex();
            Play();
        }
        public void PlayFront()
        {
            Stop();
            --playIndex;
            if (playIndex < 0) playIndex = songList.Count - 1;
            Play();
        }

        public void Play()
        {
            if (playIndex >= songList.Count || !EnablePlay) return;
            Player.Open(songList[playIndex]);
            Player.Play();
        }
        public void Stop()
        {
            if (isPlaying)
            {
                Player.Stop();
            }
        }
        public void Pause()
        {
            Player.Pause();
        }

        private void MoveNextIndex()
        {
            if (Mode == PlayMode.Shuffle) playIndex = random.Next(0, songList.Count - 1); ;
            if (Mode == PlayMode.Loop)
            {
                ++playIndex;
                if (playIndex >= songList.Count) playIndex = 0;
            }
        }

        #region Not in use

        private void LoopPlay(ref MediaPlayer player, Uri uri)
        {
            var timeLine = new MediaTimeline(uri);
            timeLine.RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever;
            var clock = timeLine.CreateClock();
            player.Clock = clock;
        }

        #endregion


    }
}
