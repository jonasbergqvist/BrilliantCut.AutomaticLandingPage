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
            var url = ServiceLocator.Current.GetInstance<FacetUrlService>().GetUrl(currentContent, htmlHelper.ViewContext.RouteData.Values, facetType, facetKeyPath, facetKey, facetValue);

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
