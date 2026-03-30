using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Interfaces;

public interface ITagService
{
    Task<List<TagViewModel>> GetAll();
    Task<TagViewModel?> GetById(int id);
    Task Create(TagViewModel viewModel);
    Task<bool> Update(TagViewModel viewModel);
    Task<bool> Delete(int id);
}