using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data
{
    public class StoryConfiguration : IEntityTypeConfiguration<Story>
    {
        public void Configure(EntityTypeBuilder<Story> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
            builder.Property(s => s.ChildName).HasMaxLength(100).IsRequired();
            builder.Property(s => s.Character).HasMaxLength(100).IsRequired();
            builder.Property(s => s.Theme).HasMaxLength(100).IsRequired();

            builder.HasMany(s => s.Pages)
                   .WithOne(p => p.Story)
                   .HasForeignKey(p => p.StoryId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class StoryPageConfiguration : IEntityTypeConfiguration<StoryPage>
    {
        public void Configure(EntityTypeBuilder<StoryPage> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Sentence).HasMaxLength(500).IsRequired();
            builder.Property(p => p.ImagePrompt).HasMaxLength(1000).IsRequired();
            builder.Property(p => p.ImagePath).HasMaxLength(500).IsRequired();
        }
    }

}
