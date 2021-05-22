using Newtonsoft.Json;
using OpcUaWebDashboard.Models;
using OpcUaWebDashboard.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace OpcUaWebDashboard
{
    /// <summary>
    /// Class to describe the shopfloor characteristics
    /// </summary>
    public class ShopfloorProperties
    {
        [JsonProperty]
        public string Type;

        [JsonProperty]
        public string Domain;

        public ShopfloorProperties()
        {
        }
    }

    /// <summary>
    /// The location of the node.
    /// </summary>
    public class LocationDescription
    {
        [JsonProperty]
        public string City;

        [JsonProperty]
        public string Country;

        [JsonProperty]
        public double Latitude;

        [JsonProperty]
        public double Longitude;

        public LocationDescription()
        {
            City = "";
            Country = "";
            Latitude = 0;
            Longitude = 0;
        }
    }

    /// <summary>
    /// The pushpin coordinates in px from the top and from the left border of the image.
    /// </summary>
    public class ContosoPushPinCoordinates
    {
        [JsonProperty]
        public double Top;

        [JsonProperty]
        public double Left;
    }

    /// <summary>
    /// Class to parse description common for all topology nodes (global, factory, production line, station)
    /// </summary>
    public class ContosoTopologyDescriptionCommon
    {
        [JsonProperty]
        public string Name;

        [JsonProperty]
        public string Description;

        [JsonProperty]
        public string Image;

        [JsonProperty]
        public LocationDescription Location;

        [JsonProperty]
        public ContosoPushPinCoordinates ImagePushpin;

        [JsonProperty]
        public string GroupLabel;

        [JsonProperty]
        public string GroupLabelLength;

        [JsonProperty (PropertyName ="Shopfloor")]
        public ShopfloorProperties ShopfloorProperties;


        /// <summary>
        /// Ctor for the information common to all topology node descriptions.
        /// </summary>
        public ContosoTopologyDescriptionCommon()
        {
            Name = "";
            Description = "";
            Image = "";
            ShopfloorProperties = new ShopfloorProperties();
        }
    }

    /// <summary>
    /// Class for the top level (global) topology description.
    /// </summary>
    public class TopologyDescription : ContosoTopologyDescriptionCommon
    {
        [JsonProperty]
        public List<FactoryDescription> Factories;

        [JsonProperty]
        public List<StationDescription> Stations;
    }

    /// <summary>
    /// Class to define information of child nodes in the topology
    /// </summary>
    public class ContosoChildInfo
    {
        // Key to address the node in the toplogy. For OPC UA servers (Station), this it the Application URI of the OPC UA application.
        public string Key { get; set; }

        // For OPC UA Nodes this containes the OPC UA nodeId.
        public string SubKey { get; set; }

        // Status of the topology node, shown in the UX.
        public string Status { get; set; }

        // Name of the topology node. shown in the UX.
        public string Name { get; set; }

        // Description of the toplogy node, shown in the UX.
        public string Description { get; set; }

        // City the topology node resides, shown in the UX.
        public string City { get; set; }

        // Last value
        public string Last { get; set; }

        // Unit of node value
        public string Unit { get; set; }

        // Pushpin coordinates
        public ContosoPushPinCoordinates ImagePushpin { get; set; }

        // Group Label
        public string GroupLabel { get; set; }

        // Group Label
        public string GroupLabelLength { get; set; }

        // Geo location the toplogy node resides. Could be used in the UX.
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool Visible { get; set; }

        /// <summary>
        /// Ctor for child node information of topology nodes (non OPC UA nodes) using values.
        /// </summary>
        public ContosoChildInfo(string key, string name, string description, string city, double latitude, double longitude, bool visible, ContosoPushPinCoordinates imagePushpin, string groupLabel, string groupLabelLength)
        {
            Key = key;
            SubKey = "null";
            Name = name;
            Description = description;
            City = city;
            Latitude = latitude;
            Longitude = longitude;
            Visible = visible;
            ImagePushpin = imagePushpin;
            GroupLabel = groupLabel;
            GroupLabelLength = groupLabelLength;
        }

        /// <summary>
        /// Ctor for child node information of OPC UA nodes.
        /// </summary>

        public ContosoChildInfo(string key, string subKey, string name, string description, string city, double latitude, double longitude, bool visible, string last, string unit, ContosoPushPinCoordinates imagePushpin, string groupLabel, string groupLabelLength)
        {
            Key = key;
            SubKey = subKey;
            Name = name;
            Description = description;
            City = city;
            Latitude = latitude;
            Longitude = longitude;
            Visible = visible;
            Last = last;
            Unit = unit != null ? unit : "";
            ImagePushpin = imagePushpin;
            GroupLabel = groupLabel;
            GroupLabelLength = groupLabelLength;
        }
    }

    /// <summary>
    /// For Contoso the topology is organized as a tree. Below the TopologyRoot (which is the global company view) there are "Factory" children,
    /// those have "ProductionLine" children, which have "Station" children. These "Station"s are
    /// OPC UA server systems, which contain OPC UA Nodes.
    /// </summary>
    public class ContosoTopology : TopologyTree
    {
        /// <summary>
        /// Represents the full topology of the company
        /// </summary>
        public static ContosoTopology Topology = new ContosoTopology(Directory.GetCurrentDirectory() + "/Topology/ContosoTopologyDescriptionStation.json");

        /// <summary>
        /// Holds the list of active sessions with all the relevant information.
        /// </summary>
        public static Dictionary<string, DashboardModel> SessionList = new Dictionary<string, DashboardModel>();

        /// <summary>
        /// Ctor for the Contoso topology.
        /// </summary>
        /// <param name="topologyDescriptionFilename"></param>
        public ContosoTopology(string topologyDescriptionFilename)
        {
            TopologyDescription topologyDescription;

            // Read the JSON equipment topology description.
            using (StreamReader topologyDescriptionStream = File.OpenText(topologyDescriptionFilename))
            {
                JsonSerializer serializer = new JsonSerializer();
                topologyDescription = (TopologyDescription)serializer.Deserialize(topologyDescriptionStream, typeof(TopologyDescription));
            }

            // Build the topology tree, start with the global level.
            ContosoTopologyNode rootNode = new ContosoTopologyNode("TopologyRoot", "Global", "Contoso", topologyDescription);
            TopologyRoot = rootNode;

            // Factory is the top level
            if (topologyDescription.Factories != null)
            {
                // There must be at least one factory.
                if (topologyDescription.Factories.Count == 0)
                {
                    IndexOutOfRangeException indexOutOfRange = new IndexOutOfRangeException("There must be at least one factory defined.");
                    throw indexOutOfRange;
                }

                // Iterate through the whole description level by level.
                foreach (var factoryDescription in topologyDescription.Factories)
                {
                    // Add it to the tree.
                    Factory factory = new Factory(factoryDescription);
                    factory.ShopfloorDomain = factory.ShopfloorDomain ?? rootNode.ShopfloorDomain;
                    factory.ShopfloorType = factory.ShopfloorType ?? rootNode.ShopfloorType;
                    AddChild(TopologyRoot.Key, factory);

                    // There must be at least one production line or one station, but not together.
                    if ((factoryDescription?.ProductionLines.Count == 0) && (factoryDescription?.Stations.Count == 0))
                    {
                        string message = String.Format("There must be at least one production line or station defined for factory '{0}'.", factory.Name);
                        IndexOutOfRangeException indexOutOfRange = new IndexOutOfRangeException(message);
                        throw indexOutOfRange;
                    }
                    else if ((factoryDescription.ProductionLines.Count > 0) && (factoryDescription.Stations.Count > 0))
                    {
                        string message = String.Format("There can not be together children type of ProductionLine and Station for factory '{0}'.", factory.Name);
                        IndexOutOfRangeException indexOutOfRange = new IndexOutOfRangeException(message);
                        throw indexOutOfRange;
                    }

                    if (factoryDescription.ProductionLines.Count > 0)
                    {
                        // Handle all production lines.
                        foreach (var productionLineDescription in factoryDescription.ProductionLines)
                        {
                            // Add it to the tree.
                            ProductionLine productionLine = new ProductionLine(productionLineDescription);
                            productionLine.Location = productionLine.Location ?? factory.Location;
                            productionLine.ShopfloorDomain = productionLine.ShopfloorDomain ?? factory.ShopfloorDomain;
                            productionLine.ShopfloorType = productionLine.ShopfloorType ?? factory.ShopfloorType;
                            AddChild(factory.Key, productionLine);

                            // There must be at least one station.
                            if (productionLineDescription.Stations.Count == 0)
                            {
                                string message = String.Format("There must be at least one station defined for production line '{0}' in factory '{1}'.", productionLine.Name, factory.Name);
                                IndexOutOfRangeException indexOutOfRange = new IndexOutOfRangeException(message);
                                throw indexOutOfRange;
                            }

                            // Handle all stations (a station is running an OPC UA server).
                            foreach (var stationDescription in productionLineDescription.Stations)
                            {
                                // Add it to the tree.
                                Station station = new Station(productionLine.ShopfloorDomain, productionLine.ShopfloorType, stationDescription);
                                AddChild(productionLine.Key, station);
                            }
                        }
                    }
                    else if (factoryDescription.Stations.Count > 0)
                    {
                        // Handle all stations (a station is running an OPC UA server).
                        foreach (var stationDescription in factoryDescription.Stations)
                        {
                            // Add it to the tree.
                            Station station = new Station(factory.ShopfloorDomain, factory.ShopfloorType, stationDescription);
                            AddChild(factory.Key, station);
                        }
                    }
                }
            }
            else if (topologyDescription.Stations != null)      //Station is the top level
            {
                // There must be at least one station.
                if (topologyDescription.Stations.Count == 0)
                {
                    IndexOutOfRangeException indexOutOfRange = new IndexOutOfRangeException("There must be at least one station defined.");
                    throw indexOutOfRange;
                }

                // Handle all stations (a station is running an OPC UA server).
                foreach (var stationDescription in topologyDescription.Stations)
                {
                    // Add it to the tree.
                    Station station = new Station(topologyDescription.ShopfloorProperties.Domain, topologyDescription.ShopfloorProperties.Type, stationDescription);
                    //station.Location.City = topologyDescription.Location.City;
                    //station.Location.Country = topologyDescription.Location.Country;
                    //station.Location.Latitude = topologyDescription.Location.Latitude;
                    //station.Location.Longitude = topologyDescription.Location.Longitude;

                    station.ShopfloorDomain = stationDescription.ShopfloorProperties.Domain ;
                    station.ShopfloorType = stationDescription.ShopfloorProperties.Type;

                    AddChild(TopologyRoot.Key, station);
                }
            }
        }

        /// <summary>
        /// Returns information for all children of the given topology node.
        /// This information will be shown in the UX.
        /// </summary>
        /// <param name="parentKey"></param>
        public List<ContosoChildInfo> GetChildrenInfo(string parentKey)
        {
            List<ContosoChildInfo> childrenInfo = new List<ContosoChildInfo>();

            // Update type of children for the given key.
            ContosoTopologyNode parent = (ContosoTopologyNode)TopologyTable[parentKey];
            Type childrenType = null;
            Type parentType = parent.GetType();

            if (parentType == typeof(Station))
            {
                childrenType = typeof(ContosoOpcUaNode);
            }
            else
            {
                if (parent.GetChildren().Count() > 0)
                {
                    var keyNode = parent.GetChildren().First();
                    childrenType = TopologyTable[keyNode].GetType();
                }
                else
                {
                    childrenType = typeof(Station);
                }
            }

            // Prepare the list with the child objects for the view.
            if (childrenType == typeof(ContosoOpcUaNode))
            {
                Station station = (Station)TopologyTable[parentKey];
                foreach (ContosoOpcUaNode opcUaNode in station.NodeList)
                {
                    ContosoChildInfo childInfo = new ContosoChildInfo(station.Key,
                                                                      opcUaNode.NodeId,
                                                                      opcUaNode.SymbolicName,
                                                                      opcUaNode.SymbolicName,
                                                                      station.Location.City,
                                                                      station.Location.Latitude,
                                                                      station.Location.Longitude,
                                                                      opcUaNode.Visible,
                                                                      opcUaNode.LastValueToUxString(),
                                                                      opcUaNode.Units,
                                                                      opcUaNode.ImagePushpin,
                                                                      opcUaNode.GroupLabel,
                                                                      opcUaNode.GroupLabelLength);
                    childrenInfo.Add(childInfo);
                }
            }
            else
            {
                var childrenKeys = ((ContosoTopologyNode)TopologyTable[parentKey]).GetChildren();
                foreach (string key in childrenKeys)
                {
                    if (childrenType == typeof(Factory))
                    {
                        Factory factory = (Factory)TopologyTable[key];
                        ContosoChildInfo dashboardChild = new ContosoChildInfo(factory.Key, factory.Name, factory.Description,
                                                                    factory.Location.City, factory.Location.Latitude, factory.Location.Longitude, true, factory.ImagePushpin, factory.GroupLabel, factory.GroupLabelLength);
                        childrenInfo.Add(dashboardChild);
                    }
                    if (childrenType == typeof(ProductionLine))
                    {
                        ProductionLine productionLine = (ProductionLine)TopologyTable[key];
                        ContosoChildInfo dashboardChild = new ContosoChildInfo(productionLine.Key, productionLine.Name, productionLine.Description,
                                                                    productionLine.Location.City, productionLine.Location.Latitude, productionLine.Location.Longitude, true, productionLine.ImagePushpin, productionLine.GroupLabel, productionLine.GroupLabelLength);
                        childrenInfo.Add(dashboardChild);
                    }
                    if (childrenType == typeof(Station))
                    {
                        Station station = (Station)TopologyTable[key];
                        ContosoChildInfo dashboardChild = new ContosoChildInfo(station.Key, station.Name, station.Description,
                                                                    station.Location.City, station.Location.Latitude, station.Location.Longitude, true, station.ImagePushpin,station.GroupLabel, station.GroupLabelLength);
                        childrenInfo.Add(dashboardChild);
                    }
                }
            }
            return childrenInfo;
        }

        /// <summary>
        /// Returns the root node of the topology.
        /// </summary>
        public ContosoTopologyNode GetRootNode()
        {
            return this.TopologyRoot as ContosoTopologyNode;
        }

        /// <summary>
        /// Returns a list of all nodes under the given node, with the given type and name.
        /// </summary>
        /// <returns>
        public List<string> GetAllChildren(string key, Type type, string name)
        {
            List<string> allChildren = new List<string>();
            foreach (var child in ((TopologyNode)TopologyTable[key]).GetChildren())
            {
                ContosoTopologyNode node = TopologyTable[child] as ContosoTopologyNode;
                if (node != null)
                {
                    if (node.GetType() == type && node.Name == name)
                    {
                        allChildren.Add(child);
                    }
                    if (((TopologyNode)TopologyTable[child]).ChildrenCount > 0)
                    {
                        allChildren.AddRange(GetAllChildren(child, type, name));
                    }
                }
            }
            return allChildren;
        }


        /// <summary>
        /// Constants used to populate new factory/production line.
        /// </summary>
        string _newFactoryName = Resources.NewFactoryName;
        string _newProductionLineName = Resources.NewProductionLineName;
        string _newStationName = Resources.NewStationName;
        const string _newFactoryImage = "newfactory.jpg";
        const string _newProductionLineImage = "assembly_floor.jpg";
        const string _newStationImage = "assembly_station.jpg";

        /// <summary>
        /// Get new factory node. Creates new node if new factory doesn't exist.
        /// </summary>
        public ContosoTopologyNode GetOrAddNewFactory()
        {
            List<string> newFactory = GetAllChildren(TopologyRoot.Key, typeof(Factory), _newFactoryName);
            if (newFactory.Count > 0)
            {
                // use existing new factory
                return (ContosoTopologyNode)TopologyTable[newFactory[0]];
            }
            // Add new factory to root
            FactoryDescription factoryDescription = new FactoryDescription();
            factoryDescription.Name = _newFactoryName;
            factoryDescription.Description = _newFactoryName;
            factoryDescription.Image = _newFactoryImage;
            factoryDescription.Guid = Guid.NewGuid().ToString();
            factoryDescription.Location.Latitude = 0;
            factoryDescription.Location.Longitude = 0;
            factoryDescription.ShopfloorProperties.Type = "Unknown";
            Factory factory = new Factory(factoryDescription);
            AddChild(TopologyRoot.Key, factory);
            return factory;
        }

        /// <summary>
        /// Get new production line node. Creates new node if new production line doesn't exist.
        /// </summary>
        public ContosoTopologyNode GetOrAddNewProductionLine()
        {
            ContosoTopologyNode newFactory = GetOrAddNewFactory();
            List<string> newProductionLine = GetAllChildren(newFactory.Key, typeof(ProductionLine), _newProductionLineName);
            if (newProductionLine.Count > 0)
            {
                // use existing new production line
                return (ContosoTopologyNode)TopologyTable[newProductionLine[0]];
            }
            // Add new production Line to existing factory
            ProductionLineDescription productionLineDescription = new ProductionLineDescription();
            productionLineDescription.Name = _newProductionLineName;
            productionLineDescription.Description = _newProductionLineName;
            productionLineDescription.Guid = Guid.NewGuid().ToString();
            productionLineDescription.Image = _newProductionLineImage;
            ProductionLine productionLine = new ProductionLine(productionLineDescription);
            productionLine.Location = newFactory.Location;
            AddChild(newFactory.Key, productionLine);
            return productionLine;
        }

        /// <summary>
        /// Add new stations to a new factory with a new production line in an unknown shopfloor domain.
        /// </summary>
        public void AddNewStations(List<string> opcUriList)
        {
            foreach (string opcUri in opcUriList)
            {
                try
                {
                    ContosoTopologyNode newProductionLine = GetOrAddNewProductionLine();
                    StationDescription desc = new StationDescription();
                    desc.Name = opcUri;
                    desc.Description = _newStationName;
                    desc.Image = _newStationImage;
                    desc.OpcUri = opcUri;
                    Station station = new Station("", null, desc);
                    TopologyNode node = station as TopologyNode;
                    AddChild(newProductionLine.Key, node);
                }
                catch
                {
                    Trace.TraceError("Failed to add station {0} to topology", opcUri);
                }
            }
        }

        /// <summary>
        /// Gets the OPC UA node for the station with the given key.
        /// </summary>
        public ContosoOpcUaNode GetOpcUaNode(string key, string nodeId)
        {
            ContosoOpcUaServer opcUaServer = TopologyTable[key] as ContosoOpcUaServer;
            if (opcUaServer != null)
            {
                return opcUaServer.GetOpcUaNode(nodeId) as ContosoOpcUaNode;
            }
            return null;
        }

        /// <summary>
        /// Get the Station topology node with the given key.
        /// </summary>
        public Station GetStation(string key)
        {
            return TopologyTable[key] as Station;
        }

    }
}
