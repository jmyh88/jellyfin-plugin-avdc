using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AVDC.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AVDC.Providers
{
    public static class AvdcApi
    {
        public const string Metadata = "/metadata/";
        public const string People = "/image/people/";
        public const string Primary = "/image/primary/";
        public const string Backdrop = "/image/backdrop/";
    }

    public abstract class AvdcBaseProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJsonSerializer _jsonSerializer;
        protected readonly PluginConfiguration Config;
        protected readonly ILogger Logger;

        protected AvdcBaseProvider(IHttpClientFactory httpClientFactory, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _jsonSerializer = jsonSerializer;
            Logger = logger;

            Config = Plugin.Instance == null ? new PluginConfiguration() : Plugin.Instance.Configuration;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            Logger.LogInformation($"[AVDC] GetImageResponse for url: {url}");

            return GetAsync(url, cancellationToken);
        }

        protected async Task<Metadata> GetMetadata(string name, CancellationToken cancellationToken)
        {
            // Simply use first value
            var vid = name.Split(' ', 2)[0];

            var url = $"{Config.AvdcServer}{AvdcApi.Metadata}{vid}";

            try
            {
                var contentStream = await GetStream(url, cancellationToken);
                return await _jsonSerializer.DeserializeFromStreamAsync<Metadata>(contentStream);
            }
            catch (Exception e)
            {
                Logger.LogError($"[AVDC] GetMetadata failed: {e.Message}");
                return new Metadata();
            }
        }

        private async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            Logger.LogDebug($"[AVDC] HTTP request to: {url}");

            cancellationToken.ThrowIfCancellationRequested();

            var httpClient = _httpClientFactory.CreateClient();
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