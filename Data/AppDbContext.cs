using Microsoft.EntityFrameworkCore;
using HunterGrassServices.Api.Models;

namespace HunterGrassServices.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<QuoteRequest> QuoteRequests => Set<QuoteRequest>();
}
