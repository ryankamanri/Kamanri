using System;
namespace Kamanri.Utils
{
	class Construct
	{
		public static T DefaultConstructor<T>()
		{
			try
			{
				return (dynamic)typeof(T).GetConstructor(new Type[0]).Invoke(null);
			}
			catch (Exception e)
			{
				throw new Exception($"Cannot Create The Default Constructor Of {typeof(T)}", e);
			}

		}
	}
}
