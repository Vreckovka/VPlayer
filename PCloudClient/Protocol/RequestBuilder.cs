using System;
using System.IO;
using System.Text;

namespace PCloud
{
	/// <summary>Prepare binary requests in MemoryStream buffer.</summary>
	class RequestBuilder
	{
		const int initialBufferCapacity = 128;

		readonly MemoryStream buffer;
		static readonly Encoding encNames = Encoding.ASCII;
		static readonly Encoding encValues = Encoding.UTF8;
		bool isClosed = false;
		readonly long parametersCountOffset;
		int parametersCount = 0;

		/// <summary>Construct the request, set name and payload length</summary>
		public RequestBuilder( string method, long? payloadLength = null )
		{
			// Validate a few things
			if( string.IsNullOrWhiteSpace( method ) )
				throw new ArgumentException( "Method can't be empty" );
			int methodBytesCount = encNames.GetByteCount( method );
			if( methodBytesCount >= 0x80 )
				throw new ArgumentOutOfRangeException( "Method is too long, 127 bytes max" );
			if( payloadLength.HasValue && payloadLength.Value < 0 )
				throw new ArgumentException( "Payload length can't be negative" );

			buffer = new MemoryStream( initialBufferCapacity );
			// First 2 bytes are for the length. Constructor leaves them 0.
			byte[] header = Utils.bufferRent( 3 );
			// The first byte of the request gives the length of the name of the method - method_len( bits 0 - 6 ) and indicates if the request has data( bit 7 ).
			header[ 2 ] = (byte)methodBytesCount;
			if( payloadLength.HasValue )
				header[ 2 ] |= 0x80;
			// The first 2 bytes are garbage, ArrayPool doesn't zero initialize, but that's OK, close() method below overwrites them anyway.
			buffer.write( header, 3 );
			Utils.bufferReturn( header );

			// If the highest bit(7) is set, than the following 8 bytes represent 64 bit number, that is the length of the data that comes immediately after the request.
			if( payloadLength.HasValue )
				buffer.write( BitConverter.GetBytes( payloadLength.Value ) );

			// The next method_len bytes are the name of the method to be called.
			buffer.write( method, methodBytesCount, encNames );

			// The following one byte represent 8 bit number containing the number of parameters passed.
			parametersCountOffset = buffer.Length;
			buffer.WriteByte( 0 );
		}

		enum eParamType: byte
		{
			String = 0,
			Number = 1 << 6,
			Boolean = 2 << 6
		};

		void writeName( string name, eParamType tp )
		{
			if( isClosed )
				throw new InvalidOperationException( "This request is closed, please create another one" );

			if( null == name )
				throw new ArgumentNullException( "name" );

			int nameBytesCount = encNames.GetByteCount( name );
			if( nameBytesCount >= 0x40 )
				throw new ArgumentException( $"Parameter name is too long, they're limited to 63 bytes" );
			// For each parameter, the first byte represent the parameter type index in two highest bits ( 6 - 7 ) and the length of the parameter name ( param_name_len ) in the low 6 bits( 0 - 5 )
			byte firstByte = (byte)tp;
			firstByte |= (byte)nameBytesCount;
			buffer.WriteByte( firstByte );
			buffer.write( name, nameBytesCount, encNames );
			parametersCount++;
		}

		/// <summary>Append a string parameter</summary>
		public void add( string name, string value )
		{
			writeName( name, eParamType.String );
			// string, 4 byte length and string contents follow
			int valueBytesCount = encValues.GetByteCount( value );
			buffer.write( BitConverter.GetBytes( valueBytesCount ) );
			buffer.write( value, valueBytesCount, encValues );
		}

		/// <summary>Append a string parameter, avoiding to use shared buffer for the value.</summary>
		public void addSecret( string name, string value )
		{
			writeName( name, eParamType.String );
			byte[] valueBytes = encValues.GetBytes( value );
			buffer.write( BitConverter.GetBytes( valueBytes.Length ) );
			buffer.write( valueBytes );
		}

		/// <summary>Append an integer parameter</summary>
		public void add( string name, long value )
		{
			if( value >= 0 )
			{
				writeName( name, eParamType.Number );
				// number, 8 byte (64 bit) number representation follow
				buffer.write( BitConverter.GetBytes( value ) );
			}
			else
			{
				// All numbers are positive numbers. If you need to send a negative number (for example a negative file descriptor, see below), send it as string.
				add( name, value.ToString() );
			}
		}

		/// <summary>Append a boolean parameter</summary>
		public void add( string name, bool value )
		{
			writeName( name, eParamType.Boolean );
			// boolean, 1 byte, zero representing false and all other values represent true
			buffer.WriteByte( value ? (byte)1 : (byte)0 );
		}

		/// <summary>Close the request and return the stream you can copy to the network</summary>
		public Stream close()
		{
			if( isClosed )
			{
				buffer.rewind();
				return buffer;
			}

			// The request to the API servers starts with 16 bit length of the request.
			// When there is a length preceding the payload, the length does not include itself, that is if you have 4 bytes length and 10 bytes payload, length will be 10, not 14.
			int length = (int)buffer.Length - 2;
			if( length >= 0x10000 )
				throw new ArgumentException( "The request is too long, it exceeds 64kb" );
			if( parametersCount >= 0x100 )
				throw new ArgumentException( "The request has too many parameters, 255 is the maximum" );

			buffer.rewind();
			buffer.write( BitConverter.GetBytes( (ushort)length ) );

			// Write parameters count
			if( parametersCount > 0 )
			{
				buffer.Seek( parametersCountOffset, SeekOrigin.Begin );
				buffer.WriteByte( (byte)parametersCount );
			}

			isClosed = true;
			buffer.rewind();
			return buffer;
		}
	}
}