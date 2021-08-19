using System;
using System.Collections.Generic;

namespace PCloud
{
	/// <summary>Utility functions to extract data from untyped dictionaries made by <see cref="ResponseParser" /> class.</summary>
	static class JSON
	{
		/// <summary>Get int32 from binary JSON</summary>
		public static int getInt( this IReadOnlyDictionary<string, object> dict, string key )
		{
			return Convert.ToInt32( dict[ key ] );
		}

		/// <summary>Get int32 from binary JSON, use default value if not found</summary>
		public static int getInt( this IReadOnlyDictionary<string, object> dict, string key, int defaultValue )
		{
			object obj = dict.lookup( key );
			if( null == obj )
				return defaultValue;
			return Convert.ToInt32( obj );
		}

		/// <summary>Get int64 from binary JSON</summary>
		public static long getLong( this IReadOnlyDictionary<string, object> dict, string key )
		{
			return Convert.ToInt64( dict[ key ] );
		}

		/// <summary>Get int64 from binary JSON, use default value if not found</summary>
		public static long getLong( this IReadOnlyDictionary<string, object> dict, string key, long defaultValue )
		{
			object obj = dict.lookup( key );
			if( null == obj )
				return defaultValue;
			return Convert.ToInt64( obj );
		}

		/// <summary>Convert timestamp from binary JSON into DateTime in UTC. The JSON is assumed to contain Unix timestamp, in seconds.</summary>
		public static DateTime getTimestamp( this IReadOnlyDictionary<string, object> dict, string key )
		{
			long seconds = dict.getLong( key );
			return DateTimeOffset.FromUnixTimeSeconds( seconds ).UtcDateTime;
		}
	}
}