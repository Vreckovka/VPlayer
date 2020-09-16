using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using File = Google.Apis.Drive.v3.Data.File;

namespace VPlayer.AudioStorage.InfoDownloader.LRC
{
  public class GoogleDriveLrcProvider : LrcProvider<GoogleDriveFile>
  {
    public class GoogleMimeTypes
    {
      public const string GoogleDriveFolder = "application/vnd.google-apps.folder";
      public const string GoogleDriveFile = "application/octet-stream";
    }

    public Dictionary<string, GoogleDriveFile> Artists = new Dictionary<string, GoogleDriveFile>();
    public override LRCProviders LRCProvider => LRCProviders.Google;

    #region BaseFolder

    private string baseFolderName = "Lyrics database";

    private Google.Apis.Drive.v3.Data.File baseFolder;
    private Google.Apis.Drive.v3.Data.File BaseFolder
    {
      get
      {
        if (baseFolder == null)
        {
          baseFolder = GetFolder(baseFolderName);
        }

        return baseFolder;
      }
    }

    #endregion

    #region LoadArtists


    private void LoadArtists()
    {
      var artists = GetFilesInFolder(BaseFolder, GoogleMimeTypes.GoogleDriveFolder);

      var notAdded = artists.Where(p => !Artists.Any(p2 => p2.Key == p.Name));

      foreach (var notAddedFolder in notAdded)
      {
        Artists.Add(notAddedFolder.Name, new GoogleDriveFile(notAddedFolder));
      }

    }

    #endregion

    #region TryGetValueFromFolder

    private GoogleDriveFile TryGetValueFromFolder(string fileName, GoogleDriveFile googleDriveFile, string mimeType)
    {
      GoogleDriveFile requestedFile = null;

      if (!googleDriveFile.Subitems.TryGetValue(fileName, out requestedFile))
      {
        googleDriveFile.LoadFiles(mimeType);
      }

      googleDriveFile.Subitems.TryGetValue(fileName, out requestedFile);

      return requestedFile;
    }

    #endregion

    #region GetFile

    private Semaphore batton = new Semaphore(1,1);
    protected override GoogleDriveFile GetFile(string songName, string artistName, string albumName)
    {
      try
      {
        batton.WaitOne();

        GoogleDriveFile artistFolder;

        if (!Artists.TryGetValue(artistName, out artistFolder))
        {
          LoadArtists();
        }

        if (Artists.TryGetValue(artistName, out artistFolder))
        {
          var album = TryGetValueFromFolder(albumName, artistFolder, GoogleDriveLrcProvider.GoogleMimeTypes.GoogleDriveFolder);

          if (album != null)
          {
            var fileName = GetFileName(artistName, songName) + ".lrc";

            var lyricsFile = TryGetValueFromFolder(fileName, album, GoogleDriveLrcProvider.GoogleMimeTypes.GoogleDriveFile);

            return lyricsFile;
          }
        }

        return null;
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        batton.Release();
      }
      
    }



    #endregion

    #region GetLinesLrcFileAsync

    protected override async Task<string[]> GetLinesLrcFileAsync(string songName, string artistName, string albumName)
    {
      var lyricsFile = GetFile(songName, artistName, albumName);

      if (lyricsFile != null)
      {
        using (var stream = await Download(lyricsFile.File))
        {
          if (stream != null)
          {
            using (var reader = new StreamReader(stream))
            {
              var text = reader.ReadToEnd().Replace("\r\n", "\n").Replace("\r", "");

              var lines = text.Split('\n');

              return lines;
            }
          }
        }
      }

      return null;
    }

    #endregion

    #region GetFolder

    private Google.Apis.Drive.v3.Data.File GetFolder(string folderName)
    {
      if (GoogleDriveServiceProvider.DriveService == null)
      {
        GoogleDriveServiceProvider.RegisterService();
      }

      if (GoogleDriveServiceProvider.DriveService != null)
      {
        FilesResource.ListRequest listRequest = GoogleDriveServiceProvider.DriveService.Files.List();

        folderName = folderName.Replace("\'", "\\'");
        var qs = $"name contains '{folderName}' and mimeType='" + GoogleMimeTypes.GoogleDriveFolder + "'";

        listRequest.Q = qs;

        listRequest.PageSize = 10;
        listRequest.Fields = "nextPageToken, files(id, name)";

        // List files.
        IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;


        return files.FirstOrDefault();
      }

      return null;
    }

    #endregion

    #region GetFilesInFolder

    public static IEnumerable<Google.Apis.Drive.v3.Data.File> GetFilesInFolder(Google.Apis.Drive.v3.Data.File folder, string mimeType)
    {
      if (folder != null)
      {
        if (GoogleDriveServiceProvider.DriveService == null)
        {
          GoogleDriveServiceProvider.RegisterService();
        }

        if (GoogleDriveServiceProvider.DriveService != null)
        {
          FilesResource.ListRequest listRequest = GoogleDriveServiceProvider.DriveService.Files.List();

          var qs = $"parents='{folder.Id}' and mimeType='{mimeType}'";

          listRequest.Q = qs;

          listRequest.Fields = "nextPageToken, files(id, name)";

          IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;

          return files;
        }
      }

      return null;
    }

    #endregion

    #region Download

    private async Task<Stream> Download(File file)
    {
      if (GoogleDriveServiceProvider.DriveService == null)
      {
        GoogleDriveServiceProvider.RegisterService();
      }

      if (GoogleDriveServiceProvider.DriveService != null)
      {
        Stream outputstream = new MemoryStream();

        var request = GoogleDriveServiceProvider.DriveService.Files.Get(file.Id);

        await request.DownloadAsync(outputstream);

        outputstream.Position = 0;

        return outputstream;
      }

      return null;
    }

    #endregion
  }
}