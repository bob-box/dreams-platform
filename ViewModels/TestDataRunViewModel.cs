using System.ComponentModel.DataAnnotations;

namespace Borderland.Dreams.ViewModels;

public sealed class TestDataRunViewModel : IValidatableObject
{
    public string? CloudName { get; set; }
    public string? CloudSlug { get; set; }

    [Range(1, 1440, ErrorMessage = "Round 1 must start at least one minute from now.")]
    public int FundingRoundStartInMinutes { get; set; } = 1;

    [Range(1, 1440)]
    public int FirstFundingRoundLengthInMinutes { get; set; } = 9;

    [Range(1, 1440, ErrorMessage = "The break between rounds must be at least one minute.")]
    public int BreakBetweenFundingRoundsInMinutes { get; set; } = 5;

    [Range(1, 10080)]
    public int SecondFundingRoundLengthInMinutes { get; set; } = 15;

    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "Allocation per user must be between 0 and 10,000,000.")]
    public decimal AllocationPerUser { get; set; } = 400m;

    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "Cloud funds must be between 0 and 10,000,000.")]
    public decimal CloudFundsAvailable { get; set; } = 10000m;

    public string? TemporaryPassword { get; set; }
    public List<TestDataAccountViewModel> Accounts { get; set; } = [];
    public List<TestDataFundingRoundViewModel> FundingRounds { get; set; } = [];
    public string? ErrorMessage { get; set; }
    public bool HasRun => !string.IsNullOrEmpty(CloudSlug);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AllocationPerUser <= -1 || AllocationPerUser > 10_000_000m)
        {
            yield return new ValidationResult(
                "Allocation per user must be between 0 and 10,000,000.",
                [nameof(AllocationPerUser)]);
        }

        if (CloudFundsAvailable <= -1 || CloudFundsAvailable > 10_000_000m)
        {
            yield return new ValidationResult(
                "Cloud funds must be between 0 and 10,000,000.",
                [nameof(CloudFundsAvailable)]);
        }

    }
}

public sealed class TestDataAccountViewModel
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public sealed class TestDataFundingRoundViewModel
{
    public string Title { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}