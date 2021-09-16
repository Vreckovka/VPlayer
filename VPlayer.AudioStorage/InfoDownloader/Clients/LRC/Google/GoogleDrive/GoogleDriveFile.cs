using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Google.Apis.Drive.v3.Data;

namespace VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google
{
  public class GoogleDriveFile
  {
    private readonly File file;
    private readonly IGoogleDriveServiceProvider googleDriveServiceProvider;

    public GoogleDriveFile(File file, IGoogleDriveServiceProvider googleDriveServiceProvider)
    {
      this.file = file ?? throw new ArgumentNullException(nameof(file));
      this.googleDriveServiceProvider = googleDriveServiceProvider ?? throw new ArgumentNullException(nameof(googleDriveServiceProvider));
    }

    public global::Google.Apis.Drive.v3.Data.File File => file;
    public Dictionary<string, GoogleDriveFile> Subitems { get; } = new Dictionary<string, GoogleDriveFile>();

    #region LoadFiles

    private void LoadFiles(string mimeType)
    {
      var albums = googleDriveServiceProvider.GetFilesInFolder(file, mimeType);

      var notAdded = albums.Where(googleItem => !Subitems.Any(cachedItem => cachedItem.Key == GetNormalizedName(googleItem.Name)));

      foreach (var notAddedFolder in notAdded)
      {
        Subitems.Add(GetNormalizedName(notAddedFolder.Name), new GoogleDriveFile(notAddedFolder, googleDriveServiceProvider));
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
          requestedFile = Subitems.FirstOrDefault(x => x.Key.Contains(normalizedName)).Value;

          if (requestedFile == null)
            requestedFile = Subitems.FirstOrDefault(x => normalizedName.Contains(x.Key)).Value;
        }
      }

      return requestedFile;
    }

    #endregion

    #region GetNormalizedName

    public static string GetNormalizedName(string input)
    {
      if (string.IsNullOrEmpty(input))
      {
        return null;
      }

      Regex rgx = new Regex("[^a-zA-Z0-9]");

      return rgx.Replace(input.ToLower(), "");
    }

    #endregion

  }
}