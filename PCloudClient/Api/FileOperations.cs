using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PCloudClient.Domain;
using FileInfo = PCloudClient.Domain.FileInfo;

namespace PCloud
{
  /// <summary>File operations RPCs</summary>
  public static class FileOperations
  {
    /// <summary>Flags for <see cref="createFileImpl(Connection, object, eFileOpenFlags)" /> RPC.</summary>
    [Flags]
    enum eFileOpenFlags : ushort
    {
      None = 0,
      O_WRITE = 0x0002,
      O_CREAT = 0x0040,
      O_EXCL = 0x0080,
      /// <summary>Truncate files when opening existing files.</summary>
      O_TRUNC = 0x0200,
      /// <summary>Always write to the end of file (unless you use pwrite). That is the only reliable method without race conditions for writing in the end of file when there are multiple writers.</summary>
      O_APPEND = 0x0400
    };

    /// <summary>Translate file open flags from System.IO into pCloud's Linux-friendly variant.</summary>
    static eFileOpenFlags openFlags(FileMode mode, FileAccess access)
    {
      if (access == FileAccess.Read)
      {
        if (mode != FileMode.Open)
          throw new ArgumentException("File mode incompatible with Read access");
        return eFileOpenFlags.None;
      }

      eFileOpenFlags flags = eFileOpenFlags.O_WRITE;
      switch (mode)
      {
        case FileMode.CreateNew:
          flags |= eFileOpenFlags.O_CREAT | eFileOpenFlags.O_EXCL;
          break;
        case FileMode.Create:
          flags |= eFileOpenFlags.O_CREAT | eFileOpenFlags.O_TRUNC;
          break;
        case FileMode.Open:
          break;
        case FileMode.OpenOrCreate:
          flags |= eFileOpenFlags.O_CREAT;
          break;
        case FileMode.Truncate:
          flags |= eFileOpenFlags.O_TRUNC;
          break;
        case FileMode.Append:
          flags |= eFileOpenFlags.O_APPEND;
          break;
      }
      return flags;
    }

    /// <summary>File descriptor and ID.</summary>
    public struct FileDescriptor
    {
      /// <summary>File descriptor, they are small positive numbers, incremented by 1 as you open more files within the session</summary>
      public readonly int fd;
      /// <summary>File ID, very large integer > 4G, persistent across connections and sessions.</summary>
      public readonly long fileId;
      internal FileDescriptor(IReadOnlyDictionary<string, object> dict)
      {
        fd = dict.getInt("fd");
        fileId = dict.getLong("fileid");
      }

      /// <summary>Implicitly convert to int to pass this structure into readFile and friends</summary>
      public static implicit operator int(FileDescriptor f) => f.fd;

      /// <summary>Returns a string that represents the current object.</summary>
      public override string ToString()
      {
        return $"descriptor { fd }, ID { fileId.ToString("x") }";
      }
    }

    /// <summary>Open file, return the descriptor</summary>
    static async Task<FileDescriptor> createFileImpl(this Connection conn, object pathOrId, eFileOpenFlags flags)
    {
      RequestBuilder req = conn.newRequest("file_open");
      req.add("flags", (long)flags);

      if (pathOrId is long id)
        req.add("fileid", id);
      else if (pathOrId is string path)
        req.add("path", path);
      else
        throw new ArgumentException("Session.openFile expects either long id, or string path");

      var response = await conn.send(req);
      return new FileDescriptor(response.dict);
    }

    /// <summary>Open a file specified by absolute path.</summary>
    public static Task<FileDescriptor> createFile(this Connection conn, string path, FileMode mode, FileAccess access)
    {
      eFileOpenFlags flags = openFlags(mode, access);
      return conn.createFileImpl(path, flags);
    }

    /// <summary>Open an existing file specified by file ID</summary>
    public static Task<FileDescriptor> createFile(this Connection conn, long fileId, FileMode mode, FileAccess access)
    {
      eFileOpenFlags flags = openFlags(mode, access);
      return conn.createFileImpl(fileId, flags);
    }

    /// <summary>Open an existing file</summary>
    public static Task<FileDescriptor> createFile(this Connection conn, FileInfo fi, FileMode mode, FileAccess access)
    {
      return conn.createFile(fi.id, mode, access);
    }

