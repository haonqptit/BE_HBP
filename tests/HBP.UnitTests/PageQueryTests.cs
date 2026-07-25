using HBP.Application.Common;

namespace HBP.UnitTests;

public class PageQueryTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void PageIsAtLeastOne(int page, int expected) =>
        Assert.Equal(expected, new PageQuery { Page = page }.NormalizedPage);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(20, 20)]
    [InlineData(500, 100)]
    public void PageSizeIsClampedToOneHundred(int pageSize, int expected) =>
        Assert.Equal(expected, new PageQuery { PageSize = pageSize }.NormalizedPageSize);

    [Fact]
    public void BlankSearchBecomesNull() =>
        Assert.Null(new PageQuery { Search = "   " }.TrimmedSearch);

    [Fact]
    public void SearchIsTrimmed() =>
        Assert.Equal("nguyen", new PageQuery { Search = "  nguyen " }.TrimmedSearch);

    [Fact]
    public void SortIsLowerCasedForWhitelistMatching() =>
        Assert.Equal("created_at_desc", new PageQuery { Sort = " Created_At_Desc " }.NormalizedSort);
}
