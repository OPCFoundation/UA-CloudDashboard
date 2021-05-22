using System.Collections.Generic;
using Newtonsoft.Json;

namespace OpcUaWebDashboard
{
    public class FactoryDescription : ContosoTopologyDescriptionCommon
    {
        [JsonProperty]
        public string Guid;

        [JsonProperty]
        public List<ProductionLineDescription> ProductionLines;

        [JsonProperty]
        public List<StationDescription> Stations;

        public FactoryDescription()
        {
            Location = new LocationDescription();
            ProductionLines = new List<ProductionLineDescription>();
            Stations = new List<StationDescription>();
        }
    }

    /// <summary>
    /// Class to define a factory in the topology tree.
    /// </summary>
    public class Factory : ContosoTopologyNode
    {
        /// <summary>
        /// Ctor of a factory in the topology tree.
        /// </summary>
        /// <param name="factoryDescription">The topology description for the factory.</param>
        public Factory(FactoryDescription factoryDescription) : base(factoryDescription.Guid, factoryDescription.Name, factoryDescription.Description, factoryDescription)
        {
            Location = new Location();
            Location.City = factoryDescription.Location.City;
            Location.Country = factoryDescription.Location.Country;
            Location.Latitude = factoryDescription.Location.Latitude;
            Location.Longitude = factoryDescription.Location.Longitude;
        }
    }
}
