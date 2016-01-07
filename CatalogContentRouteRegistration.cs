using System.Web.Routing;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Mediachase.Commerce.Catalog;

namespace BrilliantCut.AutomaticLandingPage
{
    [ServiceConfiguration(Lifecycle = ServiceInstanceScope.Singleton)]
    public class CatalogContentRouteRegistration
    {
        private readonly IContentLoader _contentLoader;
        private readonly ReferenceConverter _referenceConverter;

        public CatalogContentRouteRegistration(IContentLoader contentLoader, ReferenceConverter referenceConverter)
        {
            _contentLoader = contentLoader;
            _referenceConverter = referenceConverter;
        }

        public void RegisterDefaultRoute()
        {
            RegisterDefaultRoute(false);
        }

        public void RegisterDefaultRoute(bool enableOutgoingSeoUri)
        {
            var commerceRootContent = _contentLoader.Get<CatalogContentBase>(_referenceConverter.GetRootLink());

            var pageLink = ContentReference.IsNullOrEmpty(SiteDefinition.Current.StartPage)
                ? SiteDefinition.Current.RootPage
                : SiteDefinition.Current.StartPage;

            RegisterRoute(pageLink, commerceRootContent, enableOutgoingSeoUri);
        }

        public void RegisterRoute(ContentReference pageLink, ContentReference catalogLink, bool enableOutgoingSeoUri)
        {
            var commerceRootContent = _contentLoader.Get<CatalogContentBase>(catalogLink);
            RegisterRoute(pageLink, commerceRootContent, enableOutgoingSeoUri);
        }

        public void RegisterRoute(ContentReference pageLink, CatalogContentBase catalogContentBase, bool enableOutgoingSeoUri)
        {
            RouteTable.Routes.RegisterPartialRouter(new FacetPartialRoute(() => pageLink, catalogContentBase, enableOutgoingSeoUri));
        }
    }
}
