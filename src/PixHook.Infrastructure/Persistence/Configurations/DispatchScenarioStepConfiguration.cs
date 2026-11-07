using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixHook.Domain.Entities;

namespace PixHook.Infrastructure.Persistence.Configurations;

internal sealed class DispatchScenarioStepConfiguration : IEntityTypeConfiguration<DispatchScenarioStep>
{
    public void Configure(EntityTypeBuilder<DispatchScenarioStep> b)
    {
        b.ToTable("ScenarioSteps");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Order).IsRequired();
        b.Property(x => x.DelayBeforeMs).IsRequired();
        b.Property(x => x.Note).HasMaxLength(300).IsRequired();

        b.HasOne(x => x.Template)
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.ScenarioId).HasDatabaseName("IX_ScenarioSteps_ScenarioId");
        b.HasIndex(x => x.TemplateId).HasDatabaseName("IX_ScenarioSteps_TemplateId");
    }
}
