using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingPlane
{
    class Plane
    {
        public int ID { get; set; }
        public string HomeCountry { get; set; }
        public string DestinatiomCountry { get; set; }
        public DateTime FlightDay { get; set; }
        public string FlightInformation { get; set; }
        public int TicketsCount { get; set; }
    }
}
