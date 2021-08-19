namespace PCloud
{
	static class RequestExt
	{
		/// <summary>Request UTC unix timestamps. This library only supports that format, much cheaper to parse than strings, the server doesn't send milliseconds anyway.</summary>
		public static void unixTimestamps( this RequestBuilder request )
		{
			request.add( "timeformat", "timestamp" );
		}
	}
}