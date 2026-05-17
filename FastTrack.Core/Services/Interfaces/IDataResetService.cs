namespace FastTrack.Services.Interfaces;

public interface IDataResetService
{
    /// <summary>Wipes all user data and re-seeds preset protocols. Onboarding will re-trigger on next launch.</summary>
    Task ResetAllAsync();
}
