using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace BrilliantCut.AutomaticLandingPage
{
    public static class HtmlHelperExtensions
    {
        public static MvcHtmlString FacetContentUrl(this HtmlHelper htmlHelper, IContent currentContent, string facetType, string facetKeyPath, string facetKey, object facetValue)
        {
            var originalRouteFacets = htmlHelper.ViewContext.RouteData.Values[FacetUrlService.RouteFacets] as ConcurrentDictionary<RouteFacetModel, HashSet<object>>;

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

            var url = ServiceLocator.Current.GetInstance<UrlResolver>().GetUrl(currentContent.ContentLink, language);
            if (url.Length > 1)
            {
                url = ServiceLocator.Current.GetInstance<FacetUrlService>().GetFilterPath(url.Substring(0, url.Length - 1), routeFacets);
            }

            return new MvcHtmlString(url);
        }

        public static MvcHtmlString ContentUrl(this HtmlHelper htmlHelper, IContent currentContent)
        {
            string language = null;
            var languageContent = currentContent as ILocalizable;
            if (languageContent != null)
            {
                language = languageContent.Language.Name;
            }

            var url = ServiceLocator.Current.GetInstance<UrlResolver>().GetUrl(currentContent.ContentLink, language);

            return new MvcHtmlString(url);
        }
    }
}
