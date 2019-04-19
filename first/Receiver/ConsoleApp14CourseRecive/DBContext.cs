using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp14CourseRecive
{
    class DBContext: DbContext
    {
        public DBContext()
            : base("DbConnection")
        { }

        public DbSet<requests> Requests { get; set; }
    }
}
