using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using File = Google.Apis.Drive.v3.Data.File;

namespace VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google
{
  public class GoogleDriveLrcProvider : LrcProvider<GoogleDriveFile>
  {
    public class GoogleMimeTypes
    {
      public const string GoogleDriveFolder = "application/vnd.google-apps.folder";
      public const string GoogleDriveFile = "application/octet-stream";
    }

    public override LRCProviders LRCProvider => LRCProviders.Google;

    #region BaseFolder

    private string baseFolderName = "Lyrics database";

    private GoogleDriveFile baseFolder;
    private GoogleDriveFile BaseFolder
    {
      get
      {
        if (baseFolder == null)
        {
          var fileFolder = GetFolder(baseFolderName);
          baseFolder = new GoogleDriveFile(fileFolder);
        }

        return baseFolder;
      }
    }

    #endregion

    #region GetFile

    private Semaphore batton = new Semaphore(1, 1);
    protected override GoogleDriveFile GetFile(string songName, string artistName, string albumName)
    {
      try
      {
        batton.WaitOne();

        var artistFolder = BaseFolder.TryGetValueFromFolder(artistName, GoogleMimeTypes.GoogleDriveFolder);

        if (artistFolder != null)
        {
          var albumFolder = artistFolder.TryGetValueFromFolder(albumName, GoogleMimeTypes.GoogleDriveFolder);

          if (albumFolder != null)
          {
            var fileName = GetFileName(artistName, songName) + ".lrc";

            var lyricsFile = albumFolder.TryGetValueFromFolder(fileName, GoogleMimeTypes.GoogleDriveFile);

            return lyricsFile;
          }
        }

        return null;
      }
      catch (Exception ex)
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

    private global::Google.Apis.Drive.v3.Data.File GetFolder(string folderName)
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
        IList<global::Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;


        return files.FirstOrDefault();
      }

      return null;
    }

    #endregion

    #region GetFilesInFolder

    public static IEnumerable<global::Google.Apis.Drive.v3.Data.File> GetFilesInFolder(global::Google.Apis.Drive.v3.Data.File folder, string mimeType)
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

          IList<global::Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;

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