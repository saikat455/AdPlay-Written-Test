using System.Text.RegularExpressions;

namespace AdPlay.Api.Middleware;

public static class BodyRedactor
{
    // any JSON key containing one of these words gets its value replaced.
    // Works on cut-off JSON too, since we only keep the first few KB.
    private static readonly Regex Sensitive = new(
        "\"([^\"]*(?:password|passwd|token|secret|cvv|cvc|card|authorization|apikey|otp)[^\"]*)\"\\s*:\\s*(?:\"(?:[^\"\\\\]|\\\\.)*\"?|[^,}\\]\\s]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public static string Redact(string body)
    {
        try
        {
            return Sensitive.Replace(body, "\"$1\":\"***\"");
        }
        catch (RegexMatchTimeoutException)
        {
            return "[redaction failed, body not logged]";   // never log it unredacted
        }
    }
}
