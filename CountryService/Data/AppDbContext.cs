#nullable disable

using Microsoft.EntityFrameworkCore;
using CountryService.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace CountryService.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt)
        {

        }

        public DbSet<Country> Countries { get; set; }

        public DbSet<City> Cities { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}