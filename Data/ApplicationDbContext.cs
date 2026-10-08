using Microsoft.EntityFrameworkCore;
using tui.Models;

namespace tui.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
}
