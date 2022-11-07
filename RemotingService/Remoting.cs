using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataTypes;
using System.ServiceModel;
using System.Security.Cryptography;

namespace RemotingService
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public sealed class Remoting : RemotingInterface
    {
        List<Job> jobs = new List<Job>();
        private static Remoting instance = null;
        private static readonly object padlock = new object();

        private Remoting()
        {

        }
        public static Remoting Instance
        {
            get {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Remoting();
                    }
                    return instance;
                }
            }
        }
        public void AddJob(Job job)
        {
            byte[] data = generateHash(job.script);
            if (data.SequenceEqual(job.hash))
            {
                instance.jobs.Add(job);
            }
            else
            {
                Console.WriteLine("Data Corrupted");
            }
        }
        public byte[] generateHash(string s)
        {
            return SHA256.Create().ComputeHash(Convert.FromBase64String(s));
        }
        public List<int> GetUnfinishedJobs()
        {
            List<int> unfinishedJobs = new List<int>();
            for (int i = 0; i < instance.jobs.Count; i++)
            {
                if (instance.jobs[i].solution == null)
                {
                    unfinishedJobs.Add(i);
                }
            }
            return unfinishedJobs;
        }
        public Job GetJob(int index)
        {
            return instance.jobs[index];
        }
        public bool AddSoultion(object solution, int index)
        {
            instance.jobs[index].solution = solution;
            return true;
        }
    }
}
