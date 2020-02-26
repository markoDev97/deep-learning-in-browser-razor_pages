using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PatternRecognitionProject.Models;

namespace PatternRecognitionProject.Data
{
    public class EntryContext : DbContext
    {
        public EntryContext (DbContextOptions<EntryContext> options)
            : base(options)
        {
        }

        public DbSet<DataUnit> DataUnit { get; set; }
    }
}
