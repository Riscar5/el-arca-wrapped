using System.Globalization;
using System.Net;
using System.Text;
using Jellyfin.Plugin.ElArcaWrapped.Statistics;

namespace Jellyfin.Plugin.ElArcaWrapped.Export;

/// <summary>Renders a responsive, self-contained annual recap without external assets.</summary>
public sealed class StandaloneHtmlRenderer
{
    public string Render(WrappedReportData report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var stats = report.Statistics;
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><meta name=\"theme-color\" content=\"#101014\"><title>El Arca Wrapped ");
        sb.Append(report.Year.ToString(CultureInfo.InvariantCulture));
        sb.Append("</title><style>");
        sb.Append(Css);
        sb.Append("</style></head><body><main class=\"wrap\"><header><div class=\"brand\">EL ARCA <span>WRAPPED</span></div><small>RESUMEN ANUAL</small></header><section class=\"hero\"><p class=\"eyebrow\">Tu año en Jellyfin</p><div class=\"year\">");
        sb.Append(report.Year.ToString(CultureInfo.InvariantCulture));
        sb.Append("</div><h1>Un año de<br><em>historias.</em></h1><p class=\"intro\">Este es el resumen de visionado de <strong>");
        sb.Append(E(report.DisplayName));
        sb.Append("</strong>.</p></section><section class=\"stats\">");
        Stat(sb, FormatHours(stats.TotalMinutesWatched), "horas de visionado");
        Stat(sb, stats.TotalPlays.ToString("N0", CultureInfo.InvariantCulture), "reproducciones");
        Stat(sb, stats.ActiveDays.ToString("N0", CultureInfo.InvariantCulture), "días activos");
        Stat(sb, stats.UniqueMovies.ToString("N0", CultureInfo.InvariantCulture), "películas distintas");
        sb.Append("</section><section class=\"story\"><p class=\"kicker\">01 / Tu ritmo</p><h2>Así transcurrió tu año.</h2><div class=\"chart\"><div class=\"bars\">");
        var maxMonth = Math.Max(1, stats.MonthlyActivity.Max(m => m.MinutesWatched));
        foreach (var month in stats.MonthlyActivity.OrderBy(m => m.Month))
        {
            var height = Math.Clamp(month.MinutesWatched / maxMonth * 100, 0, 100).ToString("0.0", CultureInfo.InvariantCulture);
            sb.Append("<div class=\"barwrap\"><div class=\"bar\" style=\"height:");
            sb.Append(height);
            sb.Append("%\" title=\"");
            sb.Append(E(CultureInfo.InvariantCulture, "{0}: {1} min", MonthName(month.Month), Math.Round(month.MinutesWatched)));
            sb.Append("\"></div><small>");
            sb.Append(MonthShort(month.Month));
            sb.Append("</small></div>");
        }
        sb.Append("</div></div><p class=\"muted\">Meses con actividad registrada: <strong>");
        sb.Append(stats.MonthsWithActivity.ToString(CultureInfo.InvariantCulture));
        sb.Append(" de 12</strong>. El periodo de captura puede ser incompleto.</p></section>");
        RenderRank(sb, "02 / Tus películas", "Películas que más tiempo ocuparon tu pantalla", stats.TopMovies);
        RenderRank(sb, "03 / Tus series", "Series con más tiempo de visionado registrado", stats.TopSeries);
        sb.Append("<section class=\"story\"><p class=\"kicker\">04 / Tus marcas</p><h2>Algunos datos que destacan.</h2><div class=\"tiles\">");
        Tile(sb, FormatMinutes(stats.LongestPlayMinutes), "Tu reproducción más larga", stats.LongestPlayItemName);
        Tile(sb, stats.LongestStreakDays.ToString(CultureInfo.InvariantCulture) + " días", "Tu racha más larga", null);
        Tile(sb, stats.CompletionRatePercent.ToString("0", CultureInfo.InvariantCulture) + "%", "Reproducciones completadas", null);
        Tile(sb, FormatMinutes(stats.LongestViewingDayMinutes), "Tu día de mayor visionado", null);
        sb.Append("</div></section><section class=\"story\"><p class=\"kicker\">05 / Cobertura</p><h2>El historial disponible.</h2><p class=\"muted\">");
        if (stats.CoverageStartUtc is { } start && stats.CoverageEndUtc is { } end)
        {
            sb.Append("Actividad registrada desde <strong>"); sb.Append(start.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)); sb.Append("</strong> hasta <strong>"); sb.Append(end.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)); sb.Append("</strong> (fechas UTC). No implica que exista captura continua durante todo el periodo.");
        }
        else sb.Append("No hay actividad registrada para este año.");
        sb.Append("</p></section>");
        RenderInsightBars(sb, "06 / Tus géneros", "Los géneros que más te acompañaron", stats.TopGenres.Select(item => (item.Genre, item.MinutesWatched, item.Plays)));
        RenderInsightBars(sb, "07 / Décadas", "De qué décadas fueron tus películas", stats.MovieDecades.Select(item => ($"{item.Decade}s", item.MinutesWatched, item.Plays)));
        RenderInsightBars(sb, "08 / Tu horario UTC", "En qué horas UTC empezaste más reproducciones", stats.HourlyActivityUtc.Select(item => ($"{item.HourUtc:00}:00", item.MinutesWatched, item.Plays)).Where(item => item.Plays > 0).OrderByDescending(item => item.Plays).Take(8));
        RenderInsightBars(sb, "09 / Tus días UTC", "Los días de la semana con más visionado (UTC)", stats.WeekdayActivityUtc.Select(item => (DayName(item.Day), item.MinutesWatched, item.Plays)));
        sb.Append("<p class=\"notice\">"); sb.Append(E(report.CoverageNote)); sb.Append(" Las agrupaciones por mes, hora y día se calculan en UTC."); sb.Append("</p></section><footer>El Arca Wrapped · Informe individual · Datos calculados localmente en Jellyfin</footer></main></body></html>");
        return sb.ToString();
    }

    private static void RenderRank(StringBuilder sb, string kicker, string heading, IReadOnlyList<RankedItem> items)
    {
        sb.Append("<section class=\"story\"><p class=\"kicker\">"); sb.Append(E(kicker)); sb.Append("</p><h2>"); sb.Append(E(heading)); sb.Append("</h2><div class=\"rank\">");
        if (items.Count == 0) sb.Append("<p class=\"muted\">No hay datos suficientes.</p>");
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            sb.Append("<article class=\"rankrow\">");
            if (!string.IsNullOrWhiteSpace(item.ArtworkDataUri))
            {
                sb.Append("<img class=\"poster\" src=\""); sb.Append(E(item.ArtworkDataUri)); sb.Append("\" alt=\""); sb.Append(E(item.Name)); sb.Append("\" loading=\"lazy\">");
            }
            else sb.Append("<span class=\"poster-placeholder\" aria-hidden=\"true\"></span>");
            sb.Append("<span class=\"num\">"); sb.Append((i + 1).ToString("00", CultureInfo.InvariantCulture)); sb.Append("</span><div><strong>"); sb.Append(E(item.Name)); sb.Append("</strong><small>"); sb.Append(item.Plays.ToString(CultureInfo.InvariantCulture)); sb.Append(" reproducciones</small></div><b class=\"mins\">"); sb.Append(FormatMinutes(item.MinutesWatched)); sb.Append("</b></article>");
        }
        sb.Append("</div></section>");
    }

    private static void RenderInsightBars(StringBuilder sb, string kicker, string heading, IEnumerable<(string Label, double Minutes, int Plays)> source)
    {
        var items = source.Where(item => item.Minutes > 0 || item.Plays > 0).ToArray();
        sb.Append("<section class=\"story\"><p class=\"kicker\">"); sb.Append(E(kicker)); sb.Append("</p><h2>"); sb.Append(E(heading)); sb.Append("</h2>");
        if (items.Length == 0)
        {
            sb.Append("<p class=\"muted\">No hay datos suficientes.</p></section>");
            return;
        }
        var max = Math.Max(1, items.Max(item => item.Minutes));
        sb.Append("<div class=\"insight-bars\">");
        foreach (var item in items)
        {
            var width = Math.Clamp(item.Minutes / max * 100, 0, 100).ToString("0.0", CultureInfo.InvariantCulture);
            sb.Append("<div class=\"insight-row\"><div class=\"insight-label\"><strong>"); sb.Append(E(item.Label)); sb.Append("</strong><small>"); sb.Append(item.Plays.ToString("N0", CultureInfo.InvariantCulture)); sb.Append(" reproducciones · "); sb.Append(FormatMinutes(item.Minutes)); sb.Append("</small></div><div class=\"insight-track\"><div class=\"insight-fill\" style=\"width:");
            sb.Append(width);
            sb.Append("%\"></div></div></div>");
        }
        sb.Append("</div></section>");
    }

    private static string DayName(DayOfWeek day) => CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetDayName(day);


    private static void Stat(StringBuilder sb, string value, string label)
    { sb.Append("<article class=\"stat\"><strong>"); sb.Append(E(value)); sb.Append("</strong><span>"); sb.Append(E(label)); sb.Append("</span></article>"); }
    private static void Tile(StringBuilder sb, string value, string label, string? detail)
    { sb.Append("<article class=\"tile\"><strong>"); sb.Append(E(value)); sb.Append("</strong><span>"); sb.Append(E(label)); if (!string.IsNullOrWhiteSpace(detail)) { sb.Append("<small>"); sb.Append(E(detail)); sb.Append("</small>"); } sb.Append("</span></article>"); }
    private static string E(string value) => WebUtility.HtmlEncode(value);
    private static string E(IFormatProvider provider, string format, params object[] args) => WebUtility.HtmlEncode(string.Format(provider, format, args));
    private static string FormatHours(double minutes) => (minutes / 60).ToString("N1", CultureInfo.InvariantCulture) + " h";
    private static string FormatMinutes(double minutes) => minutes < 60 ? Math.Round(minutes).ToString("N0", CultureInfo.InvariantCulture) + " min" : $"{(int)(minutes / 60)} h {Math.Round(minutes % 60):N0} min";
    private static string MonthName(int month) => CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetMonthName(month);
    private static string MonthShort(int month) => CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetAbbreviatedMonthName(month).ToUpperInvariant();

    private const string Css = """
      :root{color-scheme:dark;--bg:#101014;--panel:#1b1b23;--ink:#f7f4ed;--muted:#aaa7b5;--lime:#d5ff48;--pink:#ff5d9e;--line:#34343e}*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.5 system-ui,-apple-system,Segoe UI,sans-serif}.wrap{width:min(100% - 32px,960px);margin:auto}header{display:flex;justify-content:space-between;align-items:center;padding:22px 0;color:var(--muted);font-size:.75rem;letter-spacing:.1em}.brand{font-weight:900;color:var(--ink)}.brand span{color:var(--lime)}.hero{min-height:64vh;display:flex;flex-direction:column;justify-content:center;padding:42px 0}.eyebrow,.kicker{color:var(--pink);font-weight:800;letter-spacing:.15em;text-transform:uppercase;font-size:.78rem}.year{font-size:clamp(5rem,23vw,12rem);font-weight:950;line-height:.8;letter-spacing:-.1em;color:var(--lime);margin:20px 0}.hero h1{font-size:clamp(3rem,10vw,6.5rem);line-height:.9;letter-spacing:-.08em;margin:0}.hero em{color:var(--pink);font-style:normal}.intro,.muted{color:var(--muted);max-width:700px}.stats{display:grid;grid-template-columns:repeat(2,1fr);gap:10px;margin-bottom:55px}.stat,.tile{background:var(--panel);border-radius:16px;padding:20px;min-height:120px}.stat strong,.tile strong{display:block;color:var(--lime);font-size:clamp(1.8rem,7vw,3.4rem);line-height:1.1;overflow-wrap:anywhere}.stat span,.tile span{display:block;color:var(--muted);margin-top:10px;font-size:.88rem}.story{border-top:1px solid var(--line);padding:48px 0}.story h2{font-size:clamp(2rem,8vw,4.4rem);line-height:1;letter-spacing:-.06em;margin:12px 0 22px}.chart{background:var(--panel);padding:18px;border-radius:16px}.bars{display:flex;align-items:stretch;gap:4px;height:220px}.barwrap{flex:1;display:flex;flex-direction:column;justify-content:end;min-width:0;gap:7px}.bar{background:linear-gradient(180deg,var(--lime),#849d2d);min-height:2px;border-radius:5px 5px 0 0}.barwrap:nth-child(3n) .bar{background:linear-gradient(180deg,var(--pink),#9e3763)}.barwrap small{text-align:center;color:var(--muted);font-size:.6rem}.insight-bars{display:grid;gap:13px;background:var(--panel);border-radius:16px;padding:18px}.insight-row{display:grid;gap:5px}.insight-label{display:flex;justify-content:space-between;gap:12px;align-items:baseline}.insight-label strong{overflow-wrap:anywhere}.insight-label small{color:var(--muted);font-size:.75rem;text-align:right;white-space:nowrap}.insight-track{height:8px;background:#34343e;border-radius:99px;overflow:hidden}.insight-fill{height:100%;background:linear-gradient(90deg,var(--pink),var(--lime));border-radius:99px}.rank{display:grid;gap:9px}.rankrow{display:grid;grid-template-columns:52px 28px minmax(0,1fr) auto;gap:10px;align-items:center;background:var(--panel);padding:13px;border-radius:12px}.poster,.poster-placeholder{width:52px;height:72px;object-fit:cover;border-radius:7px;background:#34343e}.poster-placeholder{display:block}.num{font-weight:900;color:var(--lime)}.rankrow strong{display:block;overflow-wrap:anywhere}.rankrow small{display:block;color:var(--muted);font-size:.78rem}.mins{font-size:.8rem;white-space:nowrap}.tiles{display:grid;grid-template-columns:repeat(2,1fr);gap:10px}.tile strong{color:var(--blue,#73c9ff)}.tile small{display:block;font-size:.8rem;color:var(--muted);margin-top:5px}.notice{background:#282817;border:1px solid #55512c;border-radius:12px;padding:14px;color:#e9e6bd;font-size:.85rem}footer{padding:30px 0 45px;border-top:1px solid var(--line);color:var(--muted);font-size:.8rem}@media(min-width:700px){.stats{grid-template-columns:repeat(4,1fr)}.tiles{grid-template-columns:repeat(4,1fr)}.hero{min-height:78vh}.bars{height:260px}}
      """;
}
