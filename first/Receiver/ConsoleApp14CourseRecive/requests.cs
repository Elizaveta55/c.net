using System;
using System.Runtime.Serialization;
using System.Xml;
using System.ComponentModel.DataAnnotations;

namespace ConsoleApp14CourseRecive
{
    [DataContract]
    public class requests
    {
        [DataMember]
        [Key]
        public int RequestID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Lastname { get; set; }
        [DataMember]
        public string Midname { get; set; }
        [DataMember]
        public int RequestNumber { get; set; }
        [DataMember]
        public int Gender { get; set; }
        [DataMember]
        public DateTime RequestDate { get; set; }
        [DataMember]
        public string Xml { get; set; }
    }
}
