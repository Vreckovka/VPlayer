using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PCloud
{
	static class Utils
	{
		public static void write( this Stream stm, byte[] buffer )
		{
			stm.Write( buffer, 0, buffer.Length );
		}

		public static void write( this Stream stm, byte[] buffer, int length )
		{
			stm.Write( buffer, 0, length );
		}

		/// <summary>Retrieves a buffer that is at least the requested length.</summary>
		public static byte[] bufferRent( int cb )
		{
			return ArrayPool<byte>.Shared.Rent( cb );
		}

		/// <summary>Returns an array to the pool that was previously obtained from <see cref="bufferRent" />.</summary>
		public static void bufferReturn( byte[] buffer )
		{
			ArrayPool<byte>.Shared.Return( buffer );
		}

		/// <summary>Write string to stream with specified encoding, using ArrayPool to save RAM allocations.</summary>
		public static void write( this Stream stm, string str, int bytesCount, Encoding enc )
		{
#if DEBUG
			Debug.Assert( bytesCount == enc.GetByteCount( str ) );
#endif
			byte[] buffer = bufferRent( bytesCount );
			int cb = enc.GetBytes( str, 0, str.Length, buffer, 0 );
			Debug.Assert( cb == bytesCount );
			stm.write( buffer, bytesCount );
			bufferReturn( buffer );
		}

		public static void rewind( this Stream stm )
		{
			stm.Seek( 0, SeekOrigin.Begin );
		}

		public static V lookup<K, V>( this IReadOnlyDictionary<K, V> dict, K key )
		{
			V v;
			dict.TryGetValue( key, out v );
			return v;
		}

		public static async Task read( this Stream stm, byte[] buffer, int length )
		{
			int offset = 0;
			while( true )
			{
				int cb = await stm.ReadAsync( buffer, offset, length );
				if( 0 == cb )
					throw new EndOfStreamException();
				length -= cb;
				if( length <= 0 )
					return;
				offset += cb;
			}
		}

		// https://stackoverflow.com/a/24343727/126995
		static readonly uint[] s_hexStringLookup = createStringLookup();

		private static uint[] createStringLookup()
		{
			var result = new uint[ 256 ];
			for( int i = 0; i < 256; i++ )
			{
				string s = i.ToString( "x2" );
				result[ i ] = ( (uint)s[ 0 ] ) | ( (uint)s[ 1 ] << 16 );
			}
			return result;
		}

		public static string hexString( byte[] bytes )
		{
			uint[] lookup32 = s_hexStringLookup;
			char[] result = new char[ bytes.Length * 2 ];
			for( int i = 0; i < bytes.Length; i++ )
			{
				uint val = lookup32[ bytes[ i ] ];
				result[ 2 * i ] = (char)val;
				result[ 2 * i + 1 ] = (char)( val >> 16 );
			}
			return new string( result );
		}

		/// <summary>Compute SHA1 of UTF8 bytes of the input string, convert result to lowercase hexadecimal string without delimiters between bytes</summary>
		public static string sha1( string input )
		{
			using( SHA1Managed sha1 = new SHA1Managed() )
			{
				// Not using ArrayPool here, login is not performance critical but it is security critical.
				var hash = sha1.ComputeHash( Encoding.UTF8.GetBytes( input ) );
				return hexString( hash );
			}
		}

		static readonly int[] hexValueLookup = new int[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05,
			0x06, 0x07, 0x08, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

		public static byte[] bytesFromHex( string Hex )
		{
			// https://stackoverflow.com/a/5919521/126995
			byte[] Bytes = new byte[ Hex.Length / 2 ];
			for( int x = 0, i = 0; i < Hex.Length; i += 2, x += 1 )
				Bytes[ x ] = (byte)( hexValueLookup[ char.ToUpper( Hex[ i + 0 ] ) - '0' ] << 4 | hexValueLookup[ char.ToUpper( Hex[ i + 1 ] ) - '0' ] );
			return Bytes;
		}

		/// <summary>Similar to Stream.CopyToAsync, but copies exactly specified count of bytes. Throws EndOfStreamException when the source doesn't have enough bytes.</summary>
		public static async Task copyData( this Stream from, Stream to, long length, int bufferSize = 256 * 1024 )
		{
			if( length < bufferSize )
				bufferSize = (int)length;
			byte[] buffer = bufferRent( bufferSize );
			try
			{
				while( length > 0 )
				{
					int cbRequest = bufferSize;
					if( cbRequest > length )
						cbRequest = (int)length;

					int cbRead = await from.ReadAsync( buffer, 0, cbRequest );
					if( 0 == cbRead )
						throw new EndOfStreamException();

					await to.WriteAsync( buffer, 0, cbRead );
					length -= cbRead;
				}
			}
			finally
			{
				bufferReturn( buffer );
			}
		}

		/// <summary>Enumerate items of the array. Don't crash if it's null.</summary>
		public static IEnumerable<T> enumerate<T>( this T[] arr )
		{
			return null == arr ? Enumerable.Empty<T>() : arr;
		}

		public static bool notEmpty<T>( this T[] arr )
		{
			return ( arr?.Length ?? 0 ) > 0;
		}
		public static bool isEmpty<T>( this T[] arr )
		{
			return null == arr || arr.Length <= 0;
		}

		/// <summary>Read and discard specified count of bytes from the stream.</summary>
		public static async Task fastForward( this Stream from, long length )
		{
			int bufferSize = 256 * 1024;
			byte[] buffer = bufferRent( bufferSize );
			try
			{
				while( length > 0 )
				{
					int cbRequest = bufferSize;
					if( cbRequest > length )
						cbRequest = (int)length;
					int cbRead = await from.ReadAsync( buffer, 0, cbRequest );
					if( 0 == cbRead )
						throw new EndOfStreamException();
					length -= cbRead;
				}
			}
			finally
			{
				bufferReturn( buffer );
			}
		}

		/// <summary>A string like "1 byte" or "12 bytes"</summary>
		public static string pluralString( this int i, string single )
		{
			if( 1 != i )
				return $"{ i } { single }s";
			return $"1 { single }";
		}
	}
}