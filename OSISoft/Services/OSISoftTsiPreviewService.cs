using LibreStream.Api.Models;
using LibreStream.OSISoft.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static LibreStream.Api.Constants;

namespace LibreStream.Api.Iot.Services.OSISoft
{
    public class OSISoftTsiPreviewService : IMeasurementService
    {

        private readonly IHttpClientFactory clientFactory;

        /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="clientFactory">This will be used to generate HTTP clients</param>
		/// <returns></returns>
        public OSISoftTsiPreviewService(IHttpClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        /// <summary>
        /// Return query results based on query
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public QueryResultPage QueryAsync(QueryEventPayload query)
        {
            JObject dataAttributes = new JObject();

            // if it is a query event and has the correct connection string
            if (query.GetEvents != null && (string)query.ServiceProperties["baseAddress"] != null)
            {
                // build a filter for the data attributes we are fetching from OSISoft e.g *[temperature|altitude]
                StringBuilder filterAttributes = new StringBuilder("*[");
                for (int numAttribute = 0; numAttribute < query.GetEvents.ProjectedProperties.Count; numAttribute++)
                {
                    if (!query.GetEvents.ProjectedProperties[numAttribute].Name.EndsWith("_unit"))
                    {
                        filterAttributes.Append(query.GetEvents.ProjectedProperties[numAttribute].Name + "|");
                    }
                    dataAttributes[query.GetEvents.ProjectedProperties[numAttribute].Name] = query.GetEvents.ProjectedProperties[numAttribute].Type;
                }
                filterAttributes.Append("]");

                return QueryEventAsyncHelper(query, filterAttributes.ToString(), dataAttributes).Result;
            }
            else if (query.AggregateSeries != null && (string)query.ServiceProperties["baseAddress"] != null) // a query aggregate event
            {
                Dictionary<string, string> filterAttributes = new Dictionary<string, string>();
                Dictionary<string, string> projectedVariableToAttribute = new Dictionary<string, string>();
                Dictionary<string, string> projectedVariableToAggregation = new Dictionary<string, string>();
                
                // This is a special case where Azure asks for an interval that gets measured in size in terms of days, e.g P3D but we need to take the P off and only use 3D for osisoft
                if (query.AggregateSeries.Interval.Contains("D"))
                {
                    filterAttributes["summaryDuration"] = query.AggregateSeries.Interval[1..]; // e.g removes the P out of P1D
                }
                else
                {
                    filterAttributes["summaryDuration"] = query.AggregateSeries.Interval[2..]; // e.g removes the PT out of PT50M
                }
                filterAttributes["nameFilter"] = "*[";
                filterAttributes["summaryType"] = "";

                // loop through every variable in projectedVariables which were part of the payload of the request received from Onsight connect
                foreach (string projectedVariable in query.AggregateSeries.ProjectedVariables)
                {
                    if (query.AggregateSeries.InlineVariables.ContainsKey(projectedVariable))
                    {
                        filterAttributes["nameFilter"] += ((string)query.AggregateSeries.InlineVariables[projectedVariable]["value"]["tsx"])[7..] + "|";  // e.g removes the "$event.pressure" to "pressure"
                        projectedVariableToAttribute.Add(projectedVariable, ((string)query.AggregateSeries.InlineVariables[projectedVariable]["value"]["tsx"])[7..]);

                        // AGGREGATE_FUNCTION_MAPPER translates between Azure and OSISoft e.g from Avg to Average
                        if (ServiceConstants.AGGREGATE_FUNCTION_MAPPER.ContainsKey((string)query.AggregateSeries.InlineVariables[projectedVariable]["aggregation"]["tsx"]))
                        {
                            filterAttributes["summaryType"] += ServiceConstants.AGGREGATE_FUNCTION_MAPPER[(string)query.AggregateSeries.InlineVariables[projectedVariable]["aggregation"]["tsx"]] + ",";
                            projectedVariableToAggregation.Add(projectedVariable, ServiceConstants.AGGREGATE_FUNCTION_MAPPER[(string)query.AggregateSeries.InlineVariables[projectedVariable]["aggregation"]["tsx"]]);
                        }
                    }
                }
                filterAttributes["nameFilter"] += "]";
                filterAttributes["summaryType"] = filterAttributes["summaryType"][0..^1];

                if (projectedVariableToAttribute.Count > 0 && projectedVariableToAggregation.Count > 0)
                {
                    return QueryAggregateHelper(query, filterAttributes, projectedVariableToAttribute, projectedVariableToAggregation).Result;
                }
            }

            return null;
        }


        /// <summary>
        /// Return query results based on query
        /// </summary>
        /// <param name="query"> The payload of the event</param>
        /// <param name="filterAttributes"> Contains the filter attributes for the query</param>
        /// <param name="projectedVariableToAttribute"> Holds a key/Value pair for projectedVariables->OSISoftAttribute</param>
        /// <param name="projectedVariableToAggregation"> Holds a key/Value pair for projectedVariables->OSISoftSummaryType</param>
        /// <returns></returns>
        private async Task<QueryResultPage> QueryAggregateHelper(QueryEventPayload query, Dictionary<string, string> filterAttributes, Dictionary<string, string> projectedVariableToAttribute, Dictionary<string, string> projectedVariableToAggregation)
        {
            QueryResultPage queryResultPage = new QueryResultPage
            {
                Timestamps = new List<string>(),
                Properties = new List<Property>()
            };

            // make one HTTP request to get all the aggregate information we need. GET request Query parameters were pre-processed and provided by the calling scope
            HttpClient client = clientFactory.CreateClient();
            string url = (string)query.ServiceProperties["baseAddress"] + "/streamsets/" + query.AggregateSeries.TimeSeriesId[0] + "/summary" +
                "?selectedFields=Items.Name;Items.WebId;Items.Items.Value.Type;Items.Items.Value.Value;Items.Items.Value.Timestamp;Items.Items.Type" +
                "&startTime=" + query.AggregateSeries.SearchSpan.From +
                "&endTime=" + query.AggregateSeries.SearchSpan.To +
                "&nameFilter=" + filterAttributes["nameFilter"] +
                "&summaryType=" + filterAttributes["summaryType"] +
                "&summaryDuration=" + filterAttributes["summaryDuration"];
            OSISoftAttributes osiSoftQueryResult = null;
            string authInfo = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(String.Format("{0}:{1}", (string)query.ServiceProperties["username"], (string)query.ServiceProperties["password"])));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authInfo);
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                if (response.IsSuccessStatusCode)
                {
                    string contentString = await response.Content.ReadAsStringAsync();
                    JsonSerializerSettings settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };
                    osiSoftQueryResult = JsonConvert.DeserializeObject<OSISoftAttributes>(contentString, settings);
                }
            }

            // if the request provided us with a valid result, then we proceed, parse the data and return
            if (osiSoftQueryResult != null && osiSoftQueryResult.Items.Count != 0 && osiSoftQueryResult.Items[0].Items != null)
            {
                foreach (KeyValuePair<string, string> projectedVariable in projectedVariableToAttribute)
                {
                    Property currProperty = new Property
                    {
                        Name = projectedVariable.Key,
                        Type = "Double",   // all summaries return results as a double
                        Values = new List<object>()
                    };

                    if (projectedVariableToAggregation.ContainsKey(projectedVariable.Key))
                    {
                        string aggregation = projectedVariableToAggregation[projectedVariable.Key];

                        foreach (OSISoftAttribute attribute in osiSoftQueryResult.Items)
                        {
                            if (attribute.Name == projectedVariable.Value)
                            {
                                foreach (JObject currItem in attribute.Items)
                                {
                                    if (currItem.ContainsKey("Type") && currItem["Type"].ToString() == aggregation)
                                    {
                                        currProperty.Values.Add(currItem["Value"]["Value"]);
                                        if (!queryResultPage.Timestamps.Contains(currItem["Value"]["Timestamp"].ToString()))
                                        {
                                            queryResultPage.Timestamps.Add(currItem["Value"]["Timestamp"].ToString());
                                        }
                                    }
                                }
                            }
                        }
                        queryResultPage.Properties.Add(currProperty);
                    }
                }
                queryResultPage.Progress = 100;
                return queryResultPage;
            }
            else
            {
                throw new Exception(String.Format("An Error Occured while fetching data from OSISoft. Url: {0} \n", url));
            }
        }


        /// <summary>
        /// Return query results based on query
        /// </summary>
        /// <param name="query">Contains the Query Payload from client</param>
        /// <param name="filterAttributes">Has the filter attributes for OSISoft</param>
        /// <param name="dataAttributes">Has the data attributes we are looking for</param>
        /// <returns></returns>
        private async Task<QueryResultPage> QueryEventAsyncHelper(QueryEventPayload query, string filterAttributes, JObject dataAttributes)
        {
            // Initialize return objects 
            QueryResultPage queryResultPage = new QueryResultPage
            {
                Timestamps = new List<string>(),
                Properties = new List<Property>()
            };

            // get the recorded data for the device
            HttpClient client = clientFactory.CreateClient();
            string url = (string)query.ServiceProperties["baseAddress"] + "/streamsets/" + query.GetEvents.TimeSeriesId[0] + "/recorded?nameFilter=" + filterAttributes
                + "&startTime=" + query.GetEvents.SearchSpan.From.ToString() + "&endTime=" + query.GetEvents.SearchSpan.To.ToString();
            OSISoftQueryResult osiSoftQueryResult = null;
            string authInfo = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(String.Format("{0}:{1}", (string)query.ServiceProperties["username"], (string)query.ServiceProperties["password"])));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authInfo);
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                if (response.IsSuccessStatusCode)
                {
                    string contentString = await response.Content.ReadAsStringAsync();
                    JsonSerializerSettings settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };
                    osiSoftQueryResult = JsonConvert.DeserializeObject<OSISoftQueryResult>(contentString, settings);
                }
            }

            // if we received the recorded data
            if (osiSoftQueryResult != null)
            {
                JObject tempAttributeData = new JObject();
                JObject currentAttributesValue = new JObject();
                Dictionary<string, string> currAttributeTimeStamp = new Dictionary<string, string>();

                JArray currItems;
                JObject currItem;
                Property currProperty;

                // loop through OSISoft data for easier processing
                foreach (JObject attribute in osiSoftQueryResult.Items)
                {
                    if (dataAttributes.ContainsKey(attribute["Name"].ToString()))
                    {
                        tempAttributeData.Add(attribute["Name"].ToString(), attribute["Items"]);
                        currItems = (JArray)attribute["Items"];
                        // if a paticular data attribute has at least one reading
                        if (currItems.Count > 0)
                        {
                            currItem = (JObject)currItems.First();
                            if (currItem.ContainsKey("Value") && currItem.ContainsKey("Timestamp"))
                            {
                                currentAttributesValue.Add(attribute["Name"].ToString(), currItem["Value"]);
                                currAttributeTimeStamp.Add(attribute["Name"].ToString(), currItem["Timestamp"].ToString());

                                currProperty = new Property
                                {
                                    Name = attribute["Name"].ToString(),
                                    Type = dataAttributes[attribute["Name"].ToString()].ToString(),
                                    Values = new List<object>()
                                };
                                queryResultPage.Properties.Add(currProperty);
                            }

                            if (dataAttributes.ContainsKey(attribute["Name"].ToString() + "_unit"))
                            {
                                if (currItem.ContainsKey("UnitsAbbreviation") && currItem.ContainsKey("Timestamp"))
                                {
                                    currentAttributesValue.Add(attribute["Name"].ToString() + "_unit", currItem["UnitsAbbreviation"]);
                                    currAttributeTimeStamp.Add(attribute["Name"].ToString() + "_unit", currItem["Timestamp"].ToString());

                                    currProperty = new Property
                                    {
                                        Name = attribute["Name"].ToString() + "_unit",
                                        Type = dataAttributes[attribute["Name"] + "_unit"].ToString(),
                                        Values = new List<object>()
                                    };
                                    queryResultPage.Properties.Add(currProperty);

                                }
                            }
                        }
                    }
                }

                // OSISoft provide timestamps and data values only if they have changed
                // e.g whenever a temperature value changes, it will record the value and timestamp and send it to us
                // if Onsight client asks for data for multiple attributes in one payload, then the data has to be proceesed because not all attributes would have changed at the same time
                // hence they would have different timestamp values
                // to aggregate that, we would find the earliest timestamp attribute
                // we then query the value of all the other attributes at that timestamp and then record the results
                // this processing is done in the code below
                string currMinimumTimestampAttribute = FindMinimumTimeStamp(currAttributeTimeStamp);
                OSISoftQueryResult currMinimumTimestampValues = null;

                if (currMinimumTimestampAttribute != null)
                {
                    // query the values of every attribute at time: current minimum timestamp
                    url = (string)query.ServiceProperties["baseAddress"] + "/streamsets/" + query.GetEvents.TimeSeriesId[0] + "/value?time=" + currAttributeTimeStamp[currMinimumTimestampAttribute].ToString();
                    using (HttpResponseMessage response = await client.GetAsync(url))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string contentString = await response.Content.ReadAsStringAsync();
                            JsonSerializerSettings settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };
                            currMinimumTimestampValues = JsonConvert.DeserializeObject<OSISoftQueryResult>(contentString, settings);
                        }
                    }

                    // if we received valid results
                    if (currMinimumTimestampValues != null)
                    {
                        // loop through the OSISoft results we had first received and store the data of the attribute that had the minimum timestamp
                        for (int currValueAttribute = 0; currValueAttribute < currMinimumTimestampValues.Items.Count; currValueAttribute++)
                        {
                            if (currentAttributesValue.ContainsKey(currMinimumTimestampValues.Items[currValueAttribute]["Name"].ToString()))
                            {
                                if (currMinimumTimestampValues.Items[currValueAttribute]["Name"].ToString() != currMinimumTimestampAttribute)
                                {
                                    currentAttributesValue[currMinimumTimestampValues.Items[currValueAttribute]["Name"].ToString()] = currMinimumTimestampValues.Items[currValueAttribute]["Value"]["Value"];
                                    currAttributeTimeStamp[currMinimumTimestampValues.Items[currValueAttribute]["Name"].ToString()] = currMinimumTimestampValues.Items[currValueAttribute]["Value"]["Timestamp"].ToString();

                                    if (currentAttributesValue.ContainsKey(currMinimumTimestampValues.Items[currValueAttribute]["Name"].ToString()))
                                    {
                                        currAttributeTimeStamp[currMinimumTimestampValues.Items[currValueAttribute]["Name"].ToString() + "_unit"] = currMinimumTimestampValues.Items[currValueAttribute]["Value"]["Timestamp"].ToString();
                                    }
                                }
                                else
                                {
                                    currItems = (JArray)tempAttributeData[currMinimumTimestampAttribute];
                                    currItems.RemoveAt(0);
                                }
                            }
                        }

                        // this accounts for the case where count was not specified in the payload of the request, which means we would return as many readings as we can
                        if (query.GetEvents.Take == 0)
                        {
                            query.GetEvents.Take = int.MaxValue;
                        }

                        // we will fill in the queryResultPage object using OSISoft data
                        while (query.GetEvents.Take != 0)
                        {
                            if (currMinimumTimestampAttribute != null)
                            {
                                // add the earliest/minimum timestamp to the result
                                queryResultPage.Timestamps.Add(currAttributeTimeStamp[currMinimumTimestampAttribute]);
                                string minimumTimestamp = currAttributeTimeStamp[currMinimumTimestampAttribute];

                                // loop through every property that the result requires
                                foreach (Property property in queryResultPage.Properties)
                                {
                                    // add the value that we temporarily stored at the earliest/minimum timestamp
                                    property.Values.Add(currentAttributesValue[property.Name]);

                                    // replace the temporarily stored value with a the next value of that particular property and record the timestamp at that value temporarily in a dictionary
                                    if (minimumTimestamp == currAttributeTimeStamp[property.Name].ToString() && !property.Name.EndsWith("_unit"))
                                    {
                                        currItems = (JArray)tempAttributeData[property.Name];

                                        if (currItems.Count != 0)   // if the attributes still have readings
                                        {
                                            currentAttributesValue[property.Name] = currItems.First()["Value"];
                                            currAttributeTimeStamp[property.Name] = currItems.First()["Timestamp"].ToString();

                                            if (currAttributeTimeStamp.ContainsKey(property.Name + "_unit"))
                                            {
                                                currAttributeTimeStamp[property.Name + "_unit"] = currItems[0]["Timestamp"].ToString();
                                            }

                                            currItems.RemoveAt(0);
                                        }
                                        else // this is a case where an attribute has no more timestamps and values, which means, the value has not changed fron now onwards until the end of the search span
                                        {
                                            currAttributeTimeStamp[property.Name] = "NONE";
                                        }
                                    }
                                }

                                // find the next earliest/minimum timestamp among the attributes
                                currMinimumTimestampAttribute = FindMinimumTimeStamp(currAttributeTimeStamp);
                                query.GetEvents.Take = query.GetEvents.Take - 1;

                                // special case where we are either reaching the end of our count or reaching the end of recorded values that OSISoft provided
                                if (query.GetEvents.Take == 1 || currMinimumTimestampAttribute == null) // send the last value at that timestamp
                                {
                                    // the last value sent in the response should be the value of all the attributes at the query.getEvents.searchSpan.to timestamp
                                    // therefore we will query OSISoft to find that value
                                    // It is added to ensure that the last read value of attributes in the query.getEvents.searchSpan.to is always sent
                                    OSISoftQueryResult latestAttributeValues = null;
                                    url = (string)query.ServiceProperties["baseAddress"] + "/streamsets/" + query.GetEvents.TimeSeriesId[0] + "/value?time=" + query.GetEvents.SearchSpan.To;
                                    using (HttpResponseMessage response = await client.GetAsync(url))
                                    {
                                        if (response.IsSuccessStatusCode)
                                        {
                                            string contentString = await response.Content.ReadAsStringAsync();
                                            JsonSerializerSettings settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };
                                            latestAttributeValues = JsonConvert.DeserializeObject<OSISoftQueryResult>(contentString, settings);
                                        }
                                    }

                                    // if we received values from OSISoft, then we add to the response
                                    if (latestAttributeValues != null)
                                    {
                                        queryResultPage.Timestamps.Add(query.GetEvents.SearchSpan.To);

                                        foreach (JObject currAttribute in latestAttributeValues.Items)
                                        {
                                            foreach (Property property in queryResultPage.Properties)
                                            {
                                                if (property.Name.Equals(currAttribute["Name"].ToString()))
                                                {
                                                    property.Values.Add(currAttribute["Value"]["Value"]);
                                                }

                                                if (property.Name.Equals(currAttribute["Name"] + "_unit"))
                                                {
                                                    property.Values.Add(currAttribute["Value"]["UnitsAbbreviation"]);
                                                }
                                            }
                                        }
                                        query.GetEvents.Take = query.GetEvents.Take - 1;
                                    }

                                    // break as we have built the entire response object
                                    if (currMinimumTimestampAttribute == null)
                                    {
                                        break;
                                    }

                                }
                            }
                            else
                            {
                                throw new Exception("An error occured while parsing the data from OSISoft");
                            }
                        }
                    }
                }
            }
            else
            {
                throw new Exception("OSISoft failed to respond to the GET request " + url);
            }

            queryResultPage.Progress = 100;

            return queryResultPage;
        }


        /// <summary>
        /// Gets the earliest event among different attributes
        /// </summary>
        /// <param name="currentAttributesTimeStamp">A timestamp for the current attributes</param>
        /// <returns></returns>
        private string FindMinimumTimeStamp(Dictionary<string, string> currentAttributesTimeStamp)
        {
            DateTime minimumTimeStamp = DateTime.MaxValue;
            string minimumAttribute = null;

            foreach (var attribute in currentAttributesTimeStamp)
            {
                if (attribute.Value.ToString() != "NONE" && DateTime.Parse(attribute.Value.ToString()) < minimumTimeStamp && !attribute.Key.EndsWith("_unit"))
                {
                    minimumTimeStamp = DateTime.Parse(attribute.Value.ToString());
                    minimumAttribute = attribute.Key;
                }
            }

            return minimumAttribute;
        }
    }
}
