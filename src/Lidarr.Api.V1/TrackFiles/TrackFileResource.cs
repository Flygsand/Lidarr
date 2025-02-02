using System;
using System.Linq;
using Lidarr.Http.REST;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace Lidarr.Api.V1.TrackFiles
{
    public class TrackFileResource : RestResource
    {
        public int ArtistId { get; set; }
        public int AlbumId { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime DateAdded { get; set; }
        public string SceneName { get; set; }
        public string ReleaseGroup { get; set; }
        public QualityModel Quality { get; set; }
        public int QualityWeight { get; set; }
        public MediaInfoResource MediaInfo { get; set; }

        public bool QualityCutoffNotMet { get; set; }
        public ParsedTrackInfo AudioTags { get; set; }
    }

    public static class TrackFileResourceMapper
    {
        private static int QualityWeight(QualityModel quality)
        {
            if (quality == null)
            {
                return 0;
            }

            int qualityWeight = Quality.DefaultQualityDefinitions.Single(q => q.Quality == quality.Quality).Weight;
            qualityWeight += quality.Revision.Real * 10;
            qualityWeight += quality.Revision.Version;
            return qualityWeight;
        }

        public static TrackFileResource ToResource(this TrackFile model)
        {
            if (model == null)
            {
                return null;
            }

            return new TrackFileResource
            {
                Id = model.Id,
                AlbumId = model.AlbumId,
                Path = model.Path,
                Size = model.Size,
                DateAdded = model.DateAdded,
                SceneName = model.SceneName,
                ReleaseGroup = model.ReleaseGroup,
                Quality = model.Quality,
                QualityWeight = QualityWeight(model.Quality),
                MediaInfo = model.MediaInfo.ToResource()
            };
        }

        public static TrackFileResource ToResource(this TrackFile model, NzbDrone.Core.Music.Artist artist, IUpgradableSpecification upgradableSpecification)
        {
            if (model == null)
            {
                return null;
            }

            return new TrackFileResource
            {
                Id = model.Id,

                ArtistId = artist.Id,
                AlbumId = model.AlbumId,
                Path = model.Path,
                Size = model.Size,
                DateAdded = model.DateAdded,
                SceneName = model.SceneName,
                ReleaseGroup = model.ReleaseGroup,
                Quality = model.Quality,
                QualityWeight = QualityWeight(model.Quality),
                MediaInfo = model.MediaInfo.ToResource(),
                QualityCutoffNotMet = upgradableSpecification.QualityCutoffNotMet(artist.QualityProfile.Value, model.Quality)
            };
        }
    }
}
