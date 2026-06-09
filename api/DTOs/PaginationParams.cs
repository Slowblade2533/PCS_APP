using System.ComponentModel.DataAnnotations;

namespace PCS_API.DTOs;

public class PaginationParamsDto
{
    private const int MaxPageSize = 100;

    private int _pageNumber = 1;
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber ต้องมากกว่าหรือเท่ากับ 1")]
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = Math.Max(1, value);
    }

    private int _pageSize = 15;
    [Range(1, MaxPageSize, ErrorMessage = "PageSize ต้องอยู่ระหว่าง 1 ถึง 100")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    public int GetSafeOffset()
    {
        int offset = (_pageNumber - 1) * _pageSize;
        return Math.Min(offset, 10_000);
    }
}