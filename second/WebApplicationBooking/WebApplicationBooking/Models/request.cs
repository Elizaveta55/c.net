using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplicationBooking.Models
{
    public class Request
    {
        public int Id { get; set; }

        [Required]
        public string Country { get; set; }

        [Required]
        public DateTime Datebegin { get; set; }

        [Required]
        public DateTime Dateend { get; set; }

        public override string ToString()
        {
            return Datebegin + " " + Dateend + " " + Country;
        }
    }
}
