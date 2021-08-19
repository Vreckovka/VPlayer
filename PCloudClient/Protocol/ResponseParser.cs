using System;
using System.Collections.Generic;
using System.Linq;

namespace PCloud
{
	/// <summary>Parse pCloud's proprietary binary JSON into untyped .NET objects.</summary>
	/// <remarks>All integers are unsigned, i.e. you'll get byte, ushort, uint, or ulong for integer values.</remarks>
	class ResponseParser
	{
		readonly List<string> strings = new List<string>();

		/// <summary>`Data` objects in the response, if any, are accumulated in this list, in the order they were encountered while parsing the response.</summary>
		public List<Response.Data> payloads { get; private set; } = null;

		readonly ResponseBuffer reader;

		/// <summary>Construct from a downloaded buffer.</summary>
		public ResponseParser( ResponseBuffer response )
		{
			reader = response;
		}

		/// <summary>Parse into the sequence of .NET objects.</summary>
		public IEnumerable<object> parse()
		{
			// Too bad there're no algebraic types in current C#, boxing all these numbers is relatively inefficient.
			while( true )
			{
				if( reader.EOF )
					yield break;
				yield return parseValue();
			}
		}

		/// <summary>return an exception telling about unknown type byte in the response.</summary>
		static Exception unknownType( byte tp )
		{
			return new ArgumentException( $"Unknown value type byte 0x{ tp.ToString( "x2" ) }" );
		}

		object parseValue()
		{
			byte tp = reader.readInt1();

			switch( tp )
			{
				// New string types
				case 0:
					return readString( reader.readInt1() );
				case 1:
					return readString( reader.readInt2() );
				case 2:
					return readString( (int)reader.readInt3() );
				case 3:
					return readString( (int)reader.readInt4() );
				// Reused string types
				case 4:
					return strings[ reader.readInt1() ];
				case 5:
					return strings[ reader.readInt2() ];
				case 6:
					return strings[ (int)reader.readInt3() ];
				case 7:
					return strings[ (int)reader.readInt4() ];
				// Numbers types
				case 8:
					return reader.readInt1();
				case 9:
					return reader.readInt2();
				case 10:
					return reader.readInt3();
				case 11:
					return reader.readInt4();
				case 12:
					return reader.readInt5();
				case 13:
					return reader.readInt6();
				case 14:
					return reader.readInt7();
				case 15:
					return reader.readInt8();
				// Miscellaneous
				case 16:
					return parseHash();
				case 17:
					return parseArray().ToArray();
				case 18:
					return false;
				case 19:
					return true;
				case 20:
					return readData();
			}

			if( tp < 100 )
				throw unknownType( tp );

			// [ 100, 149 ] - short string between 0 and 49 bytes in length (type-100)
			if( tp < 150 )
				return readString( tp - 100 );
			// [ 150, 199 ] - for string ids between 0 and 49 id is directly encoded in type
			if( tp < 200 )
				return strings[ tp - 150 ];

			// [ 200, 219 ] - numbers between 0 and 19 are directly encoded in the type parameter
			if( tp < 220 )
				return (byte)( tp - 200 );

			throw unknownType( tp );
		}

		string readString( int length )
		{
			string result = reader.readString( length );
			strings.Add( result );
			return result;
		}

		Dictionary<string, object> parseHash()
		{
			Dictionary<string, object> result = new Dictionary<string, object>();
			while( true )
			{
				if( reader.peekByte() == 0xFF )
				{
					reader.skipByte();
					return result;
				}
				object key = parseValue();
				string strKey = key as string;
				if( strKey == null )
					throw new ArgumentException( $"Hash keys must be strings, got { key.GetType().Name } instead." );
				result.Add( strKey, parseValue() );
			}
		}

		IEnumerable<object> parseArray()
		{
			while( true )
			{
				if( reader.peekByte() == 0xFF )
				{
					reader.skipByte();
					yield break;
				}
				yield return parseValue();
			}
		}

		Response.Data readData()
		{
			Response.Data rd = new Response.Data( (long)reader.readInt8() );
			if( null == payloads )
				payloads = new List<Response.Data>( 1 );
			payloads.Add( rd );
			return rd;
		}
	}
}