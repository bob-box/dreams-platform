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
            CreatedAt = appTime.ToLocal(model.CreatedAtUtc),
            TotalFundsAvailable = model.TotalFundsAvailable,
            TotalFundsSpent = model.TotalFundsSpent,
            DreamCreatePeriodStart = appTime.ToLocal(model.DreamCreatePeriodStartUtc),
            DreamCreatePeriodEnd = appTime.ToLocal(model.DreamCreatePeriodEndUtc),
            DreamEditDeadline = appTime.ToLocal(model.DreamEditDeadlineUtc),
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
            DreamCreatePeriodStartUtc = appTime.ToUtc(vm.DreamCreatePeriodStart),
            DreamCreatePeriodEndUtc = appTime.ToUtc(vm.DreamCreatePeriodEnd),
            DreamEditDeadlineUtc = appTime.ToUtc(vm.DreamEditDeadline),
            SendDreamWelcomeEmail = vm.SendDreamWelcomeEmail,
            DreamWelcomeEmailSubject = vm.DreamWelcomeEmailSubject,
            DreamWelcomeEmailBody = vm.DreamWelcomeEmailBody,
            Dreams = mapper.ToModels(vm.Dreams),
            FundingRounds  = mapper.ToModels(vm.FundingRounds)
        };
    }
}