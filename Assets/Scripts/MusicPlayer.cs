using Sanicball;
using Sanicball.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Sanicball.UI;
using Sanicball.Logic;
using System.IO;

namespace Sanicball
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        //public GUISkin skin;

        public MusicPlayerCanvas playerCanvasPrefab;
        public bool playerCanvasLobbyOffset = false;
        private MusicPlayerCanvas playerCanvas;

        public AchievementCanvas achievementPrefab;

        public bool startPlaying = false;
        public bool fadeIn = false;

        public Song[] playlist;
        private Song[] originalPlaylist;
        public AudioSource fastSource;

        [System.NonSerialized]
        public bool fastMode = false;

        private int currentSongID;
        private bool isPlaying;
        private string currentSongCredits;

        //Song credits
        private float timer = 0;

        private float slidePosition;
        private float slidePositionMax = 20;

        public bool changeSong = false;

        private AudioSource aSource;
        private Sanicball.Logic.RaceManager raceManager;

        //private string path;

        public void Play()
        {
            Play(playlist[currentSongID].name);
        }

        public void Next()
        {
            changeSong = true;
        }

        public void Play(string credits)
        {
            if (!ActiveData.GameSettings.music) return;
            if(credits.Length > 0) playerCanvas.Show(credits);
            isPlaying = true;
            aSource.Play();
        }

        public void Pause()
        {
            aSource.Pause();
            isPlaying = false;
        }

        /*private void Awake() gonna do it on start of game
        {
            path = Path.Join(Application.dataPath, "Music");
            
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path); //add no music warning later

            foreach (string filename in Directory.GetFiles(path))
            {
                AudioClip clip = AudioClip.Create(filename);
            }
        }*/

        private void Start()
        {
            playlist = ActiveData.songs.ToArray();
            originalPlaylist = playlist;
            playerCanvas = Instantiate(playerCanvasPrefab);
            Instantiate(achievementPrefab);
            if (playerCanvasLobbyOffset) 
            {
                playerCanvas.lobbyOffset = true;
            }

            aSource = GetComponent<AudioSource>();

            slidePosition = slidePositionMax;
            ShuffleSongs();

            if (ActiveData.ESportsFullyReady)
            {
                Sanicball.Logic.MatchManager m = FindObjectOfType<Sanicball.Logic.MatchManager>();
                if (!m.InLobby) {
                    List<Song> p = playlist.ToList();
                    Song s = new Song();
                    s.name = "Skrollex - Bungee Ride";
                    s.clip = ActiveData.ESportsMusic;
                    p.Insert(0,s);
                    playlist = p.ToArray();
                }
            }


            if (playlist.Length == 0) {
                gameObject.SetActive(false);
                return;
            } else {
                aSource.clip = playlist[0].clip;
                currentSongID = 0;
                isPlaying = aSource.isPlaying;
            }

            if (startPlaying && ActiveData.GameSettings.music)
            {
                Play();
            }
            if (fadeIn)
            {
                aSource.volume = 0f;
            }
            if (!ActiveData.GameSettings.music)
            {
                fastSource.Stop();
            }
            raceManager = FindObjectOfType<Sanicball.Logic.RaceManager>();
        }

        private void Update()
        {
            if (fadeIn && aSource.volume < 0.5f)
            {
                aSource.volume = Mathf.Min(aSource.volume + Time.deltaTime * 0.1f, 0.5f);
            }
            
            //If paused, pause song
            if (raceManager != null){
                if (UI.PauseMenu.GamePaused || raceManager.RaceState == Sanicball.Logic.RaceState.Waiting || raceManager.RaceState == Sanicball.Logic.RaceState.Countdown){
                    Pause();
                }else if(!isPlaying){
                    Play();
                }
            }else{
                if (UI.PauseMenu.GamePaused){
                    Pause();
                }else if(!isPlaying){
                    Play();
                }
            }

            //If it's not playing but supposed to play, change song
            if ((!aSource.isPlaying || GameInput.IsChangingSong() || changeSong) && isPlaying)
            {
                if (currentSongID < playlist.Length - 1)
                {
                    currentSongID++;
                }
                else
                {
                    currentSongID = 0;
                }
                aSource.clip = playlist[currentSongID].clip;
                slidePosition = slidePositionMax;
                changeSong = false;
                Play();
            }

            if (GameInput.ToggleCharacterMusic()) {
                ActiveData.GameSettings.characterMusic = !ActiveData.GameSettings.characterMusic;
                if (ActiveData.GameSettings.characterMusic) {
                    MatchPlayer player = raceManager.matchManager.Players.First(p => p.ClientGuid == raceManager.matchManager.LocalClientGuid);
                    
                    if (SceneManager.GetActiveScene().name == "Lobby") {
                        MatchManager matchManager = FindObjectOfType<MatchManager>();
                        player = matchManager.Players.First(p => p.ClientGuid == matchManager.LocalClientGuid);
                    }

                    if (player != null) player.ChangeMusic();
                } else {
                    playlist = originalPlaylist;
                    Next();
                }
                playerCanvas.Show(ActiveData.GameSettings.characterMusic ? "Character Specific Music - ON" : "Character Specific Music - OFF");
            }

            if (GameInput.ToggleMusic()) {
                ActiveData.GameSettings.music = !ActiveData.GameSettings.music;
                if (ActiveData.GameSettings.music) Play("");
                else Pause();
                playerCanvas.Show(ActiveData.GameSettings.music ? "Music - ON" : "Music - OFF");
            }
            //Timer
            if (timer > 0)
            {
                timer -= Time.deltaTime;
            }

            if (fastMode && fastSource.volume < 1)
            {
                fastSource.volume = Mathf.Min(1, fastSource.volume + Time.deltaTime * 0.25f);
                aSource.volume = 0.5f - fastSource.volume / 2;
            }
            if (!fastMode && fastSource.volume > 0)
            {
                fastSource.volume = Mathf.Max(0, fastSource.volume - Time.deltaTime * 0.5f);
                aSource.volume = 0.5f - fastSource.volume / 2;
            }
            if (timer > 0)
            {
                slidePosition = Mathf.Lerp(slidePosition, 0, Time.deltaTime * 4);
            }
            else
            {
                slidePosition = Mathf.Lerp(slidePosition, slidePositionMax, Time.deltaTime * 2);
            }
        }

        public void ShuffleSongs()
        {
            //Shuffle playlist using Fisher-Yates algorithm
            for (int i = playlist.Length; i > 1; i--)
            {
                int j = Random.Range(0, i);
                Song tmp = playlist[j];
                playlist[j] = playlist[i - 1];
                playlist[i - 1] = tmp;
            }
        }
    }

    [System.Serializable]
    public class Song
    {
        public string name;
        public AudioClip clip;
    }
}