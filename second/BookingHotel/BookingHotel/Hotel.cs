using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingHotel
{
    class Hotel
    {
        public int ID { get; set; }
        public int PersonsCount { get; set; }
        public DateTime DayComing { get; set; }
        public DateTime DayClosing { get; set; }
        public string RoomInformation { get; set; }
        public string HotelInformation { get; set; }
        public string HotelCountry { get; set; }
        public int RoomsCount { get; set; }
    }
}
