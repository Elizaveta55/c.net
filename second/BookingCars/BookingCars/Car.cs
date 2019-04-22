using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingCars
{
    class Car
    {
        public int ID { get; set; }
        public DateTime DayComing { get; set; }
        public DateTime DayClosing { get; set; }
        public string CarInformation { get; set; }
        public string FirmInformation { get; set; }
        public string CarCountry { get; set; }
        public int CarsCount { get; set; }
    }
}
