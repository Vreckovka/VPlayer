using System;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google
{
  public class GoogleDriveServiceProvider
  {
    public const string ApplicationName = "VPlayer";

    public static DriveService DriveService { get; private set; }

    private static object batton = new object();
    public static DriveService RegisterService(string[] scopes = null)
    {
      lock (batton)
      {
        if (DriveService == null)
        {
          try
          {
            if (scopes == null)
            {
              scopes = new[] {DriveService.Scope.DriveReadonly};
            }

            UserCredential credential;

            using (var stream = new FileStream("C:\\Users\\Roman Pecho\\Desktop\\vplayer-289518-7ec8818053a1.json", FileMode.Open, FileAccess.Read))
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

              Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
              HttpClientInitializer = credential,
              ApplicationName = ApplicationName,
            });

            DriveService = service;

            return DriveService;
          }
          catch (Exception ex)
          {
            Logger.Logger.Instance.Log(ex);
            return null;
          }
        }
        else
          return DriveService;
      } 
    }
  }
}