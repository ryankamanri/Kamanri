using System.Threading;
using System.Threading.Tasks;
namespace Kamanri.Utils
{
	public class Mutex
	{
		public bool Flag { get;private set; } = false;

		private int WaitTime { get; } = 5;

		private readonly System.Threading.Mutex _mutex = new System.Threading.Mutex();

		public Mutex(){}

		public Mutex(int waitTime)
		{
			WaitTime = waitTime;
		}

		public Mutex(bool initialMutex = false)
		{
			this.Flag = initialMutex;
		}
		public void Wait()
		{
			_mutex.WaitOne();
			while (this.Flag == true)
			{
				Thread.Sleep(WaitTime);
			}
			this.Flag = true;
			_mutex.ReleaseMutex();

		}

		public void Signal()
		{
			this.Flag = false;
		}
	 
	}
}