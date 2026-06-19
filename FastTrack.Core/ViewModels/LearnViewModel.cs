using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class LearnViewModel : ObservableObject
{
    private readonly IArticleCatalog _articles;
    private readonly IStageCalculator _stages;
    private readonly INavigationService _navigation;
    private bool _loaded;

    public ObservableCollection<ArticleSummaryViewModel> Articles { get; } = new();
    public ObservableCollection<StageRowViewModel> Stages { get; } = new();

    [ObservableProperty] private string headline = "Learn";
    [ObservableProperty] private string subhead = "Short, calm explainers — read at your own pace.";

    public LearnViewModel(IArticleCatalog articles, IStageCalculator stages, INavigationService navigation)
    {
        _articles = articles;
        _stages = stages;
        _navigation = navigation;
    }

    public void Load()
    {
        // Idempotent — the catalog is static, so once loaded we don't repopulate.
        if (_loaded) return;
        _loaded = true;

        foreach (var a in _articles.All())
        {
            Articles.Add(new ArticleSummaryViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Summary = a.Summary,
                HeroAsset = a.HeroAsset,
                ReadingTimeDisplay = a.ReadingMinutes <= 1 ? "1 min read" : $"{a.ReadingMinutes} min read",
            });
        }

        foreach (var s in _stages.Stages)
        {
            Stages.Add(new StageRowViewModel
            {
                Key = s.Key,
                Name = s.Name,
                Range = s.EndHour.HasValue ? $"{s.StartHour:0}–{s.EndHour:0}h" : $"{s.StartHour:0}h+",
                Summary = s.Summary,
                IconAsset = s.IconAsset,
                Opacity = 1.0,
            });
        }
    }

    [RelayCommand]
    private async Task OpenArticleAsync(ArticleSummaryViewModel? row)
    {
        if (row is null) return;
        await _navigation.GoToAsync($"ArticleDetailPage?articleId={row.Id}");
    }

    [RelayCommand]
    private async Task OpenStageAsync(StageRowViewModel? row)
    {
        if (row is null) return;
        await _navigation.GoToAsync($"StageDetailPage?stageKey={row.Key}");
    }
}
