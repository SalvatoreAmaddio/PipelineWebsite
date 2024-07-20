using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Pipeline
{
    /// <summary>
    /// The HtmlPage class is used to represent the the content of an HTML page which has a Table tag.
    /// </summary>
    public partial class HtmlTablePage
    {
        /// <summary>
        /// Gets the URL of the HTML page.
        /// </summary>
        public string URL { get; }


        /// <summary>
        /// Gets the content of the HTML page.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets the page number extracted from the URL.
        /// </summary>
        public int PageNumer { get; }

        /// <summary>
        /// Initializes a new instance of the HtmlPage class with the specified URL and content.
        /// </summary>
        /// <param name="url">The URL of the HTML page.</param>
        /// <param name="content">The content of the HTML page.</param>
        public HtmlTablePage(string url, string content)
        {
            this.URL = url;
            this.Content = content;
            this.PageNumer = ExtractPageNumber(this.URL);
        }

        /// <summary>
        /// Initializes a new instance of the HtmlPage class with a tuple containing the URL and content.
        /// </summary>
        /// <param name="tupla">A tuple containing the URL and content of the HTML page.</param>
        public HtmlTablePage((string url, string content) tupla) : this(tupla.url, tupla.content) { }

        /// <summary>
        /// Extracts the table from the HTML content.
        /// </summary>
        /// <returns>A collection of collections of strings, where each inner collection represents a row of the table.</returns>
        /// <exception cref="Exception">Thrown when no table or no rows are found in the HTML content.</exception>
        public IEnumerable<IEnumerable<string>> ExtractTable()
        {
            HtmlDocument doc = new();
            doc.LoadHtml(Content);
            try
            {
                HtmlNode table = doc.DocumentNode.SelectSingleNode("//table") ?? throw new Exception("No table found in HTML");
                HtmlNodeCollection rows = table.SelectNodes(".//tr") ?? throw new Exception("No rows found in table");

                return rows.Select(row =>
                {
                    HtmlNodeCollection cells = row.SelectNodes(".//th|.//td");
                    if (cells == null) return [];
                    return cells.Select(cell => cell.InnerText.Trim()).ToList();
                }).ToList();
            }
            catch (Exception ex)
            {
                AppManager.ExitOnError(ex.Message);
                return [];
            }
        }

        [GeneratedRegex(@"[?&]page=(\d+)")]
        private static partial Regex MyRegex();

        /// <summary>
        /// Extracts the page number from the URL.
        /// </summary>
        /// <param name="url">The URL of the HTML page.</param>
        /// <returns>The page number extracted from the URL.</returns>
        /// <exception cref="Exception">Thrown when no page number is found in the URL.</exception>
        private static int ExtractPageNumber(string url)
        {
            Match match = MyRegex().Match(url);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int pageNumber))
                return pageNumber;
            AppManager.ExitOnError("Page number not found in URL");
            return 0;
        }
        public override bool Equals(object? obj) => obj is HtmlTablePage page && PageNumer == page.PageNumer;
        public override int GetHashCode() => HashCode.Combine(PageNumer);
        public override string? ToString() => this.URL;
    }
}