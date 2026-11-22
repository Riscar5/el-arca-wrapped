using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Jellyfin.Plugin.ElArcaWrapped.Export;
using Jellyfin.Plugin.ElArcaWrapped.Services;
using Jellyfin.Plugin.ElArcaWrapped.Statistics;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ElArcaWrapped.Api;

/// <summary>Admin-only endpoints for previewing and exporting a user's annual recap.</summary>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("ElArcaWrapped")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ElArcaWrappedController : ControllerBase
{
    private readonly PlaybackSessionStore _store;
    private readonly IUserManager _userManager;
    private readonly StandaloneHtmlRenderer _renderer;

    public ElArcaWrappedController(PlaybackSessionStore store, IUserManager userManager, StandaloneHtmlRenderer renderer)
    {
        _store = store;
        _userManager = userManager;
        _renderer = renderer;
    }

    /// <summary>Returns selectable Jellyfin users (admin-only endpoint).</summary>
    [HttpGet("Users")]
    public ActionResult<IReadOnlyList<WrappedUserOption>> GetUsers()
    {
        return _userManager.GetUsers()
            .OrderBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
            .Select(user => new WrappedUserOption(user.Id, user.Username ?? string.Empty))
            .ToArray();
    }

    /// <summary>Returns the calculated statistics for the selected user and year.</summary>
    [HttpGet("Preview")]
    public async Task<ActionResult<WrappedReportData>> PreviewAsync([FromQuery, Required] Guid userId, [FromQuery, Range(1900, 9999)] int year)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null) return NotFound();
        var records = await _store.GetRecordsAsync(userId.ToString("N"), year).ConfigureAwait(false);
        var stats = WrappedStatisticsCalculator.Calculate(records, userId.ToString("N"), year);
        return WrappedReportDataFactory.Create(stats, user.Username ?? "Usuario", DateTimeOffset.UtcNow);
    }

    /// <summary>Downloads a self-contained HTML recap for the selected user and year.</summary>
    [HttpGet("Export")]
    [Produces(MediaTypeNames.Text.Html)]
    public async Task<IActionResult> ExportAsync([FromQuery, Required] Guid userId, [FromQuery, Range(1900, 9999)] int year)
    {
        var report = await PreviewReportAsync(userId, year).ConfigureAwait(false);
        if (report is null) return NotFound();
        var html = _renderer.Render(report);
        var filename = $"El-Arca-Wrapped-{year}-{SafeFilename(report.DisplayName)}.html";
        return File(System.Text.Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8", filename);
    }

    private async Task<WrappedReportData?> PreviewReportAsync(Guid userId, int year)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null) return null;
        var records = await _store.GetRecordsAsync(userId.ToString("N"), year).ConfigureAwait(false);
        var stats = WrappedStatisticsCalculator.Calculate(records, userId.ToString("N"), year);
        return WrappedReportDataFactory.Create(stats, user.Username ?? "Usuario", DateTimeOffset.UtcNow);
    }

    private static string SafeFilename(string name)
    {
        var safe = string.Concat(name.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-'));
        return string.IsNullOrWhiteSpace(safe) ? "usuario" : safe.Trim('-');
    }
}

/// <summary>Small user selector DTO that does not expose unrelated user properties.</summary>
public sealed record WrappedUserOption(Guid Id, string Name);
