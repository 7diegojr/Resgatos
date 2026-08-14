using Microsoft.EntityFrameworkCore;
using Resgatos.Models;

namespace Resgatos.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) // Injeção de dependência do ASP.NET Core (Program.cs)
    {
    }

    // Registro das tabelas
    public DbSet<Faq> Faqs { get; set; }
    public DbSet<Transparencia> Transparencias { get; set; }
    public DbSet<UsuarioAdmin> UsuariosAdmin { get; set; }
    public DbSet<Animal> Animais { get; set; }
    public DbSet<AnimalFoto> AnimalFotos { get; set; }
    public DbSet<Adotado> Adotados { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) // Regras que Data Annotations sozinhas não fazem
    {
        base.OnModelCreating(modelBuilder);


        // ---------- Índices únicos ----------        

        modelBuilder.Entity<UsuarioAdmin>()
            .HasIndex(u => u.Email) // cria índice na coluna Email
            .IsUnique(); // é único


        modelBuilder.Entity<Adotado>()
            .HasIndex(a => a.IdAnimal) // cria índice na coluna IdAnimal
            .IsUnique(); // é único (1:1)

        // ---------- Relacionamentos ----------

        modelBuilder.Entity<AnimalFoto>()
            .HasOne(af => af.Animal) // cada AnimalFoto tem um Animal
            .WithMany(a => a.Fotos) // cada Animal tem várias AnimalFoto (1:N)
            .HasForeignKey(af => af.IdAnimal) // FK: IdAnimal
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Adotado>()
            .HasOne(ad => ad.Animal) // cada Adotado tem um Animal
            .WithOne(a => a.Adotado) // cada Animal tem, no máximo, um Adotado (1:1)
            .HasForeignKey<Adotado>(ad => ad.IdAnimal)
            .OnDelete(DeleteBehavior.NoAction);
    }
}