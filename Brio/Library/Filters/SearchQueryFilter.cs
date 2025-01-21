namespace Brio.Library.Filters;

internal class SearchQueryFilter : FilterBase
{
    public string[]? Query;

    public SearchQueryFilter()
        : base("搜索")
    {
    }

    public override void Clear()
    {
        this.Query = null;
    }

    public override bool Filter(EntryBase entry)
    {
        if(this.Query == null)
            return false;

        return entry.Search(this.Query);
    }
}