    /// <summary>Open a file in the specified folder</summary>
    public static async Task<FileDescriptor> createFile(this Connection conn, FolderInfo parentFolder, string name, FileMode mode, FileAccess access)
    {
      RequestBuilder req = conn.newRequest("file_open");
      req.add("flags", (long)openFlags(mode, access));
      req.add("folderid", parentFolder.id);
      req.add("name", name);
      var response = await conn.send(req);
      return new FileDescriptor(response.dict);
    }

    /// <summary>Close file descriptor</summary>
    public static Task closeFile(this Connection conn, int fd)
    {
      RequestBuilder req = conn.newRequest("file_close");
      req.add("fd", fd);
      return conn.send(req);
    }

    /// <summary>Read into a stream. If you need data in a byte array, wrap the array into a <see cref="MemoryStream" />.</summary>
    public static Task readFile(this Connection conn, int fd, Stream dest, long count)
    {
      RequestBuilder req = conn.newRequest("file_read");
      req.add("fd", fd);
      req.add("count", count);
      return conn.download(req, dest, count);
    }

    /// <summary>Write file by copying specified count of bytes from the stream.</summary>
    public static async Task<long> writeFile(this Connection conn, int fd, Stream source, long count)
    {
      RequestBuilder req = conn.newRequest("file_write", count);
      req.add("fd", fd);
      var response = await conn.upload(req, source, count);
      return response.dict.getLong("bytes");
    }

    /// <summary>Set the current offset of the file descriptor to offset bytes.</summary>
    public static async Task<long> seekFile(this Connection conn, int fd, long offset, SeekOrigin origin = SeekOrigin.Begin)
    {
      RequestBuilder req = conn.newRequest("file_seek");
      req.add("fd", fd);
      req.add("offset", offset);
      switch (origin)
      {
        case SeekOrigin.Begin:
          break;
        case SeekOrigin.Current:
          req.add("whence", 1);
          break;
        case SeekOrigin.End:
          req.add("whence", 2);
          break;
        default:
          throw new ArgumentException("Invalid seek origin", "seekOrigin");
      }
      var response = await conn.send(req);
      return response.dict.getLong("offset");
    }

    /// <summary>Set file size in bytes.</summary>
    /// <remarks>
    /// <para>If length is less than the file size, then the extra data is cut from the file, else the the file contents are extended with zeros as needed.</para>
    /// <para>The current offset is not modified.</para>
    /// </remarks>
    public static Task resizeFile(this Connection conn, int fd, long newLength)
    {
      RequestBuilder req = conn.newRequest("file_truncate");
      req.add("fd", fd);
      req.add("length", newLength);
      return conn.send(req);
    }

    /// <summary>MD5 and SHA1 checksums of the piece of the file</summary>
    public struct SliceChecksums
    {
      /// <summary>SHA-1 checksum</summary>
      public readonly byte[] sha1;
      /// <summary>MD5 checksum</summary>
      public readonly byte[] md5;
      /// <summary>Count of bytes for which the checksums were computed</summary>
      public readonly long length;

      internal SliceChecksums(IReadOnlyDictionary<string, object> dict)
      {
        sha1 = Utils.bytesFromHex((string)dict["sha1"]);
        md5 = Utils.bytesFromHex((string)dict["md5"]);
        length = dict.getLong("size");
      }

      /// <summary>Returns a string that represents the current object.</summary>
      public override string ToString()
      {
        return $"{ length } bytes, sha1 { Utils.hexString(sha1) }, md5 { Utils.hexString(md5) }";
      }
    }

    /// <summary>Compute MD5 and SHA1 checksums of a slice of the file</summary>
    public static async Task<SliceChecksums> checksumFileSlice(this Connection conn, int fd, long sliceStart, long sliceLength)
    {
      RequestBuilder req = conn.newRequest("file_checksum");
      req.add("fd", fd);
      req.add("count", sliceLength);
      req.add("offset", sliceStart);
      var response = await conn.send(req);
      return new SliceChecksums(response.dict);
    }

    /// <summary>File size and position</summary>
    public struct FileSize
    {
      /// <summary>File size in bytes</summary>
      public readonly long length;

      /// <summary>Current file position</summary>
      public readonly long position;

      internal FileSize(IReadOnlyDictionary<string, object> dict)
      {
        length = dict.getLong("size");
        position = dict.getLong("offset");
      }

      /// <summary>Returns a string that represents the current object.</summary>
      public override string ToString()
      {
        return $"length { length }, position { position }";
      }
    }

    /// <summary>Return (size, position) of a file descriptor.</summary>
    public static async Task<FileSize> getFileSize(this Connection conn, int fd)
    {
      RequestBuilder req = conn.newRequest("file_size");
      req.add("fd", fd);
      var response = await conn.send(req);
      return new FileSize(response.dict);
    }

