using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace DataTypes
{
    [DataContract()]
    public class Job
    {
        [DataMember]
        public string script;
        [DataMember]
        public object solution;
        [DataMember]
        public byte[] hash;
        public Job(string _script, byte[] _hash)
        {
            script = _script;
            solution = null;
            hash = _hash;
        }
    }
}
