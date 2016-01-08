using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Core;
using EPiServer.Data.Dynamic;
using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace BrilliantCut.AutomaticLandingPage
{
    [ServiceConfiguration(Lifecycle = ServiceInstanceScope.Singleton)]
    public class FacetUrlService
    {
        public const string RouteFacets = "routeFacets";
        private readonly DynamicDataStoreFactory _dynamicDataStoreFactory;
        private readonly ISynchronizedObjectInstanceCache _objectInstanceCache;
        private readonly UrlResolver _urlResolver;

        public FacetUrlService(DynamicDataStoreFactory dynamicDataStoreFactory, ISynchronizedObjectInstanceCache objectInstanceCache, UrlResolver urlResolver)
        {
            _dynamicDataStoreFactory = dynamicDataStoreFactory;
            _objectInstanceCache = objectInstanceCache;
            _urlResolver = urlResolver;
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

        internal string GetUrl(IContent currentContent, RouteValueDictionary routeValues, string facetType, string facetKeyPath, string facetKey, object facetValue)
        {
            var originalRouteFacets = routeValues[RouteFacets] as ConcurrentDictionary<RouteFacetModel, HashSet<object>>;

            var routeFacets = new Dictionary<RouteFacetModel, HashSet<object>>();
            if (originalRouteFacets != null)
            {
                foreach (var routeFacetModel in originalRouteFacets.Keys)
                {
                    routeFacets.Add(routeFacetModel, new HashSet<object>());
                    foreach (var value in originalRouteFacets[routeFacetModel])
                    {
                        routeFacets[routeFacetModel].Add(value);
                    }
                }
            }

            var model = routeFacets.Select(x => x.Key).SingleOrDefault(x => x.FacetName == facetKey);
            if (model != null)
            {
                routeFacets[model].Add(facetValue);
            }
            else
            {
                model = new RouteFacetModel
                {
                    FacetName = facetKey,
                    FacetPath = facetKeyPath,
                    FacetType = facetType
                };
                routeFacets.Add(model, new HashSet<object> { facetValue });
            }

            string language = null;
            var languageContent = currentContent as ILocalizable;
            if (languageContent != null)
            {
                language = languageContent.Language.Name;
            }

            var url = _urlResolver.GetUrl(currentContent.ContentLink, language);
            return url.Length > 1 ? GetUrl(url.Substring(0, url.Length - 1), routeFacets) : url;
        }

        internal string GetUrl(string partialVirtualPath, IDictionary<RouteFacetModel, HashSet<object>> routeFacets)
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

        internal string GetFacetValue(IEnumerable<RouteFacetModel> facetNames, string originalName)
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