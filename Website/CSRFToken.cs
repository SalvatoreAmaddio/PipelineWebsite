using System.Text.RegularExpressions;

namespace Pipeline
{
    /// <summary>
    /// The CSRFToken class is used to extract a CSRF token from an HTML page.
    /// </summary>
    public partial class CSRFToken
    {
        private readonly HttpClient _httpClient;
        private readonly string _endPoint;

        /// <summary>
        /// Initializes a new instance of the CSRFToken class with the specified HttpClient and endpoint URL.
        /// </summary>
        /// <param name="httpClient">The HttpClient used to make HTTP requests.</param>
        /// <param name="loginEndPoint">The endpoint URL from which to extract the CSRF token.</param>
        public CSRFToken(HttpClient httpClient, string loginEndPoint)
        { 
            _httpClient = httpClient;
            _endPoint = loginEndPoint;
        }

        /// <summary>
        /// Regular expression to match the CSRF token in the HTML content.
        /// </summary>
        /// <returns>A Regex object configured to match the CSRF token.</returns>
        [GeneratedRegex(@"<input[^>]*name=""csrfmiddlewaretoken""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase, "en-GB")]
        private static partial Regex MyRegex();

        /// <summary>
        /// Extracts the CSRF token from the HTML content of the login page.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the extracted CSRF token.</returns>
        /// <exception cref="Exception">Thrown when the CSRF token is not found in the HTML content.</exception>
        public async Task<string> Extract()
        {
            string htmlContent = await GetLoginPageContentAsync();
            Match match = MyRegex().Match(htmlContent);
            if (match.Success)
                return match.Groups[1].Value;
            _httpClient.Dispose();
            AppManager.ExitOnError("CSRF token not found in login page");
            return string.Empty;
        }

        /// <summary>
        /// Asynchronously gets the HTML content of the login page.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the HTML content of the login page.</returns>
        private async Task<string> GetLoginPageContentAsync()
        {
            HttpResponseMessage mainPageResponse;
            try
            {
                mainPageResponse = await _httpClient.GetAsync(_endPoint);
            }
            catch
            {
                AppManager.ExitOnError("An error has occured");
                return string.Empty;
            }

            mainPageResponse.EnsureSuccessStatusCode();
            return await mainPageResponse.Content.ReadAsStringAsync();
        }
    }
}