using ClassLibrary.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary.Database;

public class ApplicationContext : DbContext
{
    public DbSet<RecognitionResult> RecognitionResults { get; set; } = null!;
}
