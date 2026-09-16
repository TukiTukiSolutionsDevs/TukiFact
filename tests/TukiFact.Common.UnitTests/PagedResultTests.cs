using TukiFact.Common.Domain;

namespace TukiFact.Common.UnitTests;

public sealed class PagedResultTests
{
    [Fact]
    public void Constructor_WithItemsAndPaging_ExposesAllProperties()
    {
        // Arrange
        var items = new[] { "a", "b" };

        // Act
        var result = new PagedResult<string>(items, TotalCount: 12, PageNumber: 2, PageSize: 2);

        // Assert
        result.Items.Should().Equal(items);
        result.TotalCount.Should().Be(12);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public void TotalPages_TotalCountNotMultipleOfPageSize_RoundsUp()
    {
        // Arrange
        var result = new PagedResult<string>([], TotalCount: 11, PageNumber: 1, PageSize: 5);

        // Act & Assert
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TotalPages_PageSizeZero_ReturnsZero()
    {
        // Arrange
        var result = new PagedResult<string>([], TotalCount: 11, PageNumber: 1, PageSize: 0);

        // Act & Assert
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void HasNextPageAndHasPreviousPage_FirstMiddleAndLastPage_ReflectPosition()
    {
        // Arrange
        var middlePage = new PagedResult<string>([], TotalCount: 30, PageNumber: 2, PageSize: 10);
        var firstPage = new PagedResult<string>([], TotalCount: 30, PageNumber: 1, PageSize: 10);
        var lastPage = new PagedResult<string>([], TotalCount: 30, PageNumber: 3, PageSize: 10);

        // Act & Assert
        middlePage.HasNextPage.Should().BeTrue();
        middlePage.HasPreviousPage.Should().BeTrue();
        firstPage.HasPreviousPage.Should().BeFalse();
        lastPage.HasNextPage.Should().BeFalse();
    }
}
