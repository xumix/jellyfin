using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Controller.TV;
using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests
{
    public class FFProbeProviderExternalAudioTests
    {
        [Theory]
        [InlineData(".", "sound*")]
        [InlineData("sound", "sound*")]
        [InlineData("Sound", "sound*, translation*")]
        [InlineData("Rus Sound", "*sound*, translation*")]
        public async Task AdditionalFiles_AddedToItem_InSameDir(string dir, string masks)
        {
            // Arrange
            var configurationManager = Substitute.For<IServerConfigurationManager>();
            configurationManager.GetConfiguration("encoding")
                .Returns(new EncodingOptions { ExternalAudioPathMasks = masks });
            var fileSystem = Substitute.For<IFileSystem>();
            var localizationManager = Substitute.For<ILocalizationManager>();

            var fileName = "video-9C788453-4980-42ED-8C2D-35B1F010A825.mkv";
            var audioFileName = "video-9C788453-4980-42ED-8C2D-35B1F010A825.mka";
            var fileWithDir = Path.Combine(dir, fileName);
            var fullPath = Path.GetFullPath(fileWithDir);
            var audioWithDir = Path.Combine(dir, audioFileName);
            var fullAudioPath = Path.GetFullPath(audioWithDir);

            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (File.Create(fileWithDir))
                {
                }

                using (File.Create(audioWithDir))
                {
                }

                var mediaSourceManager = Substitute.For<IMediaSourceManager>();
                mediaSourceManager.GetPathProtocol(fullPath).Returns(MediaProtocol.File);
                var libraryManager = Substitute.For<ILibraryManager>();
                var mediaEncoder = Substitute.For<IMediaEncoder>();

                mediaEncoder.GetMediaInfo(null, CancellationToken.None)
                    .ReturnsForAnyArgs(new MediaInfo());

                var probeProvider = new FFProbeProvider(Substitute.For<ILogger<FFProbeProvider>>(),
                    mediaSourceManager,
                    Substitute.For<IChannelManager>(),
                    Substitute.For<IIsoManager>(),
                    mediaEncoder,
                    Substitute.For<IItemRepository>(),
                    Substitute.For<IBlurayExaminer>(),
                    localizationManager,
                    Substitute.For<IApplicationPaths>(),
                    Substitute.For<IJsonSerializer>(),
                    Substitute.For<IEncodingManager>(),
                    configurationManager,
                    Substitute.For<ISubtitleManager>(),
                    Substitute.For<IChapterManager>(),
                    libraryManager);

                // For now there's no real way to inject these properly
                BaseItem.Logger = Substitute.For<ILogger<BaseItem>>();
                BaseItem.ConfigurationManager = configurationManager;
                BaseItem.LibraryManager = libraryManager;
                BaseItem.ProviderManager = Substitute.For<IProviderManager>();
                BaseItem.LocalizationManager = localizationManager;
                BaseItem.ItemRepository = Substitute.For<IItemRepository>();
                User.UserManager = Substitute.For<IUserManager>();
                BaseItem.FileSystem = fileSystem;
                BaseItem.UserDataManager = Substitute.For<IUserDataManager>();
                BaseItem.ChannelManager = Substitute.For<IChannelManager>();
                Video.LiveTvManager = Substitute.For<ILiveTvManager>();
                Folder.UserViewManager = Substitute.For<IUserViewManager>();
                UserView.TVSeriesManager = Substitute.For<ITVSeriesManager>();
                Folder.CollectionManager = Substitute.For<ICollectionManager>();
                BaseItem.MediaSourceManager = Substitute.For<IMediaSourceManager>();
                CollectionFolder.JsonSerializer = Substitute.For<IJsonSerializer>();
                AuthenticatedAttribute.AuthService = Substitute.For<IAuthService>();
                // Act

                var audio = new Audio { Path = fullPath };
                var info = await probeProvider.FetchAudioInfo(audio,
                    new MetadataRefreshOptions(Substitute.For<IDirectoryService>()),
                    CancellationToken.None);

                // Assert
                await mediaEncoder.Received()
                    .GetMediaInfo(Arg.Is<MediaInfoRequest>(r => r.PlayableStreamFileNames.Any(f => f == fullAudioPath)),
                        Arg.Any<CancellationToken>());
            }
            finally
            {
                File.Delete(fileWithDir);
                File.Delete(audioWithDir);
            }
        }
    }
}
