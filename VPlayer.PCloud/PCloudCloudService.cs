﻿

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PCloud;
using PCloudClient.Domain;
using VCore;
using VPLayer.Domain.Contracts.CloudService.Providers;
using VPlayer.PCloud.JsonResponses;
using FileInfo = PCloudClient.Domain.FileInfo;

namespace VPlayer.PCloud
{
  public class PCloudCloudService : ICloudService
  {
    #region Fields

    private readonly string filePath;
    private readonly string host;
    private readonly bool ssl;

    private LoginInfo credentials;

    #endregion

    #region Constructors

    public PCloudCloudService(string filePath)
    {
      this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
      host = "eapi.pcloud.com";
      ssl = true;
    }

    #endregion

    #region Initilize

    public void Initilize()
    {
      credentials = GetLoginInfo();
    }

    #endregion

    #region GetLoginInfo

    public LoginInfo GetLoginInfo()
    {
      if (File.Exists(filePath))
      {
        var json = File.ReadAllText(filePath);

        return JsonSerializer.Deserialize<LoginInfo>(json);
      }

      return null;
    }

    #endregion

    #region GetAudioLink

    public async Task<string> GetAudioLink(long id)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            return await conn.GetAudioLink(id);
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

    #region GetFileStats

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

    #endregion

    #region GetFilePublicLink

    private async Task<GetFilePublicLinkResponse> GetFilePublicLink(Connection conn, long id)
    {
      try
      {
        var token = conn.authToken;
        var methodName = "getfilepublink";
        var expires = ((DateTimeOffset)new DateTime(2022, 1, 1)).ToUnixTimeSeconds();
        var request = $"https://{host}/{methodName}?auth={token}&fileid={id}&expires={expires}";

        HttpClient client = new HttpClient();
        HttpResponseMessage responseMessage = await client.GetAsync(request);
        string responseBody = await responseMessage.Content.ReadAsStringAsync();

        var link = JsonSerializer.Deserialize<GetFilePublicLinkResponse>(responseBody);

        return link;
      }
      catch (Exception ex)
      {

      }


      return null;
    }

    #endregion


   

    public Task<KeyValuePair<string, string>?> GetFileLink(long id)
    {
      throw new NotImplementedException();
    }

    public async Task<List<KeyValuePair<long,string>>> GetFileLinks(IEnumerable<long> ids)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            var links = new List<KeyValuePair<long, string>>();

            foreach (var id in ids)
            {
              var link = await conn.GetAudioLink(id);

              links.Add(new KeyValuePair<long, string>(id, link));
            }

            return links;
          }
          catch (Exception ex)
          {

          }
          finally
          {
            await Logout(conn);
          }
        }
      }

      return null;
    }

    #region IsUserLoggedIn

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

    #region CreateFile

    public async Task<long?> CreateFile(string name, long parenId)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            var file = await conn.createFile(parenId, name, FileMode.Create, FileAccess.Write);

            if (file.fileId > 0)
            {
              return file.fileId;
            }
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

    #region WriteToFile

    public async Task<bool> WriteToFile(string sourceString, long id)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(sourceString), false);

            var fd = await conn.createFile(id, FileMode.Open, FileAccess.Write);

            await conn.writeFile(fd, ms, ms.Length);
            await conn.closeFile(fd);

            return true;
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

    #region CreateFileAndWrite

    public async Task<bool> CreateFileAndWrite(string name, string sourceString, long folderId)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(sourceString), false);

            var fd = await conn.createFile(folderId, name, FileMode.Create, FileAccess.Write);

            await conn.writeFile(fd, ms, ms.Length);
            await conn.closeFile(fd);

            return true;
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

    #region CreateFolder

    public async Task<FolderInfo> CreateFolder(string name, long? parentId)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            return await conn.createFolder(name, parentId);
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

    #region ReadFile

    public async Task<MemoryStream> ReadFile(long id)
    {
      if (credentials != null)
      {
        using (var conn = await Connection.open(ssl, host))
        {
          try
          {
            await conn.login(credentials.Email, credentials.Password);

            var fd = await conn.createFile(id, FileMode.Open, FileAccess.Read);
            var fileSize = await conn.getFileSize(fd);
            MemoryStream msRead = new MemoryStream();

            await conn.readFile(fd, msRead, fileSize.length);

            return msRead;
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