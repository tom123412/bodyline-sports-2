namespace Api.BackgroundServices;

public class BackgroundServiceOptions
{
    public const string SectionName = nameof(BackgroundServiceOptions);
    
    /// <summary>
    /// Number of days before token expiration when we should refresh the token.
    /// Default is 30 days.
    /// </summary>
    public int TokenRefreshThresholdDays { get; set; } = 30;
}
