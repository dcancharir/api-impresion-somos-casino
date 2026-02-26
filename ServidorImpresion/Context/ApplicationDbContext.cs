using Microsoft.EntityFrameworkCore;
using ServidorImpresion.Models;

namespace ServidorImpresion.Context
{
    public partial class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options)
        : base(options)
        {
        }
        public DbSet<ImpresionCupon> ImpresionCupons { get; set; }
        public DbSet<HistorialImpresionCupon> HistorialImpresionCupons { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ImpresionCupon>(entity => {
                entity.HasKey(x => x.ImpresionCuponId);
                entity.Property(x => x.ImpresionCuponId).ValueGeneratedOnAdd();
                entity.ToTable("ImpresionCupon");
            });
            modelBuilder.Entity<HistorialImpresionCupon>(entity => {
                entity.HasKey(x => x.HistorialImpresionCuponId);
                entity.Property(x => x.HistorialImpresionCuponId).ValueGeneratedOnAdd();
                entity.ToTable("HistorialImpresionCupon");
            });
        }
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