    const int defaultBufferSize = 512 * 1024;

    /// <summary>Download a complete file</summary>
    public static async Task downloadFile(this Connection conn, FileInfo fi, Stream destStream, int bufferSize = defaultBufferSize)
    {
      if (fi.length <= 0)
        return;

      if (bufferSize <= 0)
        bufferSize = defaultBufferSize;
      if (fi.length <= bufferSize)
        bufferSize = (int)fi.length;

      // Open the file
      int fd = (await conn.createFileImpl(fi.id, eFileOpenFlags.None)).fd;

      // Send read request for the complete file. Let's hope their server software is well designed and streams data, instead of trying to read the complete file to RAM first.
      RequestBuilder req = conn.newRequest("file_read");
      req.add("fd", fd);
      req.add("count", fi.length);

      try
      {
        await conn.download(req, destStream, fi.length);
      }
      finally
      {
        // Close the file
        await conn.closeFile(fd);
      }
    }

    /// <summary>Download a complete file specified by absolute name</summary>
    public static async Task downloadFile(this Connection conn, string path, Stream destStream, int bufferSize = defaultBufferSize)
    {
      int fd = (await conn.createFileImpl(path, eFileOpenFlags.None)).fd;

      try
      {
        long length = (await conn.getFileSize(fd)).length;

        if (bufferSize <= 0)
          bufferSize = defaultBufferSize;
        if (length <= bufferSize)
          bufferSize = (int)length;

        var req = conn.newRequest("file_read");
        req.add("fd", fd);
        req.add("count", length);
        await conn.download(req, destStream, length);
      }
      finally
      {
        await conn.closeFile(fd);
      }
    }

    /// <summary>"metadata" property of the response</summary>
    internal static IReadOnlyDictionary<string, object> metadata(this Response response)
    {
      return (IReadOnlyDictionary<string, object>)response.dict["metadata"];
    }

    /// <summary>Upload a file.</summary>
    public static async Task<FileInfo> uploadFile(this Connection conn, FolderInfo folder, string name, Stream sourceStream, bool noPartial = true, bool renameIfExists = false)
    {
      sourceStream.rewind();
      var req = conn.newRequest("uploadfile", sourceStream.Length);
      req.add("folderid", folder.id);
      req.add("filename", name);
      if (noPartial)
        req.add("nopartial", true);
      if (renameIfExists)
        req.add("renameifexists", true);
      req.unixTimestamps();

      var response = await conn.upload(req, sourceStream);
      return new FileInfo(response.metadata());
    }

    /// <summary>Delete a file</summary>
    public static Task deleteFile(this Connection conn, FileInfo fi)
    {
      var req = conn.newRequest("deletefile");
      req.add("fileid", fi.id);
      req.unixTimestamps();

      return conn.send(req);
    }

    /// <summary>Delete a file</summary>
    public static Task deleteFile(this Connection conn, long fileId)
    {
      var req = conn.newRequest("deletefile");
      req.add("fileid", fileId);
      req.unixTimestamps();

      return conn.send(req);
    }

    /// <summary>Refresh file metadata</summary>
    public static async Task<FileInfo> getFileInfo(this Connection conn, long id)
    {
      var req = conn.newRequest("stat");
      req.add("fileid", id);
      req.unixTimestamps();

      var response = await conn.send(req);
      return new FileInfo(response.metadata());
    }

    public static async Task<string> GetFilePublicLink(this Connection conn, long id)
    {
      string link = null;
      var req = conn.newRequest("getfilelink");
      req.add("fileid", id);
      req.unixTimestamps();

      var response = await conn.send(req);

      var host = ((object[])response.dict["hosts"])[0].ToString();
      var path = response.dict["path"];

      link = "http://" + host + path;

      return link;
    }


    public static async Task<PCloudResponse<Stats>> GetFileStats(this Connection conn, long id)
    {
      PCloudResponse<Stats> data = null;
      var req = conn.newRequest("stat");
      req.add("fileid", id);
      req.unixTimestamps();

      var response = await conn.send(req, true);

      var json = response.GetJson();

      var options = new JsonSerializerOptions()
      {
        AllowTrailingCommas = true
      };

      if (!string.IsNullOrEmpty(json))
      {
        data = JsonSerializer.Deserialize<PCloudResponse<Stats>>(json, options);

      }
    

      return data;
    }
  }
}