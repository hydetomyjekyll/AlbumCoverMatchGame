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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AlbumCoverMatchGame
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //1. Get access to music library

            //Get the users music library
            StorageFolder folder = KnownFolders.MusicLibrary;

            //Make a observable collection of StorageFile type to make store all the songs
            var allSongs = new ObservableCollection<StorageFile>();

            //Call the function to get all songs in music library
            await RetrieveFilesAndFolder(allSongs, folder);

            //Pick 10 random Songs
            var randomSongs = await PickRandomSongs(allSongs);



        }

        private async Task RetrieveFilesAndFolder(ObservableCollection<StorageFile> list, StorageFolder parent)
        {
            //For every files in the parent add all mp3 to observable coleection
            foreach (var item in await parent.GetFilesAsync())
            {
                if (item.FileType == ".mp3")
                    list.Add(item);
            }

            //for each folder recursively call itself
            foreach (var item in await parent.GetFoldersAsync())
            {
                await RetrieveFilesAndFolder(list, item);
            }   
        }


        private async Task<List<StorageFile>> PickRandomSongs(ObservableCollection<StorageFile> allSongs)
        {
            Random random = new Random();

            var totalSongsCount = allSongs.Count;

            var randomSongs = new List<StorageFile>();

            while (randomSongs.Count < 10)
            {
                var randomNumber = random.Next(totalSongsCount);
                var randomSong = allSongs[randomNumber];

                MusicProperties randomSongMusicProperties = 
                    await randomSong.Properties.GetMusicPropertiesAsync();

                bool isDuplicate = false;

                if (String.IsNullOrEmpty(randomSongMusicProperties.Album))
                {
                    isDuplicate = true;
                }
                else
                {
                    foreach (var song in randomSongs)
                    {
                        MusicProperties songMusicProperties = await song.Properties.GetMusicPropertiesAsync();

                        if(randomSongMusicProperties.Album == songMusicProperties.Album)
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

    }
}
