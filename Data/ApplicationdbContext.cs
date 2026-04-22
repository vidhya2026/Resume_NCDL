using Microsoft.EntityFrameworkCore;
using Resume_NCDL.Models;

namespace Resume_NCDL.Data
{
    public class ApplicationdbContext : DbContext
    {
        public ApplicationdbContext(DbContextOptions<ApplicationdbContext> options)
            : base(options) { }

        public DbSet<Details> InfoTable { get; set; }
        public DbSet<ResumeMatchResult> ResumeMatchResults { get; set; }  
    }
}