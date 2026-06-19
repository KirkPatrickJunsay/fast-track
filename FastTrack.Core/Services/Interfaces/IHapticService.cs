namespace FastTrack.Services.Interfaces;

public enum HapticIntensity { Light, Medium, Heavy }

public interface IHapticService
{
    void Tick(HapticIntensity intensity = HapticIntensity.Light);
}
