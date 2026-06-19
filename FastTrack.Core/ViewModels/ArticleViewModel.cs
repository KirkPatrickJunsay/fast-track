using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class ArticleViewModel : ObservableObject
{
    private readonly IArticleCatalog _articles;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string heroAsset = string.Empty;
    [ObservableProperty] private string readingTimeDisplay = string.Empty;
    [ObservableProperty] private bool isLoaded;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    public ObservableCollection<ArticleSectionViewModel> Sections { get; } = new();

    public ArticleViewModel(IArticleCatalog articles, INavigationService navigation)
    {
        _articles = articles;
        _navigation = navigation;
    }

    public void LoadFromId(string? id)
    {
        Sections.Clear();
        var article = string.IsNullOrWhiteSpace(id) ? null : _articles.GetById(id);
        if (article is null)
        {
            IsLoaded = false;
            HasError = true;
            ErrorMessage = "Article not found.";
            return;
        }

        Title = article.Title;
        Summary = article.Summary;
        HeroAsset = article.HeroAsset;
        ReadingTimeDisplay = article.ReadingMinutes <= 1 ? "1 min read" : $"{article.ReadingMinutes} min read";

        foreach (var s in article.Sections)
        {
            Sections.Add(new ArticleSectionViewModel
            {
                Heading = s.Heading ?? string.Empty,
                HasHeading = !string.IsNullOrWhiteSpace(s.Heading),
                Body = s.Body,
            });
        }

        IsLoaded = true;
        HasError = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private Task BackAsync() => _navigation.GoBackAsync();
}

public partial class ArticleSectionViewModel : ObservableObject
{
    public string Heading { get; init; } = string.Empty;
    public bool HasHeading { get; init; }
    public string Body { get; init; } = string.Empty;
}
