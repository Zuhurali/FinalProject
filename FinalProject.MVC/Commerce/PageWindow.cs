namespace FinalProject.MVC.Commerce;

public static class PageWindow
{
    public static (int Start, int End, int Page, int PageCount) Describe(int page, int pageSize, int itemCount, int total)
    {
        if (pageSize < 1)
        {
            pageSize = 10;
        }

        var pageCount = total <= 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page < 1)
        {
            page = 1;
        }

        if (page > pageCount)
        {
            page = pageCount;
        }

        if (total <= 0 || itemCount <= 0)
        {
            return (0, 0, page, pageCount);
        }

        var start = ((page - 1) * pageSize) + 1;
        var end = Math.Min(start + itemCount - 1, total);
        if (start > total)
        {
            start = total;
        }

        return (start, end, page, pageCount);
    }
}
