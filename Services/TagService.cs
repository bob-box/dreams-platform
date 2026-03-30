using Borderland.Dreams.Data;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Mappers;
using Borderland.Dreams.Middleware;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Borderland.Dreams.Services;

public class TagService(
    ApplicationDbContext context,
    ICloudNameResolver cloudNameResolver,
    IMemoryCache memoryCache) : ITagService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private string TagsCacheKey => $"tags:cloud:{cloudNameResolver.CloudID}";

    public async Task<List<TagViewModel>> GetAll()
    {
        if (memoryCache.TryGetValue(TagsCacheKey, out List<TagViewModel>? cachedTags) && cachedTags != null)
            return cachedTags;

        var tags = await context.Tag
            .Where(t => t.CloudID == cloudNameResolver.CloudID)
            .OrderBy(t => t.Title)
            .Select(t => t.ToViewModel())
            .ToListAsync();

        memoryCache.Set(TagsCacheKey, tags, CacheDuration);
        return tags;
    }

    public async Task<TagViewModel?> GetById(int id)
    {
        var tag = await GetTagInCloud(id);
        return tag?.ToViewModel();
    }

    public async Task Create(TagViewModel viewModel)
    {
        var tag = new Tag
        {
            Title = viewModel.Title,
            CloudID = cloudNameResolver.CloudID
        };

        context.Tag.Add(tag);
        await context.SaveChangesAsync();
        InvalidateGetAllCache();
    }

    public async Task<bool> Update(TagViewModel viewModel)
    {
        var tag = await GetTagInCloud(viewModel.ID);
        if (tag == null)
            return false;

        tag.Title = viewModel.Title;
        await context.SaveChangesAsync();
        InvalidateGetAllCache();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var tag = await GetTagInCloud(id);
        if (tag == null)
            return false;

        context.Tag.Remove(tag);
        await context.SaveChangesAsync();
        InvalidateGetAllCache();
        return true;
    }

    private Task<Tag?> GetTagInCloud(int id)
    {
        return context.Tag
            .FirstOrDefaultAsync(t => t.ID == id && t.CloudID == cloudNameResolver.CloudID);
    }


    private void InvalidateGetAllCache()
    {
        memoryCache.Remove(TagsCacheKey);
    }
}