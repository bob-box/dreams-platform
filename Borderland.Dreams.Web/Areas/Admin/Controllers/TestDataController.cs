using System.Text.Json;
using Borderland.Dreams.Authorization;
using Borderland.Dreams.Services;
using Borderland.Dreams.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Borderland.Dreams.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.AdminRole)]
public class TestDataController(
    TestDataService testDataService,
    IWebHostEnvironment environment) : Controller
{
    private const string RunResultKey = "TestDataRunResult";

    public IActionResult Index()
    {
        if (!IsEnabled)
            return NotFound();

        var result = TempData[RunResultKey] as string;
        var viewModel = result == null
            ? null
            : JsonSerializer.Deserialize<TestDataRunViewModel>(result);

        viewModel ??= new TestDataRunViewModel();

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(nameof(TestDataRunViewModel.FundingRoundStartInMinutes),
            nameof(TestDataRunViewModel.FirstFundingRoundLengthInMinutes),
            nameof(TestDataRunViewModel.BreakBetweenFundingRoundsInMinutes),
            nameof(TestDataRunViewModel.SecondFundingRoundLengthInMinutes),
            nameof(TestDataRunViewModel.AllocationPerUser),
            nameof(TestDataRunViewModel.CloudFundsAvailable))]
        TestDataRunViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
            return NotFound();

        if (!ModelState.IsValid)
            return View("Index", viewModel);

        try
        {
            var result = await testDataService.CreateAsync(viewModel, cancellationToken);
            TempData[RunResultKey] = JsonSerializer.Serialize(result);
        }
        catch (Exception exception) when (exception is InvalidOperationException or DbUpdateException)
        {
            viewModel.ErrorMessage = exception.Message;
            return View("Index", viewModel);
        }

        return RedirectToAction(nameof(Index));
    }

    private bool IsEnabled => environment.IsDevelopment() || environment.IsEnvironment("Testing");
}