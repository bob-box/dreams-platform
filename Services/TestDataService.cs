using System.Security.Claims;
using System.Security.Cryptography;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Data;
using Borderland.Dreams.Enums;
using Borderland.Dreams.Interfaces;
using Borderland.Dreams.Models;
using Borderland.Dreams.Utils;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Services;

public sealed class TestDataService(
    ApplicationDbContext context,
    UserManager<User> userManager,
    RoleManager<IdentityRole> roleManager,
    FundingRoundSchedulerService fundingRoundScheduler,
    IAppTime appTime)
{
    private static readonly string[] UserNames =
    [
        "Alex Garden",
        "Bailey Builder",
        "Casey Creator",
        "Drew Designer",
        "Elliot Organizer"
    ];

    public async Task<TestDataRunViewModel> CreateAsync(
        TestDataRunViewModel settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.FundingRoundStartInMinutes < 1 ||
            settings.FirstFundingRoundLengthInMinutes < 1 ||
            settings.BreakBetweenFundingRoundsInMinutes < 1 ||
            settings.SecondFundingRoundLengthInMinutes < 1 ||
            settings.AllocationPerUser <= 0 ||
            settings.CloudFundsAvailable <= 0)
        {
            throw new InvalidOperationException("Test data settings must define two valid funding rounds with a break and positive funding amounts.");
        }

        await EnsureMemberRoleAsync();

        var runId = Guid.NewGuid().ToString("N")[..8];
        var now = appTime.UtcNow;
        var cloudName = $"Playground {runId}";
        var cloud = new Cloud
        {
            Name = cloudName,
            Slug = CloudSlugGenerator.Generate(cloudName),
            TotalFundsAvailable = settings.CloudFundsAvailable,
            TotalFundsRemaining = settings.CloudFundsAvailable,
            TotalFundsSpent = 0m,
            DreamCreatePeriodStart = now.AddMinutes(-1),
            DreamCreatePeriodEnd = now.AddDays(30),
            DreamEditDeadline = now.AddDays(31)
        };

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        context.Cloud.Add(cloud);
        await context.SaveChangesAsync(cancellationToken);

        var password = "password1";
        var users = new List<User>();

        for (var index = 0; index < UserNames.Length; index++)
        {
            var email = $"test.{runId}.{index + 1}@example.test";
            var user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = UserNames[index]
            };

            EnsureIdentitySuccess(await userManager.CreateAsync(user, password), "create test user");
            EnsureIdentitySuccess(await userManager.AddToRoleAsync(user, Constants.MemberRole), "assign test user role");
            EnsureIdentitySuccess(
                await userManager.AddClaimAsync(user, new Claim(Constants.JOINED_CLOUD_CLAIM, cloud.ID.ToString())),
                "add test user to cloud");

            users.Add(user);
            context.Wallet.Add(new Wallet { UserID = user.Id, CloudID = cloud.ID });
        }

        var tags = new[]
        {
            new Tag { CloudID = cloud.ID, Title = "Community" },
            new Tag { CloudID = cloud.ID, Title = "Creative" },
            new Tag { CloudID = cloud.ID, Title = "Outdoors" }
        };
        context.Tag.AddRange(tags);

        var dreams = CreateDreams(cloud.ID, cloudName, users, tags);
        context.Dream.AddRange(dreams);

        var firstRound = new FundingRound
        {
            CloudID = cloud.ID,
            Title = $"Playground round 1 {runId}",
            StartDate = now.AddMinutes(settings.FundingRoundStartInMinutes),
            EndDate = now.AddMinutes(settings.FundingRoundStartInMinutes + settings.FirstFundingRoundLengthInMinutes),
            AllocationPerUser = settings.AllocationPerUser,
            MaxPercentagePerDream = 50m
        };
        var secondRound = new FundingRound
        {
            CloudID = cloud.ID,
            Title = $"Playground round 2 {runId}",
            StartDate = firstRound.EndDate.AddMinutes(settings.BreakBetweenFundingRoundsInMinutes),
            EndDate = firstRound.EndDate
                .AddMinutes(settings.BreakBetweenFundingRoundsInMinutes + settings.SecondFundingRoundLengthInMinutes),
            AllocationPerUser = settings.AllocationPerUser,
            MaxPercentagePerDream = 50m
        };
        context.FundingRound.AddRange(firstRound, secondRound);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        fundingRoundScheduler.ScheduleFundingRoundJobs(firstRound);
        fundingRoundScheduler.ScheduleFundingRoundJobs(secondRound);
        await context.SaveChangesAsync(cancellationToken);

        return new TestDataRunViewModel
        {
            CloudName = cloud.Name,
            CloudSlug = cloud.Slug,
            FundingRoundStartInMinutes = settings.FundingRoundStartInMinutes,
            FirstFundingRoundLengthInMinutes = settings.FirstFundingRoundLengthInMinutes,
            BreakBetweenFundingRoundsInMinutes = settings.BreakBetweenFundingRoundsInMinutes,
            SecondFundingRoundLengthInMinutes = settings.SecondFundingRoundLengthInMinutes,
            AllocationPerUser = settings.AllocationPerUser,
            CloudFundsAvailable = settings.CloudFundsAvailable,
            TemporaryPassword = password,
            FundingRounds =
            [
                new TestDataFundingRoundViewModel
                {
                    Title = firstRound.Title,
                    StartUtc = firstRound.StartDate,
                    EndUtc = firstRound.EndDate
                },
                new TestDataFundingRoundViewModel
                {
                    Title = secondRound.Title,
                    StartUtc = secondRound.StartDate,
                    EndUtc = secondRound.EndDate
                }
            ],
            Accounts = users.Select(user => new TestDataAccountViewModel
            {
                Name = user.Name,
                Email = user.Email!
            }).ToList()
        };
    }

    private async Task EnsureMemberRoleAsync()
    {
        if (await roleManager.RoleExistsAsync(Constants.MemberRole))
            return;

        EnsureIdentitySuccess(
            await roleManager.CreateAsync(new IdentityRole(Constants.MemberRole)),
            "create member role");
    }

    private static List<Dream> CreateDreams(int cloudId, string cloudName, IReadOnlyList<User> users, Tag[] tags)
    {
        return
        [
            new Dream
            {
                CloudID = cloudId,
                Name = $"{cloudName} Community Garden",
                Description = "A shared garden with space for neighborhood food growing.",
                Status = DreamStatus.ReadyForFunding,
                Tags = [tags[0], tags[2]],
                Dreamers = [MainDreamer(users[0]), CoDreamer(users[1])],
                BudgetItems = [Request("Raised beds", 900m, 1400m), Existing("Garden tools", 0m)]
            },
            new Dream
            {
                CloudID = cloudId,
                Name = $"{cloudName} Community Kitchen",
                Description = "A practical kitchen where neighbors can cook and learn together.",
                Status = DreamStatus.ReadyForFunding,
                Tags = [tags[0]],
                Dreamers = [MainDreamer(users[1])],
                BudgetItems = [Request("Cooking equipment", 1200m, 1800m), Existing("Kitchen tables", 250m)]
            },
            new Dream
            {
                CloudID = cloudId,
                Name = $"{cloudName} Art Studio",
                Description = "A welcoming space for workshops, painting, and making.",
                Status = DreamStatus.Active,
                Tags = [tags[1], tags[0]],
                Dreamers = [MainDreamer(users[2]), CoDreamer(users[3])],
                BudgetItems = [Request("Work tables", 700m, 1100m), Existing("Art supplies", 150m)]
            },
            new Dream
            {
                CloudID = cloudId,
                Name = $"{cloudName} Bike Workshop",
                Description = "Tools and a safe work area for community bike repair sessions.",
                Status = DreamStatus.ReadyForFunding,
                Tags = [tags[1]],
                Dreamers = [MainDreamer(users[3])],
                BudgetItems = [Request("Repair stands", 1000m, 1600m), Existing("Hand tools", 0m)]
            },
            new Dream
            {
                CloudID = cloudId,
                Name = $"{cloudName} Youth Hub",
                Description = "A flexible after-school room for young people to meet and create.",
                Status = DreamStatus.Draft,
                Tags = [tags[0], tags[1]],
                Dreamers = [MainDreamer(users[4]), CoDreamer(users[0])],
                BudgetItems = [Request("Seating and storage", 600m, 1000m), Existing("Board games", 0m)]
            }
        ];
    }

    private static Dreamer MainDreamer(User user) => new() { UserID = user.Id, IsMainDreamer = true };
    private static Dreamer CoDreamer(User user) => new() { UserID = user.Id, IsMainDreamer = false };

    private static BudgetItem Request(string name, decimal amount, decimal? maxAmount) => new()
    {
        Name = name,
        Type = BudgetItemType.Requesting,
        MinAmount = amount,
        MaxAmount = maxAmount
    };

    private static BudgetItem Existing(string name, decimal amount) => new()
    {
        Name = name,
        Type = BudgetItemType.Existing,
        MinAmount = amount
    };

    private static void EnsureIdentitySuccess(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not {operation}: {errors}");
        }
    }
}