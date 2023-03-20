using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Logger;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using unirest_net.http;
using VCore;
using VCore.WPF.LRC;
using VCore.WPF.LRC.Domain;
using VPlayer.AudioStorage.InfoDownloader.LRC;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.MiniLyrics
{
  public class SearchResult
  {
    public string Artist { get; set; }
    public string Title { get; set; }
    public string Link { get; set; }
    public int DownloadCount { get; set; }
  }


  public class MiniLyricsLRCProvider : ILrcProvider
  {
    public MiniLyricsLRCProvider()
    {

    }

    private string url = "http://search.crintsoft.com/searchlyrics.htm";

    private string magickeyStr = "Mlv1clt4.0";

    #region FindLRC

    public Task<string> FindLRC(string artist, string title)
    {
      return Task.Run(async  () =>
      {
        var results = await SearchLyrics(artist, title);

        if (results != null)
        {
          SearchResult searchResult = results.Where(x => x.Title.Similarity(title) > 0.65).FirstOrDefault(x => x.Link.Contains(".lrc"));

          if (searchResult != null)
          {
            return await DownloadLyrics(searchResult.Link);
          }
        }

        return null;
      });
    }

    #endregion

    #region DownloadLyrics

    private Task<string> DownloadLyrics(string path)
    {
      return Task.Run(() =>
      {
        WebClient wc = new WebClient();

        using (MemoryStream stream = new MemoryStream(wc.DownloadData(path)))
        {
          using (StreamReader sr = new StreamReader(stream))
          {
            //This allows you to do one Read operation.
            return sr.ReadToEnd();
          }
        }
      });
    }

    #endregion

    #region SearchLyrics

    public async Task<List<SearchResult>> SearchLyrics(string artist, string title)
    {
      HttpClient client = new HttpClient();

      HttpResponseMessage response = new HttpResponseMessage();

      client.DefaultRequestHeaders.Add("User-Agent", "MiniLyrics");
      client.DefaultRequestHeaders.ExpectContinue = true;

      string query = "<?xml version='1.0' encoding='utf-8' standalone='yes' ?><searchV1 client=\"ViewLyricsOpenSearcher\" artist=\"" + artist + "\" title=\"" + title + "\" OnlyMatched=\"1\" />";

      byte[] queryByteArray = Encoding.UTF8.GetBytes(query);

      var bytes = AssembleQuery(queryByteArray);
      var content = new ByteArrayContent(bytes);

      response = await client.PostAsync("http://search.crintsoft.com/searchlyrics.htm", content);
      string responseString = await response.Content.ReadAsStringAsync();

      var xml = DecryptResultXML(responseString);

      var stringsToParse = RemoveControlCharacters(xml).Split('\0').ToList();

      var url = stringsToParse.SingleOrDefault(x => x == "http://search.crintsoft.com/l/");

      if (url != null)
      {
        var indexOfData = stringsToParse.IndexOf(url);

        return ParseResult(stringsToParse, indexOfData + 1);
      }
      else if (xml.Contains("OK"))
      {
        throw new Exception($"NEDALO SA SPARSOVAT ALE ASI JE DOBRY {artist} {title}");
      }

      return null;
    }

    #endregion

    #region ParseResult

    private string donwloadLink = "http://search.crintsoft.com/l/";
    private List<SearchResult> ParseResult(List<string> stringsToParse, int startIndex)
    {
      List<SearchResult> results = new List<SearchResult>();

      string mainArtist = "";
      string mainTitle = "";
      bool isXmlShifted = false;

      for (int i = startIndex; i < stringsToParse.Count; i++)
      {
        if (stringsToParse[i].Contains(".txt") || stringsToParse[i].Contains(".lrc"))
        {
          if (stringsToParse[i + 1] == "artist")
          {
            var newResult = new SearchResult()
            {
              Link = donwloadLink + stringsToParse[i],
              Artist = stringsToParse[i + 2],
              Title = stringsToParse[i + 4]
            };

            results.Add(newResult);

            mainArtist = stringsToParse[i + 2];
            mainTitle = stringsToParse[i + 4];

            var mainDownloads = stringsToParse.FirstOrDefault(x => x == "downloads");

            if (mainDownloads != null)
            {
              var index = stringsToParse.IndexOf(mainDownloads);
              if (int.TryParse(stringsToParse[index + 1], out var downloads))
              {
                newResult.DownloadCount = downloads;
              }
            }

            i += 3;
          }
          else
          {
            if (stringsToParse[i + 2].Contains(".txt") || stringsToParse[i + 2].Contains(".lrc"))
            {
              var newResult = new SearchResult()
              {
                Link = donwloadLink + stringsToParse[i],
                Artist = mainArtist,
                Title = mainTitle
              };

              results.Add(newResult);

              if (int.TryParse(stringsToParse[i + 1], out var downloads))
              {
                newResult.DownloadCount = downloads;
              }
            }
            else
            {
              var newResult = new SearchResult()
              {
                Link = donwloadLink + stringsToParse[i],
                Artist = stringsToParse[i + 1],
                Title = stringsToParse[i + 2]
              };

              var firstResult = results.FirstOrDefault();

              //Shifted xml
              if (firstResult != null && (newResult.Artist != firstResult.Artist || isXmlShifted))
              {
                isXmlShifted = true;

                newResult.Artist = firstResult.Artist;
                newResult.Title = firstResult.Title;

                for (int j = i; j < stringsToParse.Count; j++)
                {
                  if (int.TryParse(stringsToParse[j], out var downloadCount))
                  {
                    newResult.DownloadCount = downloadCount;
                    break;
                  }
                }

              }
              else if (int.TryParse(stringsToParse[i + 3], out var downloads))
              {
                newResult.DownloadCount = downloads;
              }

              results.Add(newResult);
            }
          }
        }
      }

      return results;
    }

    #endregion

    #region AssembleQuery

    public byte[] AssembleQuery(byte[] value)
    {
      var magickey = Encoding.UTF8.GetBytes(magickeyStr);

      // Create the variable POG to be used in a dirt code
      byte[] pog = new byte[value.Length + magickey.Length];


      Array.Copy(value, 0, pog, 0, value.Length);
      Array.Copy(magickey, 0, pog, value.Length, magickey.Length);

      // POG is hashed using MD5
      byte[] pog_md5 = MD5CryptoServiceProvider.Create().ComputeHash(pog);



      int j = 0;
      for (int i = 0; i < value.Length; i++)
      {
        j += value[i];
      }
      int k = (byte)(j / value.Length);

      // Value is encrypted
      for (int m = 0; m < value.Length; m++)
        value[m] = (byte)(k ^ value[m]);

      // Prepare result code
      System.IO.Stream stream = new System.IO.MemoryStream();
      BinaryWriter result = new BinaryWriter(stream);

      // Write Header
      result.Write((byte)2);
      result.Write((byte)k);
      result.Write((byte)4);
      result.Write((byte)0);
      result.Write((byte)0);
      result.Write((byte)0);

      // Write Generated MD5 of POG problaby to be used in a search cache
      result.Write(pog_md5);

      // Write encrypted value
      result.Write(value);

      byte[] m_Bytes = ReadToEnd(stream);

      // Return magic encoded query
      return m_Bytes;
    }

    #endregion

    #region DecryptResultXML

    private string DecryptResultXML(string value)
    {
      // Get Magic key value
      char magickey = value[1];

      // Prepare output
      System.IO.Stream stream = new System.IO.MemoryStream();
      BinaryWriter neomagic = new BinaryWriter(stream);

      // Decrypts only the XML
      for (int i = 22; i < value.Length; i++)
      {
        neomagic.Write((byte)(value[i] ^ magickey));
      }


      // Return value
      byte[] m_Bytes = ReadToEnd(stream);

      return System.Text.Encoding.ASCII.GetString(m_Bytes);
    }

    #endregion

    #region RemoveControlCharacters

    public static string RemoveControlCharacters(string inString)
    {
      if (inString == null) return null;
      StringBuilder newString = new StringBuilder();
      char ch;
      for (int i = 0; i < inString.Length; i++)
      {
        ch = inString[i];
        if (!char.IsControl(ch) || ch == '\0')
        {
          newString.Append(ch);
        }
      }
      return newString.ToString();
    }

    #endregion

    #region ReadToEnd

    public static byte[] ReadToEnd(System.IO.Stream stream)
    {
      long originalPosition = 0;

      if (stream.CanSeek)
      {
        originalPosition = stream.Position;
        stream.Position = 0;
      }

      try
      {
        byte[] readBuffer = new byte[4096];

        int totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
        {
          totalBytesRead += bytesRead;

          if (totalBytesRead == readBuffer.Length)
          {
            int nextByte = stream.ReadByte();
            if (nextByte != -1)
            {
              byte[] temp = new byte[readBuffer.Length * 2];
              Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
              Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
              readBuffer = temp;
              totalBytesRead++;
            }
          }
        }

        byte[] buffer = readBuffer;
        if (readBuffer.Length != totalBytesRead)
        {
          buffer = new byte[totalBytesRead];
          Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
        }
        return buffer;
      }
      finally
      {
        if (stream.CanSeek)
        {
          stream.Position = originalPosition;
        }
      }
    }

    #endregion


    public LRCProviders LRCProvider { get; }
    public string GetFileName(string artistName, string songName)
    {
      throw new NotImplementedException();
    }

    public LRCFile ParseLRCFile(string[] lines)
    {
      throw new NotImplementedException();
    }

    public Task<ILRCFile> TryGetLrcAsync(string songName, string artistName, string albumName)
    {
      throw new NotImplementedException();
    }

    public Task<bool> Update(ILRCFile lRCFile)
    {
      throw new NotImplementedException();
    }
  }
}

