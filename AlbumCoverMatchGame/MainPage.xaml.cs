using AlbumCoverMatchGame.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;


namespace AlbumCoverMatchGame
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private ObservableCollection<Song> Songs;
        private ObservableCollection<StorageFile> allSongsFile;

        bool _playingMusic = false;
        int _round = 0;

        public MainPage()
        {
            this.InitializeComponent();

            Songs = new ObservableCollection<Song>();
            allSongsFile = new ObservableCollection<StorageFile>();
        }


        /// <summary>
        /// Once the page is loaded we start by displaying the progress ring
        /// then we setup the music list, and prepare for a new game
        /// once its done we finally stop displaying the progress ring
        /// </summary>      
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            StartupProgressRing.IsActive = true;

            await SetupMusicList();
            await PrepareNewGame();

            StartupProgressRing.IsActive = false;

            StartCooldown();
        }




        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            StartupProgressRing.IsActive = true;
            
            await PrepareNewGame();

            StartupProgressRing.IsActive = false;
        }

        private void SoundGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            _round++;
            StartCooldown();
        }


        private async void CountDown_CompletedAsync(object sender, object e)
        {
            if (!_playingMusic)
            {
                var song = PickSong();

                MyMediaElement.SetSource
                    (await song.SongFile.OpenAsync(FileAccessMode.Read),
                    song.SongFile.ContentType);


                StartCountdown();
            }
            else
            {
                InstructionTextBlock.Text = "Time's up, Lets just see if you could get it right";
                InstructionTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                //StartCooldown();
            }
        }

        private void StartCooldown()
        {
            _playingMusic = false;
            SolidColorBrush brush = new SolidColorBrush(Colors.Blue);
            MyProgressBar.Foreground = brush;
            InstructionTextBlock.Text = String.Format("Get Ready for Round {0}.....",_round + 1);
            InstructionTextBlock.Foreground = brush;
            CountDown.Begin();
        }

        private void StartCountdown()
        {
            _playingMusic = true;
            SolidColorBrush brush = new SolidColorBrush(Colors.Red);
            MyProgressBar.Foreground = brush;
            InstructionTextBlock.Text = "GO! Guess what album the song belongs to!";
            InstructionTextBlock.Foreground = brush;
            CountDown.Begin();
        }


        private async Task SetupMusicList()
        {
            //1. Get access to music library

            //Get the users music library
            StorageFolder folder = KnownFolders.MusicLibrary;

            //Make a observable collection of StorageFile type to make store all the songs
            allSongsFile = new ObservableCollection<StorageFile>();

            //Call the function to get all songs in music library
            await RetrieveFilesAndFolder(folder);
        }


        private async Task PrepareNewGame()
        {
            //Pick 10 random Songs
            var randomSongs = await PickRandomSongs();

            //Update the observable collection to make sure that the list updates in the UI
            await PopulateSongListAsync(randomSongs);
        }


        /// <summary>
        /// Helper method which returns all the files with mp3 extension from the 
        /// parent folder passed and all its subfolders
        /// </summary>
        /// <param name="parent">The parent folder from which we have to look for songs</param>
        private async Task RetrieveFilesAndFolder(StorageFolder parent)
        {
            //For every files in the parent add all mp3 to observable coleection
            foreach (var item in await parent.GetFilesAsync())
            {
                if (item.FileType == ".mp3")
                    allSongsFile.Add(item);
            }

            //for each folder recursively call itself
            foreach (var item in await parent.GetFoldersAsync())
            {
                await RetrieveFilesAndFolder(item);
            }
        }



        /// <summary>
        /// Helper method which picks 10 random songs each from different album from our song list
        /// </summary>
        /// <returns>The 10 songs which are selected at random</returns>
        private async Task<List<StorageFile>> PickRandomSongs()
        {
            Random random = new Random();

            var totalSongsCount = allSongsFile.Count;

            var randomSongs = new List<StorageFile>();

            while (randomSongs.Count < 10)
            {
                //Get a random number from 0 to totalSongsCount
                var randomNumber = random.Next(totalSongsCount);
                //Get song at that position
                var randomSong = allSongsFile[randomNumber];

                //Get the properties of that song
                MusicProperties randomSongMusicProperties =
                    await randomSong.Properties.GetMusicPropertiesAsync();

                bool isDuplicate = false;

                //If no album is found then mark is as unselectable
                if (String.IsNullOrEmpty(randomSongMusicProperties.Album))
                {
                    isDuplicate = true;
                }
                else
                {
                    foreach (var song in randomSongs)
                    {
                        MusicProperties songMusicProperties = await song.Properties.GetMusicPropertiesAsync();

                        //If a already selected song has the same album then the current song is unselectable
                        if (randomSongMusicProperties.Album == songMusicProperties.Album)
                        {
                            isDuplicate = true;
                            break;
                        }   
                    }
                }

                if (!isDuplicate)
                    randomSongs.Add(randomSong);

            }

            return randomSongs;
        }



        /// <summary>
        /// Helper method which updates the UI to show a new list of 10 random songs
        /// </summary>
        /// <param name="files">The list of files which we want to display</param>
        private async Task PopulateSongListAsync(List<StorageFile> files)
        {
            //Clear the list of songs for old selection
            Songs.Clear();

            int id = 0;

            foreach (var file in files)
            {
                MusicProperties songProperties = await file.Properties.GetMusicPropertiesAsync();

                //The next three lines grabs the thumbnail for the selected item
                StorageItemThumbnail currentThumbnail = await file.GetThumbnailAsync(
                    ThumbnailMode.MusicView,
                    200,
                    ThumbnailOptions.UseCurrentScale);

                var albumCover = new BitmapImage();
                albumCover.SetSource(currentThumbnail);

                //Make a song object
                var song = new Song();

                song.Id = id++;
                song.Title = songProperties.Title;
                song.Album = songProperties.Album;
                song.Artist = songProperties.Artist;
                song.AlbumCover = albumCover;
                song.SongFile = file;
                song.Selected = false;

                Songs.Add(song);
            }
        }


        /// <summary>
        /// Returns a random song from the list of songs that haven't been played before
        /// </summary>       
        private Song PickSong()
        {
            Random random = new Random();
            var unusedSongs = Songs.Where(p => p.Used == false);

            var randomNumber = random.Next(unusedSongs.Count());
            var randomSong = unusedSongs.ElementAt(randomNumber);

            randomSong.Selected = true;
            randomSong.Used = true;

            return randomSong;
        }
    }
}
