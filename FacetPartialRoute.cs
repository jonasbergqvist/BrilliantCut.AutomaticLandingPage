using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Routing;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing.Segments;
using StructureMap;

namespace BrilliantCut.AutomaticLandingPage
{
    public class FacetPartialRoute : HierarchicalCatalogPartialRouter
    {
        private readonly FacetUrlService _facetUrlCreator;

        public FacetPartialRoute(Func<ContentReference> routeStartingPoint, CatalogContentBase commerceRoot,
            bool enableOutgoingSeoUri)
            : this(
            routeStartingPoint, 
            commerceRoot, 
            enableOutgoingSeoUri,
            ServiceLocator.Current.GetInstance<IContentLoader>(),
            ServiceLocator.Current.GetInstance<IRoutingSegmentLoader>(),
            ServiceLocator.Current.GetInstance<IContentVersionRepository>(), 
            ServiceLocator.Current.GetInstance<IUrlSegmentRouter>(),
            ServiceLocator.Current.GetInstance<IContentLanguageSettingsHandler>(),
            ServiceLocator.Current.GetInstance<FacetUrlService>())
        {
        }

        [DefaultConstructor]
        public FacetPartialRoute(Func<ContentReference> routeStartingPoint, CatalogContentBase commerceRoot,
            bool supportSeoUri, IContentLoader contentLoader, IRoutingSegmentLoader routingSegmentLoader,
            IContentVersionRepository contentVersionRepository, IUrlSegmentRouter urlSegmentRouter,
            IContentLanguageSettingsHandler contentLanguageSettingsHandler,
            FacetUrlService facetUrlCreator)
            : base(
                routeStartingPoint, commerceRoot, supportSeoUri, contentLoader, routingSegmentLoader,
                contentVersionRepository, urlSegmentRouter, contentLanguageSettingsHandler)
        {
            _facetUrlCreator = facetUrlCreator;
        }

        public override object RoutePartial(PageData content, SegmentContext segmentContext)
        {
            var routedContet = base.RoutePartial(content, segmentContext);

            var segmentPair = segmentContext.GetNextValue(segmentContext.RemainingPath);
            if (String.IsNullOrEmpty(segmentPair.Next))
            {
                return routedContet;
            }

            var facetNames = _facetUrlCreator.GetFacetModels().ToArray();

            var nextSegment = _facetUrlCreator.GetFacetValueWhenReadingUrl(facetNames, segmentPair.Next);
            if (String.IsNullOrEmpty(nextSegment))
            {
                return routedContet;
            }

            var routeFacets = segmentContext.RouteData.Values[FacetUrlService.RouteFacets] as ConcurrentDictionary<RouteFacetModel, HashSet<object>>;
            if (routeFacets == null)
            {
                segmentContext.RouteData.Values[FacetUrlService.RouteFacets] = new ConcurrentDictionary<RouteFacetModel, HashSet<object>>();
                routeFacets = (ConcurrentDictionary<RouteFacetModel, HashSet<object>>)segmentContext.RouteData.Values[FacetUrlService.RouteFacets];
            }

            AddFacetsToSegmentContext(routeFacets, segmentContext, facetNames, nextSegment, segmentPair.Remaining, null);
            return routedContet;
        }

        private void AddFacetsToSegmentContext(ConcurrentDictionary<RouteFacetModel, HashSet<object>> routeFacets, SegmentContext segmentContext, RouteFacetModel[] facetNames, string nextSegment, string remaining, RouteFacetModel currentFacet)
        {
            if (String.IsNullOrEmpty(nextSegment))
            {
                return;
            }

            var value = facetNames.FirstOrDefault(x => x.FacetName == nextSegment);
            if (value != null)
            {
                currentFacet = value;
                
            }
            else if (currentFacet != null)
            {
                var facetValue = _facetUrlCreator.GetFacetValueWhenReadingUrl(facetNames, nextSegment);

                routeFacets.AddOrUpdate(currentFacet,
                   (key) => new HashSet<object> { facetValue },
                   (key, list) =>
                   {
                       list.Add(facetValue);
                       return list;
                   });
            }

            segmentContext.RemainingPath = remaining;

            var segmentPair = segmentContext.GetNextValue(segmentContext.RemainingPath);
            nextSegment = _facetUrlCreator.GetFacetValueWhenReadingUrl(facetNames, segmentPair.Next);

            AddFacetsToSegmentContext(routeFacets, segmentContext, facetNames, nextSegment, segmentPair.Remaining, currentFacet);
        }
    }
}
