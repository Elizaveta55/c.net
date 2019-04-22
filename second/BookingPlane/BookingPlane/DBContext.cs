using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingPlane
{
    class DBContext : DbContext
    {
        public DBContext()
            : base("DbConnection")
        {
            Database.SetInitializer(
                new DropCreateDatabaseIfModelChanges<DBContext>());
        }

        public DbSet<Plane> Planes { get; set; }
    }
}
