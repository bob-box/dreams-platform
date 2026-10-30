using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.ViewModels;

namespace Borderland.Dreams.Mappers;

public class CloudMapper(IMapperFacade mapper, IAppTime appTime)
{
    public CloudViewModel ToViewModel(Cloud model)
    {
        return new CloudViewModel
        {
            ID = model.ID,
            Name = model.Name,
            Slug = model.Slug,
            CreatedAt = appTime.ToLocal(model.CreatedAt),
            TotalFundsAvailable = model.TotalFundsAvailable,
            TotalFundsSpent = model.TotalFundsSpent,
            DreamCreatePeriodStart = appTime.ToLocal(model.DreamCreatePeriodStart),
            DreamCreatePeriodEnd = appTime.ToLocal(model.DreamCreatePeriodEnd),
            DreamEditDeadline = appTime.ToLocal(model.DreamEditDeadline),
            SendDreamWelcomeEmail = model.SendDreamWelcomeEmail,
            DreamWelcomeEmailSubject = model.DreamWelcomeEmailSubject,
            DreamWelcomeEmailBody = model.DreamWelcomeEmailBody,
            Dreams = mapper.ToViewModels(model.Dreams),
            FundingRounds = mapper.ToViewModels(model.FundingRounds)
        };
    }

    public Cloud ToModel(CloudViewModel vm)
    {
        return new Cloud
        {
            ID = vm.ID,
            Name = vm.Name,
            Slug = vm.Slug,
            TotalFundsAvailable = vm.TotalFundsAvailable,
            TotalFundsSpent = vm.TotalFundsSpent,
            DreamCreatePeriodStart = appTime.ToUtc(vm.DreamCreatePeriodStart),
            DreamCreatePeriodEnd = appTime.ToUtc(vm.DreamCreatePeriodEnd),
            DreamEditDeadline = appTime.ToUtc(vm.DreamEditDeadline),
            SendDreamWelcomeEmail = vm.SendDreamWelcomeEmail,
            DreamWelcomeEmailSubject = vm.DreamWelcomeEmailSubject,
            DreamWelcomeEmailBody = vm.DreamWelcomeEmailBody,
            Dreams = mapper.ToModels(vm.Dreams),
            FundingRounds  = mapper.ToModels(vm.FundingRounds)
        };
    }
}