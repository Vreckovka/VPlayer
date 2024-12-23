﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using VCore.WPF.LRC;
using VCore.WPF.LRC.Domain;
using File = Google.Apis.Drive.v3.Data.File;

namespace VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google
{
  public class GoogleDriveLrcProvider : LrcProvider<GoogleDriveFile>
  {
    private readonly IGoogleDriveServiceProvider googleDriveServiceProvider;

    public GoogleDriveLrcProvider(IGoogleDriveServiceProvider googleDriveServiceProvider)
    {
      this.googleDriveServiceProvider = googleDriveServiceProvider ?? throw new ArgumentNullException(nameof(googleDriveServiceProvider));
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

          if (fileFolder != null)
            baseFolder = new GoogleDriveFile(fileFolder, googleDriveServiceProvider);
        }

        return baseFolder;
      }
    }

    #endregion

    #region Methods

    #region GetFile

    private Semaphore semaphore = new Semaphore(1, 1);
    protected override Task<GoogleDriveFile> GetFile(string songName, string artistName, string albumName, string extension)
    {
      return Task.Run(() =>
      {
        try
        {
        
          semaphore.WaitOne();

          if (string.IsNullOrEmpty(songName) || string.IsNullOrEmpty(artistName) || string.IsNullOrEmpty(albumName))
          {
            return null;
          }

          var artistFolder = BaseFolder?.TryGetValueFromFolder(artistName, GoogleMimeTypes.GoogleDriveFolder);

          if (artistFolder != null)
          {
            var albumFolder = artistFolder.TryGetValueFromFolder(albumName, GoogleMimeTypes.GoogleDriveFolder);

            if (albumFolder != null)
            {
              var fileName = GetFileName(artistName, songName);

              if (fileName != null)
              {
                fileName = fileName + extension;

                var lyricsFile = albumFolder.TryGetValueFromFolder(fileName, GoogleMimeTypes.GoogleDriveFile);

                return lyricsFile;
              }
            }
          }

          return null;
        }
        finally
        {
          semaphore.Release();
        }
      });
    }



    #endregion

    #region GetLinesLrcFileAsync

    protected override async Task<KeyValuePair<string[], ILRCFile>> GetLinesLrcFileAsync(string songName, string artistName, string albumName)
    {
      var lyricsFile = await GetFile(songName, artistName, albumName, ".lrc");

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

              var lRCFile = new GoogleLRCFile(null)
              {
                GoogleDriveFileId = lyricsFile.File.Id
              };

              return new KeyValuePair<string[], ILRCFile>(lines, lRCFile);
            }
          }
        }
      }

      return new KeyValuePair<string[], ILRCFile>(null, null);
    }

    #endregion

    #region GetFolder

    private File GetFolder(string folderName)
    {
      if (googleDriveServiceProvider.DriveService != null)
      {
        FilesResource.ListRequest listRequest = googleDriveServiceProvider.DriveService.Files.List();

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

    #region Download

    private async Task<Stream> Download(File file)
    {
      if (googleDriveServiceProvider.DriveService != null)
      {
        Stream outputstream = new MemoryStream();

        var request = googleDriveServiceProvider.DriveService.Files.Get(file.Id);

        await request.DownloadAsync(outputstream);

        outputstream.Position = 0;

        return outputstream;
      }

      return null;
    }

    #endregion

    #region Update

    public override Task<bool> Update(ILRCFile lRCFile)
    {
      return Task.Run(() =>
      {
        if (lRCFile is GoogleLRCFile googleLRCFile)
        {
          var file = googleDriveServiceProvider.GetFile(googleLRCFile.GoogleDriveFileId);

          Stream stream = GenerateStreamFromString(lRCFile.GetString());

          googleDriveServiceProvider.UpdateFile(file, file.Id, stream, file.MimeType);

          return true;
        }

        return false;
      });
    }

    #endregion

    #region GenerateStreamFromString

    public static Stream GenerateStreamFromString(string s)
    {
      var stream = new MemoryStream();
      var writer = new StreamWriter(stream);
      writer.Write(s);
      writer.Flush();
      stream.Position = 0;
      return stream;
    }

    #endregion

    #endregion
  }
}