using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PCloud
{
	/// <summary>Byte array to parse binary responses from.</summary>
	class ResponseBuffer: IDisposable
	{
		/// <summary>Buffer with the complete binary JSON.</summary>
		readonly byte[] buffer;

		/// <summary>For performance reasons, the above buffer comes from ArrayPool&lt;byte&gt;, can be longer than response. This readonly field has the actual length.</summary>
		readonly int length;

		/// <summary>Current read offset. The parser reads the data sequentially.</summary>
		int offset = 0;

		/// <summary>Encoding for strings.</summary>
		static readonly Encoding encStrings = Encoding.UTF8;

		/// <summary>Release buffer back to the pool</summary>
		public void Dispose()
		{
			if( null != buffer )
				Utils.bufferReturn( buffer );
		}

		/// <summary>Receive response (without payload) into a new buffer</summary>
		public static async Task<ResponseBuffer> receive( Stream connection )
		{
			// Receive size
			byte[] buffer = Utils.bufferRent( 4 );
			int length;
			try
			{
				await connection.read( buffer, 4 );
				length = BitConverter.ToInt32( buffer, 0 );
			}
			finally
			{
				Utils.bufferReturn( buffer );
			}

			// Receive the payload
			buffer = Utils.bufferRent( length );
			try
			{
				await connection.read( buffer, length );
				return new ResponseBuffer( buffer, length );
			}
			catch( Exception )
			{
				Utils.bufferReturn( buffer );
				throw;
			}
		}

		/// <summary>The constructor is private. To create the buffer, call await <see cref="receive( Stream )" /> static method.</summary>
		ResponseBuffer( byte[] buffer, int length )
		{
			this.buffer = buffer;
			this.length = length;
		}

		/// <summary>If the response doesn't have `n` bytes remaining to read, throw EndOfStreamException</summary>
		void checkRemainingBytes( int n )
		{
			if( offset + n <= length )
				return; // This code is called a lot during parsing. Let's hope speculative execution will do the job. Under normal circumstances, this always returns without throwing an exception.
			throw new EndOfStreamException( $"Incomplete response: binary JSON parser wants to read { n.pluralString( "byte" ) }, only { ( length - offset ).pluralString( "byte" ) } remaining" );
		}

		public bool EOF => offset >= length;

		/// <summary>Get next byte, don't move the read pointer</summary>
		public byte peekByte()
		{
			checkRemainingBytes( 1 );
			return buffer[ offset ];
		}

		/// <summary>Increment read pointer by 1 byte</summary>
		public void skipByte()
		{
			checkRemainingBytes( 1 );
			offset++;
		}

		/// <summary>Read 1 byte integer</summary>
		public byte readInt1()
		{
			checkRemainingBytes( 1 );
			byte res = buffer[ offset ];
			offset++;
			return res;
		}

		/// <summary>Read 2 bytes integer</summary>
		public ushort readInt2()
		{
			checkRemainingBytes( 2 );
			ushort res = BitConverter.ToUInt16( buffer, offset );
			offset += 2;
			return res;
		}

		/// <summary>Read 3 bytes integer</summary>
		public uint readInt3()
		{
			checkRemainingBytes( 3 );
			uint res = BitConverter.ToUInt16( buffer, offset );
			res |= (uint)( buffer[ offset + 2 ] ) << 16;
			offset += 3;
			return res;
		}

		/// <summary>Read 4 bytes integer</summary>
		public uint readInt4()
		{
			checkRemainingBytes( 4 );
			uint res = BitConverter.ToUInt32( buffer, offset );
			offset += 4;
			return res;
		}

		/// <summary>Make ulong value by combining low and high 32-bit pieces.</summary>
		static ulong makeUlong( uint low, uint hi )
		{
			ulong res = hi;
			res = res << 32;
			return res | low;
		}

		/// <summary>Read 5 bytes integer</summary>
		public ulong readInt5()
		{
			return makeUlong( readInt4(), readInt1() );
		}

		/// <summary>Read 6 bytes integer</summary>
		public ulong readInt6()
		{
			return makeUlong( readInt4(), readInt2() );
		}

		/// <summary>Read 7 bytes integer</summary>
		public ulong readInt7()
		{
			return makeUlong( readInt4(), readInt3() );
		}

		/// <summary>Read 8 bytes integer</summary>
		public ulong readInt8()
		{
			checkRemainingBytes( 8 );
			ulong res = BitConverter.ToUInt64( buffer, offset );
			offset += 8;
			return res;
		}

		/// <summary>Read string of the specified length. The length is in encoded bytes, <b>not</b> characters.</summary>
		public string readString( int length )
		{
			checkRemainingBytes( length );
			string result = encStrings.GetString( buffer, offset, length );
			offset += length;
			return result;
		}
	}
}