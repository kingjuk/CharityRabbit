namespace CharityRabbit.Platform;

public enum ShareOutcome
{
    /// <summary>Handed off to a native share sheet.</summary>
    Shared,
    /// <summary>No share sheet available; the link was copied to the clipboard instead.</summary>
    Copied,
    Failed,
}

/// <summary>
/// Sharing a link. Web implementation uses the navigator.share/clipboard JS helper;
/// the MAUI implementation uses Share.RequestAsync.
/// </summary>
public interface IShareProvider
{
    Task<ShareOutcome> ShareAsync(string title, string text, string url);
}
