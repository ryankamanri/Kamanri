using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Data.Common;
using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;

namespace Kamanri.Utils
{
	public class Pool<T>
	{
		private readonly List<KeyValuePair<T, Mutex>> pool;

		private int currentIndex = 0;
		public int Size { get; private set; } = 3;

		public Pool(int size, Func<T> SetOriginItem)
		{
			Size = size;
			pool = new List<KeyValuePair<T,Mutex>>();
			InitPool(SetOriginItem);
		}

		private void InitPool(Func<T> SetOriginItem)
		{
			for(int i = 0; i < Size; i++)
			{
				var originItem = SetOriginItem();
				pool.Add(new KeyValuePair<T, Mutex>(originItem, new Mutex(5)));
			}
		}

		public T Allocate()
		{
			pool[currentIndex].Value.Wait();
			return AllocateAndAssignMutex().Key;
		}

		public KeyValuePair<T, Mutex> AllocateAndAssignMutex()
		{
			for (int i = 0; i < Size; i++)
			{
				if (pool[currentIndex].Value.Flag == false) break;
				if (i == Size - 1)
					Console.WriteLine($"[{DateTime.Now}] : Current Index : {currentIndex}, All {typeof(T)} Are Full Used");
				currentIndex++;
				currentIndex %= Size;
			}
			return pool[currentIndex];
		}

		public void Free(T item)
		{
			var mutex = (from poolItem in pool
				where poolItem.Key.Equals(item)
				select poolItem.Value).FirstOrDefault();
			if (mutex == default)
				Console.WriteLine("The Given Item Is NOT In The Pool");
			else mutex.Signal();
		}

		public void Map(Action<T, Mutex> ItemDelegate)
		{
			foreach(var poolItem in pool)
				ItemDelegate(poolItem.Key, poolItem.Value);
		}
	}
}