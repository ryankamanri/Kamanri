using System.Threading;
using System.Threading.Tasks;
namespace Kamanri.Utils
{
	public class Mutex
	{
		public bool Flag { get;private set; } = false;

		private int WaitTime { get; } = 5;

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
			while (this.Flag == true)
			{
				Thread.Sleep(WaitTime);
			}
			this.Flag = true;

		}

		public void Signal()
		{
			this.Flag = false;
		}
	 
	}
}