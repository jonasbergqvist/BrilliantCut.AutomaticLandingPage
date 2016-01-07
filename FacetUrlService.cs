using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EPiServer.Data.Dynamic;
using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;

namespace BrilliantCut.AutomaticLandingPage
{
    [ServiceConfiguration(Lifecycle = ServiceInstanceScope.Singleton)]
    public class FacetUrlService
    {
        public const string RouteFacets = "routeFacets";
        private readonly DynamicDataStoreFactory _dynamicDataStoreFactory;
        private readonly ISynchronizedObjectInstanceCache _objectInstanceCache;

        public FacetUrlService(DynamicDataStoreFactory dynamicDataStoreFactory, ISynchronizedObjectInstanceCache objectInstanceCache)
        {
            _dynamicDataStoreFactory = dynamicDataStoreFactory;
            _objectInstanceCache = objectInstanceCache;
        }

        public IEnumerable<RouteFacetModel> GetFacetModels()
        {
            var facetNames = GetCachedFacetNames();
            if (facetNames != null)
            {
                return facetNames;
            }

            var routingFacetNameStore = GetRoutingFacetNameStore();
            var allRouteFacetModels = routingFacetNameStore.LoadAll<RouteFacetModel>();

            var cacheKey = GetCacheName();
            _objectInstanceCache.Insert(cacheKey, allRouteFacetModels, new CacheEvictionPolicy(new string[0]));

            return allRouteFacetModels;
        }

        internal string GetFilterPath(string partialVirtualPath, IDictionary<RouteFacetModel, HashSet<object>> routeFacets)
        {
            var path = new StringBuilder(partialVirtualPath);

            var routeFacetKeys = routeFacets.Keys.OrderBy(x => x.FacetName);
            foreach (var routeFacetKey in routeFacetKeys)
            {
                HashSet<object> keyValues;
                if (routeFacets.TryGetValue(routeFacetKey, out keyValues))
                {
                    SaveIfNotExist(routeFacetKey);
                    path.Append(String.Concat("/", routeFacetKey.FacetName));

                    var keyValueStrings = keyValues.Select(x => x.ToString()).OrderBy(x => x);
                    foreach (var keyValueString in keyValueStrings)
                    {
                        var facetValue = GetFacetValueWhenCreatingUrl(keyValueString);
                        path.Append(String.Concat("/", facetValue));
                    }
                }
            }

            return path.ToString();
        }

        internal string GetFacetValueWhenReadingUrl(IEnumerable<RouteFacetModel> facetNames, string originalName)
        {
            var possibleProblems = facetNames.Where(x => x.FacetName.EndsWith(originalName));
            if (!possibleProblems.Any())
            {
                return originalName;
            }

            var modifiedName = originalName;
            while (modifiedName.Length > 0)
            {
                modifiedName = modifiedName.Substring(1);
                if (!facetNames.Any(x => x.FacetName.EndsWith(originalName)))
                {
                    return modifiedName;
                }
            }

            return originalName;
        }

        private string GetFacetValueWhenCreatingUrl(string originalName)
        {
            var facetNames = GetFacetModels();
            return GetFacetValueWhenCreatingUrl(facetNames, originalName);
        }

        private static string GetFacetValueWhenCreatingUrl(IEnumerable<RouteFacetModel> facetNames, string originalName)
        {
            if (facetNames == null || !facetNames.Any(x => x.FacetName == originalName))
            {
                return originalName;
            }

            return GetFacetValueWhenCreatingUrl(facetNames, String.Concat("f", originalName));
        }

        private void SaveIfNotExist(RouteFacetModel facetName)
        {
            var facetNames = GetFacetModels();
            if (facetNames != null && facetNames.Any(x => x.FacetName == facetName.FacetName))
            {
                return;
            }

            var routingFacetNameStore = GetRoutingFacetNameStore();
            routingFacetNameStore.Save(facetName);
            ClearFacetNamesCache();
        }

        private IEnumerable<RouteFacetModel> GetCachedFacetNames()
        {
            return _objectInstanceCache.Get(GetCacheName()) as IEnumerable<RouteFacetModel>;
        }

        private void ClearFacetNamesCache()
        {
            _objectInstanceCache.Remove(GetCacheName());
        }

        private static string GetCacheName()
        {
            return "bc:routingfacetnames";
        }

        private DynamicDataStore GetRoutingFacetNameStore()
        {
            const string routingFacetNames = "RoutingFacetNames";
            return _dynamicDataStoreFactory.GetStore(routingFacetNames) ??
                _dynamicDataStoreFactory.CreateStore(routingFacetNames, typeof(RouteFacetModel));
        }
    }
}