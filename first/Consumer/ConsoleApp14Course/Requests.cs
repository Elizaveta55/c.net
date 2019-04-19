using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace ConsoleApp14Course
{
    [DataContract]
    class Requests
    {
        [DataMember]
        public int RequestID{get;set;}
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Lastname { get; set; }
        [DataMember]
        public string Midname { get; set; }
        [DataMember]
        public int RequestNumber {get;set;}
        [DataMember]
        public int Gender { get; set; }
        [DataMember]
        public DateTime RequestDate { get; set; }
        [DataMember]
        public string Xml { get; set; }
    }

}
