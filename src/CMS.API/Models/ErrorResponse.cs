namespace CMS.API.Models;

/// <summary>
/// The single body shape returned for an unhandled server-side failure. Carries a generic message
/// only — never exception text, SQL, or connection details — plus a trace id that correlates the
/// user's report with the full exception in the server log.
/// </summary>
/// <param name="Message">A safe, user-facing message. Always <see cref="GenericMessage"/> in practice.</param>
/// <param name="TraceId">Correlation id, also stamped on the logged exception.</param>
public sealed record ErrorResponse(string Message, string TraceId)
{
    /// <summary>
    /// The only message a client ever sees for an unhandled failure. Bilingual to match the UI;
    /// the Angular error interceptor shows it verbatim.
    /// </summary>
    public const string GenericMessage = "系統發生錯誤，請稍後再試。An unexpected error occurred.";
}
