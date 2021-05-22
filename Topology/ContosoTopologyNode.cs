using System;

namespace OpcUaWebDashboard
{
    /// <summary>
    /// The location of the node.
    /// </summary>
    public class Location
    {
        /// <summary>
        /// The city where the factory is located.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// The country where the factory is located.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// The latitude of the geolocation of the factory.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude of the geolocation of the factory.
        /// </summary>
        public double Longitude { get; set; }
    }

    /// <summary>
    /// Class to define a Contoso specific node in the topology. Each Contoso node has a status, name and description, as well
    /// as certain performance targets and telemetry time series for OEE/KPI.
    /// </summary>
    public class ContosoTopologyNode : TopologyNode
    {

        /// <summary>
        /// Define the default shopfloor type
        /// </summary>
        private const string _defaultShopfloorType = "Simulation";

        /// <summary>
        /// Specify the aggregation views
        /// </summary>
        public enum AggregationView
        {
            Last = 0,
            Hour = 1,
            Day = 2,
            Week = 3
        }

        /// <summary>
        /// Default image for the node if nothing is configured.
        /// </summary>
        private string _defaultImage = "microsoft.jpg";

        /// <summary>
        /// Name of the topology node.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the topology node.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Location or the topology node.
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// Shopfloor domain of the topology node.
        /// </summary>
        public string ShopfloorDomain { get; set; }

        /// <summary>
        /// Shopfloor type of the topology node.
        /// </summary>
        public string ShopfloorType { get; set; }

        /// <summary>
        /// Path to an image showing this topology node.
        /// </summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// Image pushpin coordinates.
        /// </summary>
        public ContosoPushPinCoordinates ImagePushpin { get; set; }

        /// <summary>
        /// Group Label.
        /// </summary>
        public string GroupLabel { get; set; }

        /// <summary>
        /// Group Label Length.
        /// </summary>
        public string GroupLabelLength { get; set; }


        /// <summary>
        /// Ctor for the topology node using values.
        /// </summary>
        public ContosoTopologyNode(string key, string name, string description, string domain, string shopfloorType, ContosoPushPinCoordinates imagePushpin, string groupLabel, string groupLabelLength) : base(key)
        {
            Name = name;
            Description = description;
            ImagePath = "/Content/img/" + _defaultImage;
            ImagePushpin = imagePushpin;
            GroupLabel = groupLabel;
            GroupLabelLength = groupLabelLength;
            if (shopfloorType != null)
            {
                ShopfloorType = shopfloorType;
            }
            else
            {
                ShopfloorType = _defaultShopfloorType;
            }
            ShopfloorDomain = domain;
        }

        /// <summary>
        /// Ctor for a topology node in the topology using the topology node description.
        /// </summary>
        public ContosoTopologyNode(string key, string name, string description, ContosoTopologyDescriptionCommon topologyDescription) : this(key, name, description, topologyDescription.ShopfloorProperties.Domain, topologyDescription.ShopfloorProperties.Type, topologyDescription.ImagePushpin, topologyDescription.GroupLabel, topologyDescription.GroupLabelLength)
        {
            if (topologyDescription == null)
            {
                throw new ArgumentException("topologyDescription must be a non-null value");
            }

            // Initialize image path.
            if (topologyDescription.Image != null && topologyDescription.Image != "")
            {
                ImagePath = "/Content/img/" + topologyDescription.Image;
            }
        }
    }
}
