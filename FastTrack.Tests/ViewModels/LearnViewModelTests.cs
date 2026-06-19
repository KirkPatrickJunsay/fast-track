using FastTrack.Models;
using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class LearnViewModelTests
{
    private static (LearnViewModel Vm, Mock<INavigationService> Nav) Build()
    {
        var nav = new Mock<INavigationService>();
        return (new LearnViewModel(new ArticleCatalog(), new StageCalculator(), nav.Object), nav);
    }

    [Fact]
    public void Load_populates_articles_and_stages()
    {
        var (vm, _) = Build();
        vm.Load();
        vm.Articles.Should().HaveCountGreaterThan(0);
        vm.Stages.Should().HaveCount(7);
    }

    [Fact]
    public void Load_is_idempotent()
    {
        var (vm, _) = Build();
        vm.Load();
        var first = vm.Articles.Count;
        vm.Load();
        vm.Articles.Count.Should().Be(first);
        vm.Stages.Count.Should().Be(7);
    }

    [Fact]
    public async Task OpenArticle_navigates_to_article_detail_with_id()
    {
        var (vm, nav) = Build();
        vm.Load();
        var first = vm.Articles[0];
        await vm.OpenArticleCommand.ExecuteAsync(first);
        nav.Verify(n => n.GoToAsync($"ArticleDetailPage?articleId={first.Id}"), Times.Once);
    }

    [Fact]
    public async Task OpenArticle_null_is_noop()
    {
        var (vm, nav) = Build();
        await vm.OpenArticleCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OpenStage_navigates_to_stage_detail_with_key()
    {
        var (vm, nav) = Build();
        vm.Load();
        var stage = vm.Stages.First(s => s.Key == "autophagy");
        await vm.OpenStageCommand.ExecuteAsync(stage);
        nav.Verify(n => n.GoToAsync("StageDetailPage?stageKey=autophagy"), Times.Once);
    }

    [Fact]
    public void Stage_glossary_carries_canonical_keys()
    {
        var (vm, _) = Build();
        vm.Load();
        vm.Stages.Select(s => s.Key).Should().Contain(
            new[] { "anabolic", "catabolic", "fat-burning", "ketosis", "autophagy", "deep-ketosis", "extended" });
    }
}
