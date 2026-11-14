using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;
using Ganss.Xss;

namespace Borderland.Dreams.Mappers;

public class CloudInfoMapper(HtmlSanitizer htmlSanitizer)
{
    public CloudInfoViewModel ToViewModel(CloudInfo model)
    {
        return new CloudInfoViewModel
        {
            Content = htmlSanitizer.Sanitize(model.Content)
        };
    }

    public CloudInfo ToModel(CloudInfoViewModel vm)
    {
        return new CloudInfo
        {
            Content = htmlSanitizer.Sanitize(vm.Content)
        };
    }
}
