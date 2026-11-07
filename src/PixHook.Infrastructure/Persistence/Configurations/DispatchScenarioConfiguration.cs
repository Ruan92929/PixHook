using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixHook.Domain.Entities;

namespace PixHook.Infrastructure.Persistence.Configurations;

internal sealed class DispatchScenarioConfiguration : IEntityTypeConfiguration<DispatchScenario>
{
    public void Configure(EntityTypeBuilder<DispatchScenario> b)
    {
        b.ToTable("Scenarios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("IX_Scenarios_Slug");

        b.HasMany(x => x.Steps)
            .WithOne(s => s.Scenario)
            .HasForeignKey(s => s.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Navigation(x => x.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
