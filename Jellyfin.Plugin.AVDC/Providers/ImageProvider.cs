using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AVDC.Providers
{
    public class ImageProvider : BaseProvider, IRemoteImageProvider, IHasOrder
    {
        public ImageProvider(IHttpClientFactory httpClientFactory,
            IJsonSerializer jsonSerializer,
            ILogger<ImageProvider> logger) : base(httpClientFactory, jsonSerializer, logger)
        {
            // Empty
        }

        public int Order => 1;
        public string Name => Plugin.Instance.Name;

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            Logger.LogInformation("[AVDC] GetImages for video: {Name}", item.Name);

            var vid = item.GetProviderId(Name);
            if (string.IsNullOrWhiteSpace(vid)) vid = Utility.ExtractVid(item.FileNameWithoutExtension);

            var m = await GetMetadata(vid, cancellationToken);
            if (m == null || string.IsNullOrWhiteSpace(m.Vid)) return new List<RemoteImageInfo>();

            return new List<RemoteImageInfo>
            {
                new()
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = $"{Config.Server}{ApiPath.PrimaryImage}{m.Vid}"
                },
                new()
                {
                    ProviderName = Name,
                    Type = ImageType.Backdrop,
                    Url = $"{Config.Server}{ApiPath.BackdropImage}{m.Vid}"
                }
            };
        }

        public bool Supports(BaseItem item)
        {
            return item is Movie;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Backdrop
            };
        }
    }
}