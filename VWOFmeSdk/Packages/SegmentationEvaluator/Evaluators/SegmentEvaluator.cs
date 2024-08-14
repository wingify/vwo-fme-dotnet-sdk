#pragma warning disable 1587
/**
 * Copyright 2024 Wingify Software Pvt. Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#pragma warning disable 1587

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.SegmentationEvaluator.Enums;
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Packages.SegmentationEvaluator.Utils;
using VWOFmeSdk.Decorators;
using VWOFmeSdk.Packages.Storage;

namespace VWOFmeSdk.Packages.SegmentationEvaluator.Evaluators
{
    public class SegmentEvaluator
    {
        public VWOContext context;
        public Settings settings;
        public Feature feature;

        public bool IsSegmentationValid(JToken dsl, Dictionary<string, object> properties)
        {
            var entry = SegmentUtil.GetKeyValue(dsl);
            var operatorEnum = SegmentOperatorValueEnumExtensions.FromValue(entry.Value.Key);
            var subDsl = entry.Value.Value;

            switch (operatorEnum)
            {
                case SegmentOperatorValueEnum.NOT:
                    return !IsSegmentationValid(subDsl, properties);
                case SegmentOperatorValueEnum.AND:
                    return Every(subDsl, properties);
                case SegmentOperatorValueEnum.OR:
                    return Some(subDsl, properties);
                case SegmentOperatorValueEnum.USER:
                    return new SegmentOperandEvaluator().EvaluateUserDSL(subDsl.ToString(), properties);
                case SegmentOperatorValueEnum.CUSTOM_VARIABLE:
                    return new SegmentOperandEvaluator().EvaluateCustomVariableDSL(subDsl, properties); 
                case SegmentOperatorValueEnum.UA:
                    return new SegmentOperandEvaluator().EvaluateUserAgentDSL(subDsl.ToString(), context);
                default:
                    return false;
            }
        }

        public bool Some(JToken dslNodes, Dictionary<string, object> customVariables)
        {
            var uaParserMap = new Dictionary<string, List<string>>();
            var keyCount = 0;
            var isUaParser = false;

            foreach (var dsl in dslNodes)
            {
                foreach (var property in dsl.Children<JProperty>())
                {
                    var keyEnum = SegmentOperatorValueEnumExtensions.FromValue(property.Name);
                    if (keyEnum == SegmentOperatorValueEnum.OPERATING_SYSTEM ||
                        keyEnum == SegmentOperatorValueEnum.BROWSER_AGENT ||
                        keyEnum == SegmentOperatorValueEnum.DEVICE_TYPE ||
                        keyEnum == SegmentOperatorValueEnum.DEVICE)
                    {
                        isUaParser = true;
                        var value = property.Value;

                        if (!uaParserMap.ContainsKey(property.Name))
                        {
                            uaParserMap[property.Name] = new List<string>();
                        }

                        if (value is JArray valueArray)
                        {
                            foreach (var val in valueArray)
                            {
                                if (val.Type == JTokenType.String)
                                {
                                    uaParserMap[property.Name].Add(val.ToString());
                                }
                            }
                        }
                        else if (value.Type == JTokenType.String)
                        {
                            uaParserMap[property.Name].Add(value.ToString());
                        }

                        keyCount++;
                    }

                    if (keyEnum == SegmentOperatorValueEnum.FEATURE_ID)
                    {
                        var featureIdObject = property.Value as JObject;
                        if (featureIdObject != null)
                        {
                            foreach (var featureIdProperty in featureIdObject.Properties())
                            {
                                var featureIdKey = featureIdProperty.Name;
                                var featureIdValue = featureIdProperty.Value.ToString();

                                if (featureIdValue.Equals("on") || featureIdValue.Equals("off"))
                                {
                                    var feature = settings.Features.Find(f => f.Id == int.Parse(featureIdKey));

                                    if (feature != null)
                                    {
                                        var featureKey = feature.Key;
                                        bool result = CheckInUserStorage(settings, featureKey, context);
                                        if (featureIdValue.Equals("off")) {
                                            return !result;
                                        }
                                        return result;
                                    }
                                    else
                                    {
                                        LoggerService.Log(LogLevelEnum.DEBUG, "Feature not found with featureIdKey: " + featureIdKey);
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }

                if (isUaParser && keyCount == dslNodes.Count())
                {
                    try
                    {
                        return CheckUserAgentParser(uaParserMap);
                    }
                    catch (Exception err)
                    {
                        LoggerService.Log(LogLevelEnum.ERROR, "Failed to validate User Agent. Error: " + err);
                    }
                }

                if (IsSegmentationValid(dsl, customVariables))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Every(JToken dslNodes, Dictionary<string, object> customVariables)
        {
            var locationMap = new Dictionary<string, object>();
            foreach (var dsl in dslNodes)
            {
                foreach (var keyValuePair in dsl)
                {
                    var key = ((JProperty)keyValuePair).Name;
                    var value = ((JProperty)keyValuePair).Value;
                    
                    var keyEnum = SegmentOperatorValueEnumExtensions.FromValue(key);
                    if (keyEnum == SegmentOperatorValueEnum.COUNTRY ||
                        keyEnum == SegmentOperatorValueEnum.REGION ||
                        keyEnum == SegmentOperatorValueEnum.CITY)
                    {
                        AddLocationValuesToMap(dsl, locationMap);
                        if (locationMap.Count == dslNodes.Count())
                        {
                            return CheckLocationPreSegmentation(locationMap);
                        }
                        continue;
                    }
                    var res = IsSegmentationValid(dsl, customVariables);
                    if (!res)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void AddLocationValuesToMap(JToken dsl, Dictionary<string, object> locationMap)
        {
            var fieldNames = dsl.Children<JProperty>().Select(prop => prop.Name);
            var keyEnum = SegmentOperatorValueEnumExtensions.FromValue(fieldNames.FirstOrDefault());
            if (keyEnum == SegmentOperatorValueEnum.COUNTRY)
            {
                locationMap[keyEnum.GetValue()] = dsl[keyEnum.GetValue()].ToString();
            }
            if (keyEnum == SegmentOperatorValueEnum.REGION)
            {
                locationMap[keyEnum.GetValue()] = dsl[keyEnum.GetValue()].ToString();
            }
            if (keyEnum == SegmentOperatorValueEnum.CITY)
            {
                locationMap[keyEnum.GetValue()] = dsl[keyEnum.GetValue()].ToString();
            }
        }

        public bool CheckLocationPreSegmentation(Dictionary<string, object> locationMap)
        {
            if (context == null || string.IsNullOrEmpty(context.IpAddress))
            {
                LoggerService.Log(LogLevelEnum.INFO, "To evaluate location pre Segment, please pass ipAddress in context object");
                return false;
            }
            if (context.Vwo == null || context.Vwo.Location == null || context.Vwo.Location.Count == 0)
            {
                return false;
            }
            return SegmentUtil.ValuesMatch(locationMap, context.Vwo.Location);
        }

        public bool CheckUserAgentParser(Dictionary<string, List<string>> uaParserMap)
        {
            if (context == null || string.IsNullOrEmpty(context.UserAgent))
            {
                LoggerService.Log(LogLevelEnum.INFO, "To evaluate user agent related segments, please pass userAgent in context object");
                return false;
            }
            if (context.Vwo == null || context.Vwo.UserAgent == null || context.Vwo.UserAgent.Count == 0)
            {
                return false;
            }

            return SegmentUtil.CheckValuePresent(uaParserMap, context.Vwo.UserAgent);
        }

        public bool CheckInUserStorage(Settings settings, string featureKey, VWOContext context)
        {
            var storageService = new StorageService();
            var storedDataMap = new StorageDecorator().GetFeatureFromStorage(featureKey, context, storageService);
            try
            {
                var storageMapAsString = JsonConvert.SerializeObject(storedDataMap);
                var storedData = JsonConvert.DeserializeObject<VWOFmeSdk.Models.Storage>(storageMapAsString);

                return storedData != null && storedDataMap.Count > 1;
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "Error in checking feature in user storage. Got error: " + exception);
                return false;
            }
        }
    }
}
