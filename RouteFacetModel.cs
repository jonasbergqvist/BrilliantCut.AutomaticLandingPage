using System;
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace BrilliantCut.AutomaticLandingPage
{
    public class RouteFacetModel : IDynamicData, IEquatable<RouteFacetModel>
    {
        public string FacetType { get; set; }
        
        public string FacetPath { get; set; }
        
        public string FacetName { get; set; }

        public Identity Id { get; set; }

        public bool Equals(RouteFacetModel other)
        {
            return FacetName.Equals(other.FacetName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
