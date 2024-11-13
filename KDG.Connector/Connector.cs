using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using KDG.Connector.Models;
using KDG.Common.Extensions;

namespace KDG.Connector
{
    public abstract class Connector
    {
        public Connector(string baseUrl,
                            ILogger<Connector> logger,
                            Newtonsoft.Json.JsonSerializer serializer,
                            JsonSerializerSettings serializerSettings)
        {
            BaseUrl = baseUrl;
            BaseUri = new Uri(baseUrl);
            Logger = logger;
            ConnectorFriendlyName = GetType().Name.Wordify();
            Serializer = serializer;
            SerializerSettings = serializerSettings;
        }

        protected readonly Uri BaseUri;
        protected readonly string BaseUrl;
        protected readonly ILogger<Connector> Logger;
        protected readonly Newtonsoft.Json.JsonSerializer Serializer;
        protected readonly JsonSerializerSettings SerializerSettings;
        protected readonly string ConnectorFriendlyName;
        protected string CommentDivider = "=================================================";

        //https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.timeout?view=net-8.0
        public const int TimeOutInMinutes = 5; //Default value is 100 seconds (1 min 40 seconds)

        #region Helper methods
        protected abstract Task<AuthenticationHeaderValue> GetAuthenticationHeaderValue();
        protected virtual Dictionary<string, string> GetHeaders(ApiParams config)
        {
            var dict = new Dictionary<string, string>();

            if (config.headers != null && config.headers.Any())
            {
                foreach (var header in config.headers)
                {
                dict.Add(header.Key, header.Value);
                }
            }

            return dict;
        }

        private bool TryGetResponse<RESPONSE>(HttpResponseMessage response, string contents, bool logResponseData, out RESPONSE? responseData)
        {
            var success = false;
            responseData = default(RESPONSE);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                success = true;
            }
            else if (response.IsSuccessStatusCode)
            {
                success = response.IsSuccessStatusCode;

                if (contents != "")
                {
                var divider = string.Join("", Environment.NewLine, CommentDivider, Environment.NewLine);
                var contentsLogData = logResponseData ? contents : "[Omitted]";

                if (logResponseData)
                    Logger.LogInformation("{divider}{connectorFriendlyName} Response: {newLine}{contents}{divider}",
                                    divider,
                                    ConnectorFriendlyName,
                                    Environment.NewLine,
                                    contentsLogData,
                                    divider);

                responseData = JsonConvert.DeserializeObject<RESPONSE>(contents, SerializerSettings);
                }
            }

            return success;
        }

        private async Task<Response<RESPONSE>> AttemptSend<RESPONSE>(HttpClient client, bool logResponseData, Func<Task<HttpRequestMessage>> getRequest)
        {
            RESPONSE? responseData;
            var contents = string.Empty;
            HttpRequestMessage? request = null;
            var requestUri = string.Empty;
            var requestDataDisplayed = false;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            HttpResponseMessage? response = default(HttpResponseMessage);

            try
            {
                request = await getRequest();
                requestUri = request.RequestUri == null ? "" : request.RequestUri.ToString();

                if (!requestDataDisplayed)
                {
                    if (request.Content != null)
                    {
                    var json = await request.Content.ReadAsStringAsync();
                    var message = string.Join(Environment.NewLine, CommentDivider, json, CommentDivider);
                    Logger.LogInformation(message);
                    }

                    requestDataDisplayed = true;
                }

                Logger.LogInformation("Sending {method} request to {url}", request.Method.Method, requestUri);

                response = await client.SendAsync(request);
                contents = await response.Content.ReadAsStringAsync();
                statusCode = response.StatusCode;

                TryGetResponse<RESPONSE>(response, contents, logResponseData, out responseData);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if(request != null)
                {
                    request.Dispose();
                }
            }

            if(responseData != null)
            {
                return new Response<RESPONSE>(responseData,response!);
            }
            else
            {
                throw new Exception("null response");
            }
        }

        protected string GetUrl(string pathToAppend, string? baseUrlOverride, bool addSurroundingSlashes = false)
        {
            var url = string.IsNullOrEmpty(baseUrlOverride) ? BaseUrl : baseUrlOverride;
            var uri = new Uri(url);

            if (!string.IsNullOrEmpty(pathToAppend))
            {
                if (addSurroundingSlashes)
                {
                    if (!pathToAppend.StartsWith("/"))
                    {
                        pathToAppend = "/" + pathToAppend;
                    }
                    if (!pathToAppend.EndsWith("/"))
                    {
                        pathToAppend = pathToAppend + "/";
                    }
                }

                if (!Uri.TryCreate(uri, pathToAppend, out var result))
                {
                    throw new Exception($"'{pathToAppend}' is not a valid relative path. Please review the path and try again");
                }

                url = result.ToString();
            }

            return url;
        }

        protected async Task<HttpRequestMessage> GetRequest(HttpMethod method, Uri uri, ApiParams config)
        {
            var request = new HttpRequestMessage(method, uri);
            var headers = GetHeaders(config);
            request.Headers.Authorization = await GetAuthenticationHeaderValue();

            if ((method == HttpMethod.Post || method == HttpMethod.Patch || method == HttpMethod.Put) &&
                config.postParams != null)
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(config.postParams, SerializerSettings);

                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return request;
        }
        #endregion

        protected virtual async Task<Response<RESPONSE>> Send<RESPONSE>(HttpMethod method, string path, ApiParams config, bool logResponseData = true, string? baseUrlOverride = null)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(TimeOutInMinutes);
                var url = GetUrl(path, baseUrlOverride);
                var uri = config.urlParams == null ?
                        new Uri(url) :
                        KDG.Connector.Utilities.Parameters.GenerateUri(new Uri(url), config.urlParams);

                if(config.AcceptHeader != null)
                {
                    client.DefaultRequestHeaders.Accept.Add(config.AcceptHeader);
                }

                return await AttemptSend<RESPONSE>(client, logResponseData, async () => await GetRequest(method, uri, config));
            }
        }
    }
}
