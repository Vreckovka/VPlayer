

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PCloud;
using PCloudClient.Domain;
using VCore;
using VPLayer.Domain.Contracts.CloudService.Providers;
using FileInfo = PCloudClient.Domain.FileInfo;

namespace VPlayer.PCloud
{
  public class CloudService : ICloudService
  {
    private readonly string filePath;
    private readonly string host;
    private readonly bool ssl;

    private LoginInfo credentials;

    public CloudService(string filePath)
    {
      this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
      host = "eapi.pcloud.com";
      ssl = true;
    }

    public void Initilize()
    {
      credentials = GetLoginInfo();
    }

    public LoginInfo GetLoginInfo()
    {
      if (File.Exists(filePath))
      {
        var json = File.ReadAllText(filePath);

        return JsonSerializer.Deserialize<LoginInfo>(json);
      }

      return null;
    }

    public async Task<PCloudResponse<Stats>> GetFileStats(long id)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            return await conn.GetFileStats(id);
          }
          finally
          {
            await Logout(conn);
          }
        }
      }

      return null;
    }


    #region GetPublicLink

    public async Task<string> GetPublicLink(long id)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            return await conn.GetFilePublicLink(id);
          }
          finally
          {
            await Logout(conn);
          }
        }
      }

      return null;
    }

  public bool IsUserLoggedIn()
    {
      return File.Exists(filePath);
    }

    #endregion

    #region GetFilesAsync

    public async Task<IEnumerable<FileInfo>> GetFilesAsync(long folderId)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            return await conn.getFiles(folderId);

          }
          finally
          {
            await Logout(conn);
          }
        }
      }

      return null;
    }

    #endregion

    #region GetFoldersAsync

    public async Task<IEnumerable<FolderInfo>> GetFoldersAsync(long folderId)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            return await conn.getFolders(folderId);

          }
          finally
          {
            await Logout(conn);
          }
        }
      }

      return null;
    }

    #endregion

    #region SaveLoginInfo

    public void SaveLoginInfo(string email, string password)
    {
      var loginInfo = new LoginInfo()
      {
        Email = email,
        Password = password
      };

      var json = JsonSerializer.Serialize(loginInfo);


      StringHelper.EnsureDirectoryExists(filePath);

      File.WriteAllText(filePath, json);

      credentials = loginInfo;
    }

    #endregion

    #region GetFoldersAsync

    public async Task<FolderInfo> GetFolderInfo(long id)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            return await conn.listFolder(id);

          }
          finally
          {
            await Logout(conn);
          }
        }
      }

      return null;
    }

    #endregion

    #region ExistsFolderAsync

    public async Task<bool> ExistsFolderAsync(long id)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(true, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            return (await conn.listFolder(id)) != null;

          }
          finally
          {
            await Logout(conn);
          }
        }
      }

      return false;
    }

    #endregion

    #region Logout

    private async Task<bool> Logout(Connection conn)
    {
      if (conn.isDesynced)
        return false;

      await conn.logout();

      return true;
    }

    #endregion
  }
}
