namespace CatalogX.API
{
    public class ProductQueryParameters
    {
        private const int MaxPageSize = 100;

        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        //filtering params
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string Category { get; set; }
        public string Search { get; set; } //search in product name and description

    }
}
