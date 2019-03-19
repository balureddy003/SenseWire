using System;
using System.Threading;
using System.Threading.Tasks;

namespace PersistanceAgent
{
    public interface IAgent
    {
        Task RunAsync(CancellationToken runState);
    }

    public class Agent : IAgent
    {
        public Task RunAsync(CancellationToken runState)
        {
            Console.WriteLine("Agent running");
            return null;
        }
    }
}
