using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PixHook.Domain.Entities;

namespace PixHook.Infrastructure.Persistence.Configurations;

internal sealed class WebhookDispatchConfiguration : IEntityTypeConfiguration<WebhookDispatch>
{
    public void Configure(EntityTypeBuilder<WebhookDispatch> b)
    {
        b.ToTable("Dispatches");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.DestinationUrl).HasMaxLength(2048).IsRequired();
        b.Property(x => x.RenderedPayload).IsRequired();
        b.Property(x => x.RenderedHeaders).IsRequired();

        // SQLite não ordena DateTimeOffset nativamente: armazenamos como long (binário)
        // para permitir ORDER BY / filtros no índice de RequestedAt.
        b.Property(x => x.RequestedAt)
            .HasConversion(new DateTimeOffsetToBinaryConverter())
            .IsRequired();

        b.Property(x => x.ResponseBody);
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);
        b.Ignore(x => x.IsReplay);

        b.HasOne(x => x.Template)
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ReplayOf)
            .WithMany()
            .HasForeignKey(x => x.ReplayOfDispatchId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.TemplateId).HasDatabaseName("IX_Dispatches_TemplateId");
        b.HasIndex(x => x.RequestedAt).HasDatabaseName("IX_Dispatches_RequestedAt");
        b.HasIndex(x => x.ReplayOfDispatchId).HasDatabaseName("IX_Dispatches_ReplayOfDispatchId");
    }
}
