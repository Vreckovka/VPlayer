



using System.Collections.Generic;

namespace VPlayer.Library.VirtualList
{
	public interface IObjectGenerator<T>
	{
		/// <summary>
		/// Returns the number of items in the collection.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Generate the item that is located on the specified index.
		/// </summary>
		/// <remarks>
		/// This method is only be called once per index.
		/// </remarks>
		/// <param name="index">Index of the items that must be generated.</param>
		/// <returns>Fresh new instance.</returns>
		T CreateObject(int index);


		T[] AllItems { get; }
	}
}