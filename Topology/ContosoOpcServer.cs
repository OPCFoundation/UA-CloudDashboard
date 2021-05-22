using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace OpcUaWebDashboard
{
    /// <summary>
    /// Contoso related base information for query
    /// </summary>
    public class ContosoOeeKpiOpCodeQueryInfo
    {
        public string AppUri { get; }
    }

    /// <summary>
    /// Operation for queries of node values.
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Conver‌​ters.StringEnumConve‌​rter))]
    public enum ContosoOpcNodeOpCode
    {
        Undefined = 0,
        Diff = 1,
        Avg = 2,
        Sum = 3,
        Last = 4,
        Count = 5,
        Max = 6,
        Min = 7,
        Const = 8,
        Nop = 9,
        SubMaxMin = 10,
        Timespan = 11
    };

    /// <summary>
    /// Class to parse the Contoso specific OPC UA node description.
    /// </summary>
    public class ContosoOpcNodeDescription
    {
        [JsonProperty]
        public string NodeId;

        [JsonProperty]
        public string ExpandedNodeId;

        [JsonProperty]
        public uint OpcSamplingInterval;

        [JsonProperty]
        public uint OpcPublishingInterval;

        [JsonProperty]
        public ContosoPushPinCoordinates ImagePushpin;

        [JsonProperty]
        public string GroupLabel;

        [JsonProperty]
        public string GroupLabelLength;

        [JsonProperty]
        public string SymbolicName;

        [JsonProperty]
        public List<string> Relevance;

        [DefaultValue(ContosoOpcNodeOpCode.Undefined)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public ContosoOpcNodeOpCode OpCode;

        [JsonProperty]
        public string Units;

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool Visible;

        [JsonProperty]
        public double? ConstValue;

        [JsonProperty]
        public double? Minimum;

        [JsonProperty]
        public double? Maximum;
    }

    /// <summary>
    /// Class for one data time.
    /// </summary>
    public class ContosoDataItem
    {
        /// <summary>
        /// Time the data item have been created at source.
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// The actual data value.
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// Default ctor of a data item.
        /// </summary>
        public ContosoDataItem()
        {
            Time = DateTime.MinValue;
            Value = null;
        }
        /// <summary>
        /// Ctor of a data item with actual data.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="data"></param>
        public ContosoDataItem(DateTime time, object value)
        {
            Time = time;
            Value = value;
        }
        /// <summary>
        /// Ctor of a data item for inherited ctor.
        /// </summary>
        /// <param name="dataItem"></param>
        protected ContosoDataItem(ContosoDataItem dataItem)
        {
            Time = dataItem.Time;
            Value = dataItem.Value;
        }
        /// <summary>
        /// Default add operation for data item.
        /// Overloaded in inherited classes
        /// </summary>
        /// <param name="dataItem"></param>
        public virtual void Add(ContosoDataItem dataItem)
        {
            double value = (double)Value;
            //Value += dataItem.Value;
            value += (double)dataItem.Value;
            Value = value;
            Time = new DateTime(Math.Max(Time.Ticks, dataItem.Time.Ticks), DateTimeKind.Utc);
        }
        /// <summary>
        /// Default add operation when adding a station data item.
        /// Overloaded in inherited classes. By default using Add.
        /// </summary>
        /// <param name="dataItem"></param>
        public virtual void AddStation(ContosoDataItem dataItem)
        {
            Add(dataItem);
        }
    }


    /// <summary>
    /// Class for Contoso OPC UA node information.
    /// </summary>
    public class ContosoOpcUaNode : OpcUaNode
    {
        /// <summary>
        /// The last value obtained by a query.
        /// </summary>
        public ContosoDataItem Last;

        /// <summary>
        /// The last value ingested to IoTHub.
        /// </summary>
        public string LastValue;

        /// <summary>
        /// The timestamp of the last value ingested to IoTHub.
        /// </summary>
        public string LastValueTimestamp;

        /// <summary>
        /// Physical unit for value, optional.
        /// </summary>
        public string Units { get; }

        /// <summary>
        /// Tag if node should be visible in Dashboard.
        /// </summary>
        public bool Visible { get; }

        /// <summary>
        /// Specifies the operation in the query to get the value of the node.
        /// </summary>
        public ContosoOpcNodeOpCode OpCode { get; set; }

        /// <summary>
        /// Const Value if OpCode Const is chosen.
        /// </summary>
        public double? ConstValue { get; set; }

        /// <summary>
        /// If the actual value falls below this value, an alert is generated.
        /// </summary>
        public double? Minimum { get; set; }

        /// <summary>
        /// If the actual value raises above Maximum, an alert is created.
        /// </summary>
        public double? Maximum { get; set; }

        /// <summary>
        /// Define Pushpin coordinates in px from the top and from the left border of the image.
        /// <summary>
        public ContosoPushPinCoordinates ImagePushpin { get; set; }

        /// <summary>
        /// Define group label.
        /// <summary>
        public string GroupLabel { get; set; }

        /// <summary>
        /// Define group label length.
        /// <summary>
        public string GroupLabelLength { get; set; }

        /// <summary>
        /// Ctor for a Contoso OPC UA node, specifying alert related information.
        /// </summary>
        public ContosoOpcUaNode(
            string opcUaNodeId,
            string opcUaSymbolicName,
            ContosoOpcNodeOpCode opCode,
            string units,
            bool visible,
            double? constValue,
            double? minimum,
            double? maximum,
            ContosoPushPinCoordinates imagePushpin,
            string groupLabel,
            string groupLabelLength
            )
            : base(opcUaNodeId, opcUaSymbolicName)
        {
            OpCode = opCode;
            Units = units;
            Visible = visible;
            ConstValue = constValue;
            Minimum = minimum;
            Maximum = maximum;
            Last = new ContosoDataItem();
            ImagePushpin = imagePushpin;
            GroupLabel = groupLabel;
            GroupLabelLength = groupLabelLength;
        }

        /// <summary>
        /// Ctor for a Contoso OPC UA node, using alert related descriptions.
        /// </summary>

        public ContosoOpcUaNode(
            string opcUaNodeId,
            string opcUaSymbolicName,
            ContosoOpcNodeDescription opcNodeDescription)
            : base(opcUaNodeId, opcUaSymbolicName)
        {
            OpCode = opcNodeDescription.OpCode;
            Units = opcNodeDescription.Units;
            Visible = opcNodeDescription.Visible;
            ConstValue = opcNodeDescription.ConstValue;
            Minimum = opcNodeDescription.Minimum;
            Maximum = opcNodeDescription.Maximum;

            Last = new ContosoDataItem();
            ImagePushpin = opcNodeDescription.ImagePushpin;
            GroupLabel = opcNodeDescription.GroupLabel;
            GroupLabelLength = opcNodeDescription.GroupLabelLength;
        }

        /// <summary>
        /// Formats the last value to display it in the dashboard UX.
        /// </summary>
        public string LastValueToUxString()
        {
            // Set the last value with a UX conform formatting
            bool boolValue;
            int intValue;
            double doubleValue;

            if (LastValue == null && Last.Value != null)
            {
                LastValue = Last.Value.ToString();
            }

            string value = LastValue == null ? "-" : LastValue;
            string uxString = string.Empty;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-en");

            if (!Boolean.TryParse(value, out boolValue))
            {
                if (!Int32.TryParse(value, out intValue))
                {
                    if (!Double.TryParse(value, NumberStyles.Float, culture, out doubleValue))
                    {
                        // stick with a string for all non parsable values
                        uxString = value;
                    }
                    else
                    {
                        uxString = doubleValue.ToString("F2");
                    }
                }
                else
                {
                    uxString = intValue.ToString();
                }
            }
            else
            {
                // very use case dependent, go with digits here
                uxString = boolValue ? "1" : "0";
            }
            return uxString;
        }
    }

    /// <summary>
    /// Class for Contoso OPC UA server information.
    /// </summary>
    public class ContosoOpcUaServer : OpcUaServer
    {
        /// <summary>
        /// Ctor for Contoso OPC UA server using the station description.
        /// </summary>
        public ContosoOpcUaServer(
            string shopfloorDomain,
            string shopfloorType,
            string uri,
            string url,
            string name,
            bool useSecurity,
            string description,
            StationDescription stationDescription)
            : base(shopfloorDomain, shopfloorType, uri, url, name, description, useSecurity, stationDescription)
        {
        }

        /// <summary>
        /// Adds an OPC UA Node to this OPC UA server topology node.
        /// </summary>
        public void AddOpcServerNode(
            string opcUaNodeId,
            string opcUaSymbolicName,
            ContosoOpcNodeOpCode opCode,
            string units,
            bool visible,
            double? constValue,
            double? minimum,
            double? maximum,
            ContosoPushPinCoordinates imagePushpin,
            string groupLabel,
            string groupLabelLength)
        {
            foreach (var node in NodeList)
            {
                if (OpCodeRequiresOpcUaNode(opCode) &&
                    node.NodeId == opcUaNodeId
                    )
                {
                    throw new Exception(string.Format("The OPC UA node with NodeId '{0}' and SymbolicName '{1}' does already exist. Please change.", opcUaNodeId, opcUaSymbolicName));
                }
            }
            ContosoOpcUaNode opcUaNodeObject = new ContosoOpcUaNode(
                opcUaNodeId,
                opcUaSymbolicName,
                opCode,
                units,
                visible,
                constValue,
                minimum,
                maximum,
                imagePushpin,
                groupLabel,
                groupLabelLength);

            NodeList.Add(opcUaNodeObject);
        }

        /// <summary>
        /// Adds an OPC UA Node to this OPC UA server topology node using the OPC UA node description.
        /// </summary>
        public void AddOpcServerNode(ContosoOpcNodeDescription opcUaNodeDescription)
        {
            foreach (var node in NodeList)
            {
                if (OpCodeRequiresOpcUaNode(opcUaNodeDescription.OpCode) &&
                    (node.NodeId == (opcUaNodeDescription.NodeId ?? opcUaNodeDescription.ExpandedNodeId))
                    )
                {
                    throw new Exception(string.Format("The OPC UA node with NodeId '{0}' and SymbolicName '{1}' does already exist for station '{2}'. Please change.",
                        opcUaNodeDescription.NodeId ?? opcUaNodeDescription.ExpandedNodeId, opcUaNodeDescription.SymbolicName, Name));
                }
            }
            ContosoOpcUaNode opcUaNodeObject = new ContosoOpcUaNode(
                opcUaNodeDescription.NodeId ?? opcUaNodeDescription.ExpandedNodeId,
                opcUaNodeDescription.SymbolicName,
                opcUaNodeDescription);

            NodeList.Add(opcUaNodeObject);
        }

        /// <summary>
        /// Test if opcode requires a unique OPC UA Node Id.
        /// </summary>
        private static bool OpCodeRequiresOpcUaNode(ContosoOpcNodeOpCode opCode)
        {
            switch (opCode)
            {
                case ContosoOpcNodeOpCode.Const:
                case ContosoOpcNodeOpCode.Timespan:
                case ContosoOpcNodeOpCode.Nop:
                    return false;
            }
            return true;
        }
    }
}
