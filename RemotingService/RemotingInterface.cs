using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using DataTypes;

namespace RemotingService
{
    [ServiceContract]
    public interface RemotingInterface
    {
        [OperationContract]
        void AddJob(Job job);
        [OperationContract]
        List<int> GetUnfinishedJobs();
        [OperationContract]
        Job GetJob(int index);
        [OperationContract]
        bool AddSoultion(object solution, int index);
    }
}
