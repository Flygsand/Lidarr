using System.Collections.Generic;
using System.Linq;
using Lidarr.Http.REST;
using NzbDrone.Core.Profiles.Releases;

namespace Lidarr.Api.V1.Profiles.Release
{
    public class ReleaseProfileResource : RestResource
    {
        public bool Enabled { get; set; }
        public List<string> Required { get; set; }
        public List<string> Ignored { get; set; }
        public int IndexerId { get; set; }
        public HashSet<int> Tags { get; set; }

        public ReleaseProfileResource()
        {
            Tags = new HashSet<int>();
        }
    }

    public static class RestrictionResourceMapper
    {
        public static ReleaseProfileResource ToResource(this ReleaseProfile model)
        {
            if (model == null)
            {
                return null;
            }

            return new ReleaseProfileResource
            {
                Id = model.Id,

                Enabled = model.Enabled,
                Required = model.Required,
                Ignored = model.Ignored,
                IndexerId = model.IndexerId,
                Tags = new HashSet<int>(model.Tags)
            };
        }

        public static ReleaseProfile ToModel(this ReleaseProfileResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new ReleaseProfile
            {
                Id = resource.Id,

                Enabled = resource.Enabled,
                Required = resource.Required,
                Ignored = resource.Ignored,
                IndexerId = resource.IndexerId,
                Tags = new HashSet<int>(resource.Tags)
            };
        }

        public static List<ReleaseProfileResource> ToResource(this IEnumerable<ReleaseProfile> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
