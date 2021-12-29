using CsQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Moneyes.LiveData
{
    /*DEPRECATED. USE ONLINE BANKING SERVICE INSTEAD.*/
    public class SparkasseSalesService
    {
        const string BaseUrl = "https://www.sparkasse-pforzheim-calw.de";
        const string HomePage = "/de/home.html";
        const string SalesPage = "/de/home/onlinebanking/umsaetze/umsaetze.html";

        private readonly HttpClient _httpClient;

        public SparkasseSalesService()
        {
            CookieContainer cookies = new();
            HttpClientHandler handler = new()
            {
                CookieContainer = cookies
            };

            _httpClient = new(handler)
            {
                Timeout = TimeSpan.FromSeconds(1000)
            };
        }

        /// <summary>
        /// Asynchronously logs into the bank account with the given credentials.
        /// </summary>
        /// <param name="userName">The user name to use.</param>
        /// <param name="password">The password / pin to use.</param>
        /// <returns></returns>
        public async Task Login(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentException("User name cannot be empty", nameof(userName));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password cannot be empty", nameof(password));
            }

            HttpResponseMessage response = await LoginInternal(userName, password);

            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Asynchronously logs out of the bank account.
        /// </summary>
        /// <returns></returns>
        public async Task Logout()
        {
            HttpResponseMessage response = await LogoutInternal();

            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Requests a sales csv file, for the selected bank account and time span.
        /// </summary>
        /// <param name="selectAccount">The bank account to use.</param>
        /// <param name="startDate">The start date of the sales to include.</param>
        /// <param name="endDate">The end date of the sales to include.</param>
        /// <returns>The content of the csv file.</returns>
        public async Task<string> GetSalesCsvContent(
            Func<IEnumerable<string>, string> selectAccount,
            DateTime startDate,
            DateTime endDate)
        {
            var (html, data) = await GetSalesPage(selectAccount, startDate, endDate);

            return await GetSalesCSV(html, data);
        }

        /// <summary>
        /// Checks if a user session is currently active by requesting the home page.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsSessionActive()
        {
            try
            {
                string homePageHtml = await GetHomePage();

                return IsSessionActiveInternal(homePageHtml);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks whether a user session is active on the current page using the DOM.
        /// </summary>
        /// <param name="pageHtml">The DOM of the page to test.</param>
        /// <returns></returns>
        private static bool IsSessionActiveInternal(string pageHtml)
        {
            CQ dom = pageHtml;
                        
            if (dom["form.header-logout"].Any())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the HTML content of the home page.
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetHomePage()
        {
            HttpRequestMessage loginPageRequest = new(HttpMethod.Get, BaseUrl + HomePage);

            var loginPageResponse = await _httpClient.SendAsync(loginPageRequest);

            return await loginPageResponse.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> LoginInternal(string userName, string password)
        {
            string homePageHtml = await GetHomePage();

            if (IsSessionActiveInternal(homePageHtml))
            {
                throw new InvalidOperationException("An active session currently exists. Already logged in.");
            }

            CQ dom = homePageHtml;

            CQ form = dom[".header-login"];
            CQ nameInput = dom[".header-login > input:nth-of-type(1)"];
            CQ pinInput = dom[".header-login > input:nth-of-type(2)"];
            CQ hiddenInput = dom[".header-login > input:nth-of-type(4)"];
            CQ submitBtn = dom[".header-login .login input"];

            string nameId = nameInput.Attr("name");
            string pinId = pinInput.Attr("name");
            string submitId = submitBtn.Attr("name");
            string hiddenId = hiddenInput.Attr("name");
            string hiddenValue = hiddenInput.Val();
            string formAction = form.Attr("action");

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(nameId, userName),
                new KeyValuePair<string, string>(pinId, password),
                new KeyValuePair<string, string>(submitId, "Anmelden"),
                new KeyValuePair<string, string>("isJavaScriptActive", "1"),
                new KeyValuePair<string, string>(hiddenId, hiddenValue)
            });

            var loginUri = new Uri(BaseUrl + formAction);

            var loginResponse = await _httpClient.PostAsync(loginUri, formContent);

            return loginResponse;
        }

        private async Task<HttpResponseMessage> LogoutInternal()
        {
            string homePageHtml = await GetHomePage();

            if (!IsSessionActiveInternal(homePageHtml))
            {
                throw new InvalidOperationException("No active session to logout from.");
            }

            CQ dom = homePageHtml;

            CQ form = dom["form.header-logout"];
            CQ hiddenLogoutInput = dom[".header-logout > input:last-of-type"];
            CQ submitBtn = dom[".logout > input"];

            string postAction = form.Attr("action");
            string hiddenLogoutKey = hiddenLogoutInput.Attr("name");
            string hiddenLogoutVal = hiddenLogoutInput.Val();
            string submitKey = submitBtn.Attr("name");
            string submitVal = submitBtn.Val();

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(submitKey, submitVal),
                new KeyValuePair<string, string>("logout", "logout"),
                new KeyValuePair<string, string>(hiddenLogoutKey, hiddenLogoutVal),
            });

            var logoutResponse = await _httpClient.PostAsync(BaseUrl + postAction, formContent);

            return logoutResponse;
        }

        /// <summary>
        /// Loads the sales from the sales page.
        /// </summary>
        /// <param name="selectAccount">The bank account to use.</param>
        /// <param name="startDate">The start date of the sales to load.</param>
        /// <param name="endDate">The end date of the sales to load.</param>
        /// <returns>The HTML content of the loaded sales page 
        /// and <see cref="SalesPostData"/> to reuse.</returns>
        private async Task<(string html, SalesPostData data)> GetSalesPage(
            Func<IEnumerable<string>, string> selectAccount,
            DateTime startDate,
            DateTime endDate)
        {
            if (startDate > endDate)
            {
                throw new ArgumentException("The start date cannot be greater than the end date", nameof(startDate));
            }

            if ((DateTime.Now - startDate).Days > 90)
            {
                throw new InvalidOperationException("Requesting data older than 90 days is not supported." +
                    " Start date needs to be greater.");
            }

            // Get html of template sales page
            HttpRequestMessage salesPageRequest = new(HttpMethod.Get, BaseUrl + SalesPage);
            HttpResponseMessage salesPageResponse = await _httpClient.SendAsync(salesPageRequest);

            string salesPageHtml = await salesPageResponse.Content.ReadAsStringAsync();

            // Verify session is active
            if (!IsSessionActiveInternal(salesPageHtml))
            {
                throw new InvalidOperationException("No user session is currently active. You have to login first.");
            }

            // Parse necessary elements from dom

            CQ dom = salesPageHtml;

            CQ form = dom["div.cbox:nth-child(4) > form:nth-child(1)"];
            CQ accountInputElement = dom["#konto select"];
            CQ accountOptionElements = dom["#konto select option"];
            CQ hiddenInputElement = dom["div.cbox:nth-child(4) > form:nth-child(1) input:nth-of-type(1)"];
            CQ hiddenLogoutInputElement = dom[".header-logout > input:nth-child(7)"];
            CQ startDateInputElement = dom["#zeitraumKalender > input:nth-of-type(1)"];
            CQ endDateInputElement = dom["#zeitraumKalender > input:nth-of-type(2)"];
            CQ submitBtnElement = dom[".icon-if5_refresh > input:nth-child(1)"];

            string accountInputName = accountInputElement.Attr("name");
            string submitButtonName = submitBtnElement.Attr("name");
            string hiddenInputName = hiddenInputElement.Attr("name");
            string hiddenLogoutInputName = hiddenLogoutInputElement.Attr("name");
            string hiddenLogoutInputVal = hiddenLogoutInputElement.Val();
            string formAction = form.Attr("action");
            string startDateInputName = startDateInputElement.Attr("name");
            string endDateInputName = endDateInputElement.Attr("name");

            // Let user select account
            var accountOptionsMap = accountOptionElements.Elements.Skip(1)
                .ToDictionary(element => element.InnerText, element => element.Value);

            var selectedAccountText = selectAccount?.Invoke(accountOptionsMap.Keys);
            var selectedAccountId = accountOptionsMap[selectedAccountText];

            // Create post request data

            SalesPostData postData = new()
            {
                PostAction = formAction,
                Account = new(accountInputName, selectedAccountId),
                HiddenField = new(hiddenInputName, "1"),
                HiddenLogoutField = new(hiddenLogoutInputName, hiddenLogoutInputVal),
                StartDate = new(startDateInputName, startDate.ToString("dd.MM.yyyy")),
                EndDate = new(endDateInputName, endDate.ToString("dd.MM.yyyy")),
                Submit = new(submitButtonName, "Weiter")
            };

            // Send post request to get sales

            HttpRequestMessage postRequest = CreatePostRequestWithHeaders(postData);
            HttpResponseMessage postResponse = await _httpClient.SendAsync(postRequest);

            // Return resulting html
            return (await postResponse.Content.ReadAsStringAsync(), postData);
        }

        /// <summary>
        /// Requests a sales csv file from a loaded sales page and existing <see cref="SalesPostData"/>.
        /// </summary>
        /// <param name="salesPageHtml">The HTML content of the loaded sales page.</param>
        /// <param name="postData">The post data used to load the sales.</param>
        /// <returns>The content of the csv file.</returns>
        private async Task<string> GetSalesCSV(string salesPageHtml, SalesPostData postData)
        {
            // Parse necessary dom elements that changed

            CQ dom = salesPageHtml;

            string postAction = dom["form.pfm-umsatz"].Attr("action");
            string hiddenInputKey = dom["div.cbox:nth-child(4) > form:nth-child(1) input:nth-of-type(1)"].Attr("name");
            string searchElementKey = dom[".bsearch input"].Attr("name");
            string numberSelectKey = dom[".bpageselect select"].Attr("name");
            string numberSelectValue = dom[".bpageselect select option:nth-of-type(5)"].Val();
            string submitElementKey = dom[".exportable > ul:nth-child(2) > li:nth-child(1) > div:nth-child(1) input"]
                .Attr("name");

            // Apply changes to post data

            postData.PostAction = postAction;
            postData.HiddenField = new(hiddenInputKey, "0");
            postData.Search = new(searchElementKey, "");
            postData.NumerOfSalesPerPage = new(numberSelectKey, numberSelectValue);
            postData.Submit = new(submitElementKey, "CSV-CAMT-Format");

            // Send post request to retrieve csv

            HttpRequestMessage postRequest = CreatePostRequestWithHeaders(postData);
            postRequest.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            
            HttpResponseMessage response = await _httpClient.SendAsync(postRequest, HttpCompletionOption.ResponseHeadersRead);

            // Return csv content

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Create form content from <see cref="SalesPostData"/>.
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        private static FormUrlEncodedContent CreateFormContent(SalesPostData postData)
        {
            return new FormUrlEncodedContent(new[]
            {
                postData.HiddenField,
                postData.Account,
                postData.StartDate,
                postData.EndDate,
                postData.Search,
                postData.NumerOfSalesPerPage,
                postData.HiddenLogoutField,
                postData.Submit
            });
        }

        /// <summary>
        /// Create a HTTP post request with predefined headers, 
        /// and a request Uri created from the <see cref="SalesPostData.PostAction"/>.
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        private static HttpRequestMessage CreatePostRequestWithHeaders(SalesPostData postData)
        {
            Uri postUri = new(BaseUrl + postData.PostAction);
            HttpRequestMessage postReq = new(HttpMethod.Post, postUri);

            postReq.Content = CreateFormContent(postData);

            postReq.Headers.Add("Accept", "*/*");
            postReq.Headers.Add("Accept-Language", "de,en-US;q=0.7,en;q=0.3");
            postReq.Headers.Add("Cache-Control", "no-cache");
            postReq.Headers.Add("Connection", "keep-alive");
            postReq.Headers.Add("DNT", "1");
            postReq.Headers.Add("Host", "www.sparkasse-pforzheim-calw.de");
            postReq.Headers.Add("Origin", "https://www.sparkasse-pforzheim-calw.de");
            postReq.Headers.Add("Pragma", "no-cache");
            postReq.Headers.Add("Referer", "https://www.sparkasse-pforzheim-calw.de/de/home/onlinebanking/umsaetze/umsaetze.html?n=true&stref=hnav");
            postReq.Headers.Add("Sec-Fetch-Dest", "empty");
            postReq.Headers.Add("Sec-Fetch-Mode", "cors");
            postReq.Headers.Add("Sec-Fetch-Site", "same-origin");
            postReq.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:93.0) Gecko/20100101 Firefox/93.0");
            postReq.Headers.Add("X-Requested-With", "XMLHttpRequest");

            return postReq;
        }

        /// <summary>
        /// Represents data for a sales post request.
        /// </summary>
        record SalesPostData
        {
            public string PostAction { get; set; }
            public KeyValuePair<string, string> Account { get; set; } = new(null, null);
            public KeyValuePair<string, string> HiddenField { get; set; } = new(null, null);
            public KeyValuePair<string, string> HiddenLogoutField { get; set; } = new(null, null);
            public KeyValuePair<string, string> StartDate { get; set; } = new(null, null);
            public KeyValuePair<string, string> EndDate { get; set; } = new(null, null);
            public KeyValuePair<string, string> Search { get; set; } = new(null, null);
            public KeyValuePair<string, string> NumerOfSalesPerPage { get; set; } = new(null, null);
            public KeyValuePair<string, string> Submit { get; set; } = new(null, null);
        }
    }
}
