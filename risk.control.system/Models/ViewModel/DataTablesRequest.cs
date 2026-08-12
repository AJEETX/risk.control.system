namespace risk.control.system.Models.ViewModel
{

    public class DataTablesRequest
    {
        /// <summary>
        /// Information sequence counter sent by DataTables to draw/render requests synchronously.
        /// </summary>
        public int Draw { get; set; }

        /// <summary>
        /// Paging first record indicator (0-based index offset).
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Number of records that the table can display in the current draw (-1 for all records).
        /// </summary>
        public int Length { get; set; } = 10;

        /// <summary>
        /// Global search term string sent from client-side search input.
        /// </summary>
        public string Search { get; set; } = string.Empty;

        /// <summary>
        /// Primary order column index. Helper property for quick single-column sorting.
        /// </summary>
        public int OrderColumn { get; set; }

        /// <summary>
        /// Direction for sorting ("asc" or "desc"). Helper property for quick single-column sorting.
        /// </summary>
        public string OrderDir { get; set; } = "asc";

        /// <summary>
        /// Detailed search payload sent by standard DataTables AJAX requests.
        /// </summary>
        public DataTablesSearch? SearchDetails { get; set; }

        /// <summary>
        /// List of sort order criteria sent by DataTables.
        /// </summary>
        public List<DataTablesOrder> Order { get; set; } = new();

        /// <summary>
        /// List of column configuration definitions sent by DataTables.
        /// </summary>
        public List<DataTablesColumn> Columns { get; set; } = new();
    }

    public class DataTablesSearch
    {
        public string Value { get; set; } = string.Empty;
        public bool Regex { get; set; }
    }

    public class DataTablesOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "asc";
    }

    public class DataTablesColumn
    {
        public string Data { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTablesSearch? Search { get; set; }
    }
}
