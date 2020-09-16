using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Drive.v3.Data;

namespace VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google
{
  public class GoogleDriveFile
  {
    private readonly File file;

    public GoogleDriveFile(File file)
    {
      this.file = file ?? throw new ArgumentNullException(nameof(file));
    }

    public global::Google.Apis.Drive.v3.Data.File File => file;
    public Dictionary<string, GoogleDriveFile> Subitems { get; } = new Dictionary<string, GoogleDriveFile>();

    #region LoadFiles

    private void LoadFiles(string mimeType)
    {
      var albums = GoogleDriveLrcProvider.GetFilesInFolder(file, mimeType);

      var notAdded = albums.Where(googleItem => !Subitems.Any(cachedItem => cachedItem.Key == GetNormalizedName(googleItem.Name)));

      foreach (var notAddedFolder in notAdded)
      {
        Subitems.Add(GetNormalizedName(notAddedFolder.Name), new GoogleDriveFile(notAddedFolder));
      }
    }

    #endregion

    #region TryGetValueFromFolder

    public GoogleDriveFile TryGetValueFromFolder(string fileName, string mimeType)
    {
      GoogleDriveFile requestedFile = null;

      var normalizedName = GetNormalizedName(fileName);

      if (!Subitems.TryGetValue(normalizedName, out requestedFile))
      {
        LoadFiles(mimeType);

        Subitems.TryGetValue(normalizedName, out requestedFile);

        if (requestedFile == null)
        {
          requestedFile = Subitems.FirstOrDefault(x => x.Key.Contains(GoogleDriveFile.GetNormalizedName(fileName))).Value;

          if (requestedFile == null)
            requestedFile = Subitems.FirstOrDefault(x => GoogleDriveFile.GetNormalizedName(fileName).Contains(x.Key)).Value;
        }
      }

      return requestedFile;
    }

    #endregion

    #region GetNormalizedName

    public static string GetNormalizedName(string input)
    {
      return input.ToLower().Replace(" ", "").Replace("_", "");
    }

    #endregion

  }
}