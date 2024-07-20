using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;

namespace Pipeline
{
    /// <summary>
    /// The Website class extends HttpClient and provides functionality for handling user login, 
    /// CSRF token extraction, and reading content from multiple URLs.
    /// </summary>
    public class Website : HttpClient
    {
        private readonly List<string> _urls = [];

        #region Properties
        public int PageCount { get; private set; }
        public int RecordCount { get; private set; }

        /// <summary>
        /// Gets the login endpoint URL.
        /// </summary>
        public string LoginEndPoint { get; } = string.Empty;

        /// <summary>
        /// Gets the URL to which the user is redirected after a successful login.
        /// </summary>
        public string RedirectUrl { get; } = string.Empty;

        /// <summary>
        /// Gets the User object containing login credentials.
        /// </summary>
        public User User { get; }

        /// <summary>
        /// Gets the CSRFToken object used for extracting CSRF tokens.
        /// </summary>
        private CSRFToken CSRFToken { get; }
        #endregion

        private static readonly string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
        private static readonly string URL_ENCODE_MIME_TYPE = "application/x-www-form-urlencoded";

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Website class with the specified login endpoint, redirect URL, and user credentials.
        /// </summary>
        /// <param name="loginEndPoint">The login endpoint URL.</param>
        /// <param name="redirectUrl">The URL to redirect to after a successful login.</param>
        /// <param name="user">The User object containing login credentials.</param>
        public Website(string loginEndPoint, string redirectUrl, User user) : base(new HttpClientHandler2())
        {
            LoginEndPoint = loginEndPoint;
            RedirectUrl = redirectUrl;
            User = user;
            DefaultRequestHeaders.Accept.Add(new(URL_ENCODE_MIME_TYPE));
            DefaultRequestHeaders.UserAgent.ParseAdd(USER_AGENT);
            DefaultRequestHeaders.Referrer = new(LoginEndPoint);
            CSRFToken = new(this, loginEndPoint);
            Timeout = TimeSpan.FromMinutes(200);
        }

        public Website(IConfigurationRoot configuration) : this(configuration["Credentials:LoginPage"]!, configuration["Credentials:RedirectPage"]!, new(configuration))
        { }

        /// <summary>
        /// Initializes a new instance of the Website class with the specified login endpoint, redirect URL, user credentials, and timeout.
        /// </summary>
        /// <param name="loginEndPoint">The login endpoint URL.</param>
        /// <param name="redirectUrl">The URL to redirect to after a successful login.</param>
        /// <param name="user">The User object containing login credentials.</param>
        /// <param name="timeout">The timespan to wait before the request times out.</param>
        public Website(string loginEndPoint, string redirectUrl, User user, TimeSpan timeout) : this(loginEndPoint, redirectUrl, user)
        {
            Timeout = timeout;
        }
        #endregion

        #region Login
        /// <summary>
        /// Asynchronously logs in the user by sending login credentials and CSRF token to the login endpoint.
        /// </summary>
        /// <returns>A task that represents the asynchronous login operation. The task result is true if the login was successful, otherwise false.</returns>
        public async Task<bool> LoginAsync()
        {
            string csrfToken = await CSRFToken.Extract();

            Dictionary<string, string> loginData = new()
            {
                { "email", User.UserName },
                { "password", User.Password },
                { "csrfmiddlewaretoken", csrfToken }
            };

            FormUrlEncodedContent content = new(loginData);

            HttpRequestMessage request = new(HttpMethod.Post, LoginEndPoint)
            {
                Content = content
            };

            HttpResponseMessage response;

            try
            {
                response = await SendAsync(request);
            }
            catch (Exception ex)
            {
                Dispose();
                AppManager.ExitOnError(ex.Message);
                return false;
            }

            try 
            {
                response.EnsureSuccessStatusCode();
                return await EnsureRedirectAsync();
            }
            catch (HttpRequestException)
            {
                Dispose();
                AppManager.ExitOnError("Login failed");
                return false;
            }
        }

        /// <summary>
        /// Ensures that the user is redirected to the expected URL after a successful login.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the redirect was successful.</returns>
        /// <exception cref="Exception">Thrown when the login failed or the redirect URL is not as expected.</exception>
        private async Task<bool> EnsureRedirectAsync()
        {
            HttpResponseMessage response = await GetAsync(RedirectUrl);
            if (response.RequestMessage.RequestUri.ToString() != RedirectUrl) 
            {
                AppManager.ExitOnError("Login failed or not redirected to the expected URL.");
                return false;
            }
            return true;
        }
        #endregion

