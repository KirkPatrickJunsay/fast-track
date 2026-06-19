using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

/// <summary>
/// Source of education content shown on the Learn tab. Implementation is in-memory
/// for v1 — the seed text is small and shipped with the app so we don't need a DB
/// table or remote fetch. Pull this onto a real interface anyway so we can swap to
/// a Markdown-loader or CMS later without touching the ViewModels.
/// </summary>
public interface IArticleCatalog
{
    IReadOnlyList<Article> All();
    Article? GetById(string id);
}
