using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Data;
using FastTrack.Models;

namespace FastTrack.ViewModels;

public partial class CustomProtocolViewModel : ObservableObject
{
    private readonly IFastingProtocolRepository _protocols;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SummaryText))]
    private string name = "My protocol";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SummaryText))]
    private double fastHours = 16;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SummaryText))]
    private double eatHours = 8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string SummaryText =>
        $"{(int)FastHours}h fast · {(int)EatHours}h eat";

    /// <summary>Set by the page after save so it can navigate back.</summary>
    public Func<Task>? OnSaved { get; set; }

    public CustomProtocolViewModel(IFastingProtocolRepository protocols)
    {
        _protocols = protocols;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        var nameTrim = (Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nameTrim))
        {
            ErrorMessage = "Please give your protocol a name.";
            return;
        }

        var fh = (int)Math.Round(FastHours);
        var eh = (int)Math.Round(EatHours);

        // Validation per spec US-01.2: fast > 0; total cycle ≤ 168.
        if (fh <= 0)
        {
            ErrorMessage = "Fast duration must be at least 1 hour.";
            return;
        }
        if (fh > 168)
        {
            ErrorMessage = "Fast duration cannot exceed 168 hours.";
            return;
        }
        if (eh < 0 || eh > 24)
        {
            ErrorMessage = "Eating window must be between 0 and 24 hours.";
            return;
        }
        if (fh + eh > 168)
        {
            ErrorMessage = "Total cycle (fast + eat) cannot exceed 168 hours.";
            return;
        }

        var protocol = new FastingProtocol
        {
            Id = Guid.NewGuid(),
            Name = nameTrim,
            FastHours = fh,
            EatHours = eh,
            Difficulty = ClassifyDifficulty(fh),
            IsCustom = true,
            IsPreset = false,
            Description = "Custom protocol.",
        };

        await _protocols.UpsertAsync(protocol);

        if (OnSaved is not null) await OnSaved();
    }

    private static Difficulty ClassifyDifficulty(int fastHours) => fastHours switch
    {
        <= 16 => Difficulty.Beginner,
        <= 20 => Difficulty.Intermediate,
        _ => Difficulty.Advanced,
    };
}
