using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingHotel
{
    class DBContext : DbContext
    {
        public DBContext()
            : base("DbConnection")
        {
            Database.SetInitializer(
                new DropCreateDatabaseIfModelChanges<DBContext>());
        }

        public DbSet<Hotel> Hotels { get; set; }
    }
}
