using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public static class CloudMapper
{
    public static CloudViewModel ToViewModel(this Cloud model)
    {
        if (model == null) return null!;
        return new CloudViewModel
        {
            ID = model.ID,
            Name = model.Name,
            CreatedDate = model.CreatedDate,
            Dreams = model.Dreams?.Select(d => d.ToViewModel()).ToList()
        };
    }

    public static Cloud ToModel(this CloudViewModel vm)
    {
        if (vm == null) return null!;
        return new Cloud
        {
            ID = vm.ID,
            Name = vm.Name,
            CreatedDate = vm.CreatedDate,
            Dreams = vm.Dreams?.Select(d => d.ToModel()).ToList()
        };
    }
}