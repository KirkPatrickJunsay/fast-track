using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class ArticleDetailPage : ContentPage, IQueryAttributable
{
    private readonly ArticleViewModel _vm;

    public ArticleDetailPage(ArticleViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        query.TryGetValue("articleId", out var id);
        _vm.LoadFromId(id?.ToString());
    }
}
