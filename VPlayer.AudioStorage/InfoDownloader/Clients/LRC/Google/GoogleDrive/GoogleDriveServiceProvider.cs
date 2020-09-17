using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Logger;
using VCore.Annotations;
using File = Google.Apis.Drive.v3.Data.File;

namespace VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google
{
  public interface IGoogleDriveServiceProvider
  {
    DriveService DriveService { get; }

    IEnumerable<File> GetFilesInFolder(File folder, string mimeType);
  }

  public class GoogleDriveServiceProvider : IGoogleDriveServiceProvider
  {
    private readonly string keyPath;
    private readonly ILogger logger;

    public GoogleDriveServiceProvider(string keyPath, [NotNull] ILogger logger)
    {
      this.keyPath = keyPath ?? throw new ArgumentNullException(nameof(keyPath));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Properties

    public const string ApplicationName = "VPlayer";

    #region DriveService

    private DriveService driveService;
    public DriveService DriveService
    {
      get
      {
        if (driveService == null)
        {
          driveService = RegisterService();
        }

        return driveService;
      }
    }

    #endregion

    #endregion

    #region Methods


    #region RegisterService

    private DriveService RegisterService(string[] scopes = null)
    {
      lock (this)
      {
        if (DriveService == null)
        {
          try
          {
            if (scopes == null)
            {
              scopes = new[] { DriveService.Scope.DriveReadonly };
            }

            UserCredential credential;

            using (var stream = new FileStream(keyPath, FileMode.Open, FileAccess.Read))
            {
              // The file token.json stores the user's access and refresh tokens, and is created
              // automatically when the authorization flow completes for the first time.
              string credPath = "token.json";
              credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true)).Result;
            }

            // Create Drive API service.
            return new DriveService(new BaseClientService.Initializer()
            {
              HttpClientInitializer = credential,
              ApplicationName = ApplicationName,
            });
          }
          catch (Exception ex)
          {
            logger.Log(ex);
            return null;
          }
        }
        else
          return DriveService;
      }
    }

    #endregion

    #region GetFilesInFolder

    public IEnumerable<File> GetFilesInFolder(File folder, string mimeType)
    {
      if (folder != null)
      {
        if (DriveService != null)
        {
          FilesResource.ListRequest listRequest = DriveService.Files.List();

          var qs = $"parents='{folder.Id}' and mimeType='{mimeType}'";

          listRequest.Q = qs;

          listRequest.Fields = "nextPageToken, files(id, name)";

          IList<File> files = listRequest.Execute().Files;

          return files;
        }
      }

      return null;
    }

    #endregion 

    #endregion
  }
}