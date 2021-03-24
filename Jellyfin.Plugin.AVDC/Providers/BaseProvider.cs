using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AVDC.Configuration;
using MediaBrowser.Common.Json;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AVDC.Providers
{
    public abstract class BaseProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;

        protected readonly ILogger Logger;

        protected BaseProvider(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            Logger = logger;
            _httpClientFactory = httpClientFactory;
            _jsonOptions = new JsonSerializerOptions(JsonDefaults.GetOptions());
        }

        protected PluginConfiguration Config => Plugin.Instance?.Configuration ?? new PluginConfiguration();

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            Logger.LogInformation("[AVDC] GetImageResponse for url: {Url}", url);

            return GetAsync(url, cancellationToken);
        }

        protected async Task<Metadata> GetMetadata(string vid, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(vid))
            {
                Logger.LogError("[AVDC] invalid vid: \"{Vid}\"", vid);
                return new Metadata();
            }

            var url = $"{Config.Server}{ApiPath.Metadata}{vid}";

            try
            {
                var contentStream = await GetStream(url, cancellationToken);
                return await JsonSerializer.DeserializeAsync<Metadata>(contentStream, _jsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogError("[AVDC] GetMetadata for {Vid} failed: {Message}", vid, e.Message);
                return new Metadata();
            }
        }

        protected async Task<Actress> GetActress(string name, CancellationToken cancellationToken)
        {
            var url = $"{Config.Server}{ApiPath.Actress}{name}";

            try
            {
                var contentStream = await GetStream(url, cancellationToken);
                return await JsonSerializer.DeserializeAsync<Actress>(contentStream, _jsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogError("[AVDC] GetActress for {Name} failed: {Message}", name, e.Message);
                return new Actress();
            }
        }

        private async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            Logger.LogDebug("[AVDC] HTTP request to: {Url}", url);

            cancellationToken.ThrowIfCancellationRequested();

            var httpClient = _httpClientFactory.CreateClient();

            if (!string.IsNullOrEmpty(Config.Token) && url.StartsWith(Config.Server))
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Config.Token);

            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            return response;
        }

        private async Task<Stream> GetStream(string url, CancellationToken cancellationToken)
        {
            var response = await GetAsync(url, cancellationToken);
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
    }
}