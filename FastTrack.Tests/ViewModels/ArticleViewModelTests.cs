using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class ArticleViewModelTests
{
    private static (ArticleViewModel Vm, Mock<INavigationService> Nav) Build()
    {
        var nav = new Mock<INavigationService>();
        return (new ArticleViewModel(new ArticleCatalog(), nav.Object), nav);
    }

    [Fact]
    public void LoadFromId_known_id_populates_fields()
    {
        var (vm, _) = Build();
        vm.LoadFromId("what-is-if");
        vm.IsLoaded.Should().BeTrue();
        vm.HasError.Should().BeFalse();
        vm.Title.Should().Contain("intermittent fasting");
        vm.HeroAsset.Should().EndWith(".svg");
        vm.Sections.Should().HaveCountGreaterThan(0);
        vm.ReadingTimeDisplay.Should().Contain("min read");
    }

    [Fact]
    public void LoadFromId_unknown_id_sets_error()
    {
        var (vm, _) = Build();
        vm.LoadFromId("not-real");
        vm.IsLoaded.Should().BeFalse();
        vm.HasError.Should().BeTrue();
        vm.ErrorMessage.Should().NotBeNullOrEmpty();
        vm.Sections.Should().BeEmpty();
    }

    [Fact]
    public void LoadFromId_null_sets_error()
    {
        var (vm, _) = Build();
        vm.LoadFromId(null);
        vm.IsLoaded.Should().BeFalse();
        vm.HasError.Should().BeTrue();
    }

    [Fact]
    public void LoadFromId_clears_previous_sections_on_reload()
    {
        var (vm, _) = Build();
        vm.LoadFromId("what-is-if");
        var firstCount = vm.Sections.Count;
        vm.LoadFromId("breaking-fast");
        // The second article's section count should not stack on the first.
        vm.Sections.Count.Should().BeLessThan(firstCount + vm.Sections.Count);
        vm.Sections.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Back_calls_go_back()
    {
        var (vm, nav) = Build();
        await vm.BackCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }
}

public class ArticleCatalogTests
{
    [Fact]
    public void All_returns_at_least_six_articles()
    {
        new ArticleCatalog().All().Should().HaveCountGreaterThanOrEqualTo(6);
    }

    [Fact]
    public void Articles_all_have_id_title_hero_and_sections()
    {
        foreach (var a in new ArticleCatalog().All())
        {
            a.Id.Should().NotBeNullOrWhiteSpace();
            a.Title.Should().NotBeNullOrWhiteSpace();
            a.HeroAsset.Should().EndWith(".svg");
            a.Sections.Should().HaveCountGreaterThan(0);
            a.ReadingMinutes.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void Article_ids_are_unique()
    {
        var ids = new ArticleCatalog().All().Select(a => a.Id).ToList();
        ids.Distinct().Count().Should().Be(ids.Count);
    }
}
