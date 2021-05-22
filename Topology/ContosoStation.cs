using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso
{
    public class StationDescription : ContosoTopologyDescriptionCommon
    {
        [JsonProperty]
        public string OpcUri;

        [JsonProperty]
        public string EndpointUrl;

        [JsonProperty]
        public bool UseSecurity;

        [JsonProperty]
        public List<ContosoOpcNodeDescription> OpcNodes;

        public StationDescription()
        {
            OpcNodes = new List<ContosoOpcNodeDescription>();
        }
    }

    /// <summary>
    /// Class to define a station in the topology tree.
    /// </summary>
    public class Station : ContosoOpcUaServer
    {
        public string _shopfloorDomain;

        /// <summary>
        /// Property to allow setting the shopfloor domain the server is in.
        /// </summary>
        public new string ShopfloorDomain
        {
            set
            {
                // When a shopfloor domain is used, we append it separated with a colon to the application URI.
                // The publisher needs to be configured to add a colon and the shopfloor domain to all telemetry ingested.
                if (value != null)
                {
                    ApplicationUri += (":" + value);
                }
                _shopfloorDomain = value;
            }
            get
            {
                return _shopfloorDomain;
            }
        }

        /// <summary>
        /// Ctor of the node using topology description data.
        /// </summary>
        public Station(string shopfloorDomain, string shopfloorType, StationDescription stationDescription) :
            base(shopfloorDomain, shopfloorType, stationDescription.OpcUri, stationDescription.EndpointUrl, stationDescription.Name, stationDescription.UseSecurity, stationDescription.Description, stationDescription)
        {
            Location = new Location();

            foreach (var opcNode in stationDescription.OpcNodes)
            {
                // Add the OPC UA node to this station.
                AddOpcServerNode(opcNode);
            }
        }
    }
}