        /// <summary>
        /// Adds a URL to the list of pages to be read.
        /// </summary>
        /// <param name="url">The URL of the page to be read.</param>
        public void AddPageToRead(string url) => _urls.Add(url);

        public void AddPageToRead(IConfigurationRoot configuration, int pageNumber) 
        {
            string pageUrl = configuration["Website:PageUrl"]!.Replace("{page}", pageNumber.ToString());
            AddPageToRead(pageUrl);
        }
        /// <summary>
        /// Asynchronously reads the content of all URLs added through <see cref="AddPageToRead(string)"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result is an array of tuples containing URLs and their corresponding content.</returns>
        public async Task<(string url, string content)[]> ReadAllAsync()
        {
            List<Task<(string url, string content)>> fetchTasks = [];

            foreach (var url in _urls)
                fetchTasks.Add(ReadPageContentAsync(url));

            return await Task.WhenAll(fetchTasks);
        }

        public async Task<List<(string url, string content)>> ReadAllByBatchAsync()
        {
            int batchSize = 5;
            int totalItems = _urls.Count;
            int numberOfBatches = (int)Math.Ceiling(totalItems / (double)batchSize);
            List<(string url, string content)> tuple = [];

            for (int i = 0; i < numberOfBatches; i++)
            {
                List<Task<(string url, string content)>> fetchTasks = [];

                IEnumerable<string> url_batch = _urls.Skip(i * batchSize).Take(batchSize);
                foreach (var url in url_batch)
                    fetchTasks.Add(ReadPageContentAsync(url));

                tuple.AddRange(await Task.WhenAll(fetchTasks));
            }

            return tuple;
        }

        /// <summary>
        /// Asynchronously reads the content of a single URL.
        /// </summary>
        /// <param name="url">The URL of the page to be read.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is a tuple containing the URL and its corresponding content.</returns>
        private async Task<(string url, string content)> ReadPageContentAsync(string url)
        {
            HttpResponseMessage response;
            try
            {
                response = await this.GetAsync(url);
            }
            catch (Exception ex) 
            {
                Dispose();
                AppManager.ExitOnError(ex.Message);
                return (string.Empty,string.Empty);
            }

            using (Stream responseStream = await response.Content.ReadAsStreamAsync())
            {
                string content = string.Empty;
                using (StreamReader reader = new(responseStream))
                {
                    try 
                    {
                        content = await reader.ReadToEndAsync();
                    }
                    catch (Exception ex)
                    {
                        Dispose();
                        AppManager.ExitOnError(ex.Message);
                        return (string.Empty, string.Empty);
                    }
                }
                return (url, content);
            }
        }

        /// <summary>
        /// Asynchronously sets the <see cref="PageCount"/> and <see cref="RecordCount"/> properties by fetching the number of records and pages from the specified website.
        /// This method is important to determine if there have been any updates on the website.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown if there is an error during the operation.</exception>
        public async Task GetPageAndRecordCountAsync()
        {
            try 
            {
                string url = "https://crm.lsc.group/dashboard/applications/?page=90000&feedback=reject&cohort=inactive&global__filter__component=all";
                (string url, string content) tupla = await ReadPageContentAsync(url);
                HtmlDocument document = new();
                document.LoadHtml(tupla.content);
                HtmlNode pagesDiv = document.DocumentNode.SelectSingleNode("//div[@class='pages']");
                HtmlNodeCollection allLinks = pagesDiv.SelectNodes(".//a");
                HtmlNode lastLink = allLinks.Last();
                PageCount = Convert.ToInt32(lastLink.InnerHtml);

                HtmlNode displayDiv = document.DocumentNode.SelectSingleNode("//div[@class='display']");
                RecordCount = Convert.ToInt32(displayDiv.InnerText.Split("of")[1].Trim().RemoveExtraSpaces());
            }
            catch(Exception ex) 
            {
                Dispose();
                AppManager.ExitOnError(ex.Message);
            }
        }

        /// <summary>
        /// Custom HttpClientHandler with a CookieContainer and automatic redirection enabled.
        /// </summary>
        internal class HttpClientHandler2 : HttpClientHandler
        {
            public HttpClientHandler2()
            {
                CookieContainer = new();
                AllowAutoRedirect = true;
            }
        }
    }
}