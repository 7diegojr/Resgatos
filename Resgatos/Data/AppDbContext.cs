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

        // ---------- Lixeira (exclusão lógica) ----------
        // Animal com ExcluidoEm preenchido está na lixeira: some de TODAS as consultas (site e admin)
        // sem precisar lembrar de filtrar em cada uma. Quem precisa enxergar a lixeira usa .IgnoreQueryFilters().
        // Fotos e adoção seguem o animal (o EF exige o mesmo filtro nos dois lados da relação).
        modelBuilder.Entity<Animal>()
            .HasQueryFilter(a => a.ExcluidoEm == null);

        modelBuilder.Entity<AnimalFoto>()
            .HasQueryFilter(af => af.Animal.ExcluidoEm == null);

        modelBuilder.Entity<Adotado>()
            .HasQueryFilter(ad => ad.Animal.ExcluidoEm == null);
    }
}