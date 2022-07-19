using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Profiles.Delay
{
    public interface IDelayProfileService
    {
        DelayProfile Add(DelayProfile profile);
        DelayProfile Update(DelayProfile profile);
        void Delete(int id);
        List<DelayProfile> All();
        DelayProfile Get(int id);
        DelayProfile GetDefaultProfile();
        List<DelayProfile> AllForTag(int tagId);
        List<DelayProfile> AllForTags(HashSet<int> tagIds);
        DelayProfile BestForTags(HashSet<int> tagIds);
        List<DelayProfile> Reorder(int id, int? afterId);
    }

    public class DelayProfileService : IDelayProfileService
    {
        private readonly IDelayProfileRepository _repo;
        private readonly ICached<DelayProfile> _bestForTagsCache;
        private readonly List<IDownloadProtocol> _downloadProtocols;

        public DelayProfileService(IDelayProfileRepository repo,
                                   IEnumerable<IDownloadProtocol> downloadProtocols,
                                   ICacheManager cacheManager)
        {
            _repo = repo;
            _downloadProtocols = downloadProtocols.ToList();
            _bestForTagsCache = cacheManager.GetCache<DelayProfile>(GetType(), "best");
        }

        public DelayProfile Add(DelayProfile profile)
        {
            profile.Order = _repo.Count();

            var result = _repo.Insert(profile);
            _bestForTagsCache.Clear();

            return result;
        }

        public DelayProfile Update(DelayProfile profile)
        {
            var result = _repo.Update(profile);
            _bestForTagsCache.Clear();
            return result;
        }

        public void Delete(int id)
        {
            _repo.Delete(id);

            var all = All().OrderBy(d => d.Order).ToList();

            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id == 1)
                {
                    continue;
                }

                all[i].Order = i + 1;
            }

            _repo.UpdateMany(all);
            _bestForTagsCache.Clear();
        }

        public List<DelayProfile> All()
        {
            return _repo.All().Select(x => AddMissingItems(x)).ToList();
        }

        public DelayProfile Get(int id)
        {
            return AddMissingItems(_repo.Get(id));
        }

        public DelayProfile GetDefaultProfile()
        {
            var standardTypes = new[] { typeof(UsenetDownloadProtocol), typeof(TorrentDownloadProtocol) };

            var others = _downloadProtocols.Where(x => !standardTypes.Contains(x.GetType())).OrderBy(x => x.GetType().Name);

            var result = new DelayProfile();
            result.Items.AddRange(others.Select(x => GetProtocolItem(x, true)));

            return result;
        }

        public List<DelayProfile> AllForTag(int tagId)
        {
            return All().Where(r => r.Tags.Contains(tagId))
                        .ToList();
        }

        public List<DelayProfile> AllForTags(HashSet<int> tagIds)
        {
            return All().Where(r => r.Tags.Intersect(tagIds).Any() || r.Tags.Empty()).ToList();
        }

        public DelayProfile BestForTags(HashSet<int> tagIds)
        {
            var key = "-" + tagIds.Select(v => v.ToString()).Join(",");
            return _bestForTagsCache.Get(key, () => FetchBestForTags(tagIds), TimeSpan.FromSeconds(30));
        }

        private DelayProfile FetchBestForTags(HashSet<int> tagIds)
        {
            return All()
                .Where(r => r.Tags.Intersect(tagIds).Any() || r.Tags.Empty())
                .OrderBy(d => d.Order).First();
        }

        public List<DelayProfile> Reorder(int id, int? afterId)
        {
            var all = All().OrderBy(d => d.Order)
                           .ToList();

            var moving = all.SingleOrDefault(d => d.Id == id);
            var after = afterId.HasValue ? all.SingleOrDefault(d => d.Id == afterId) : null;

            if (moving == null)
            {
                // TODO: This should throw
                return all;
            }

            var afterOrder = GetAfterOrder(moving, after);
            var afterCount = afterOrder + 2;
            var movingOrder = moving.Order;

            foreach (var delayProfile in all)
            {
                if (delayProfile.Id == 1)
                {
                    continue;
                }

                if (delayProfile.Id == id)
                {
                    delayProfile.Order = afterOrder + 1;
                }
                else if (delayProfile.Id == after?.Id)
                {
                    delayProfile.Order = afterOrder;
                }
                else if (delayProfile.Order > afterOrder)
                {
                    delayProfile.Order = afterCount;
                    afterCount++;
                }
                else if (delayProfile.Order > movingOrder)
                {
                    delayProfile.Order--;
                }
            }

            _repo.UpdateMany(all);

            return All();
        }

        private int GetAfterOrder(DelayProfile moving, DelayProfile after)
        {
            if (after == null)
            {
                return 0;
            }

            if (moving.Order < after.Order)
            {
                return after.Order - 1;
            }

            return after.Order;
        }

        private DelayProfile AddMissingItems(DelayProfile profile)
        {
            var missing = _downloadProtocols.Where(x => !profile.Items.Any(i => i.Protocol == x.GetType().Name));
            profile.Items.AddRange(missing.Select(x => GetProtocolItem(x, false)));

            var protocolNames = _downloadProtocols.Select(x => x.GetType().Name).ToList();
            profile.Items.RemoveAll(x => !protocolNames.Contains(x.Protocol));
            return profile;
        }

        private DelayProfileProtocolItem GetProtocolItem(IDownloadProtocol protocol, bool allowed)
        {
            return new DelayProfileProtocolItem
            {
                Name = protocol.GetType().Name.Replace("DownloadProtocol", ""),
                Protocol = protocol.GetType().Name,
                Allowed = allowed
            };
        }
    }
}
