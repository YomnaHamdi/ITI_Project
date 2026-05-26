using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    public class StoryRepository(AppDbContext db) : IStoryRepository
    {
        public async Task SaveAsync(Story story)
        {
            db.Stories.Add(story);
            await db.SaveChangesAsync();
        }

        public async Task<Story?> GetByIdAsync(Guid id)
        {
            return await db.Stories
                .Include(s => s.Pages)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
    }

}
