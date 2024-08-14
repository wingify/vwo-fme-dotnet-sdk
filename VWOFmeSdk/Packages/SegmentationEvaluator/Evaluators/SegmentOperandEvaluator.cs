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
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.SegmentationEvaluator.Enums;
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Packages.SegmentationEvaluator.Utils;

namespace VWOFmeSdk.Packages.SegmentationEvaluator.Evaluators
{
    public class SegmentOperandEvaluator
    {
        public bool EvaluateCustomVariableDSL(JToken dslOperandValue, Dictionary<string, object> properties)
        {
            var entry = SegmentUtil.GetKeyValue(dslOperandValue);
            var operandKey = entry.Value.Key;
            var operandValueNode = entry.Value.Value;
            var operandValue = operandValueNode.ToString();

            if (!properties.ContainsKey(operandKey))
            {
                return false;
            }

            if (operandValue.Contains("inlist"))
            {
                var listIdPattern = new Regex("inlist\\((\\w+:\\d+)\\)");
                var matcher = listIdPattern.Match(operandValue);
                if (!matcher.Success)
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "Invalid 'inList' operand format");
                    return false;
                }
                var listId = matcher.Groups[1].Value;
                var tagValue = properties[operandKey];
                var attributeValue = PreProcessTagValue(tagValue.ToString());
                var queryParamsObj = new Dictionary<string, string>
                {
                    { "attribute", attributeValue },
                    { "listId", listId }
                };

                var response = GatewayServiceUtil.GetFromGatewayService(queryParamsObj, UrlEnum.ATTRIBUTE_CHECK.GetUrl());
                if (response == null)
                {
                    return false;
                }
                return bool.Parse(response);
            }
            else
            {
                var tagValue = properties[operandKey];
                tagValue = PreProcessTagValue(tagValue.ToString());
                var preProcessOperandValue = PreProcessOperandValue(operandValue);
                var processedValues = ProcessValues(preProcessOperandValue["operandValue"], tagValue);
                tagValue = processedValues["tagValue"];
                var operandType = (SegmentOperandValueEnum)preProcessOperandValue["operandType"];
                return ExtractResult(operandType, processedValues["operandValue"].ToString().Trim().Replace("\"", ""), tagValue.ToString());
            }
        }

        public Dictionary<string, object> PreProcessOperandValue(string operand)
        {
            SegmentOperandValueEnum operandType;
            string operandValue = null;

            if (SegmentUtil.MatchWithRegex(operand, SegmentOperandRegexEnum.LOWER_MATCH.GetRegex()))
            {
                operandType = SegmentOperandValueEnum.LOWER_VALUE;
                operandValue = ExtractOperandValue(operand, SegmentOperandRegexEnum.LOWER_MATCH.GetRegex());
            }
            else if (SegmentUtil.MatchWithRegex(operand, SegmentOperandRegexEnum.WILDCARD_MATCH.GetRegex()))
            {
                operandValue = ExtractOperandValue(operand, SegmentOperandRegexEnum.WILDCARD_MATCH.GetRegex());
                bool startingStar = SegmentUtil.MatchWithRegex(operandValue, SegmentOperandRegexEnum.STARTING_STAR.GetRegex());
                bool endingStar = SegmentUtil.MatchWithRegex(operandValue, SegmentOperandRegexEnum.ENDING_STAR.GetRegex());
                if (startingStar && endingStar)
                {
                    operandType = SegmentOperandValueEnum.STARTING_ENDING_STAR_VALUE;
                }
                else if (startingStar)
                {
                    operandType = SegmentOperandValueEnum.STARTING_STAR_VALUE;
                }
                else if (endingStar)
                {
                    operandType = SegmentOperandValueEnum.ENDING_STAR_VALUE;
                }
                else
                {
                    operandType = SegmentOperandValueEnum.REGEX_VALUE;
                }
                operandValue = operandValue.Trim('*');
            }
            else if (SegmentUtil.MatchWithRegex(operand, SegmentOperandRegexEnum.REGEX_MATCH.GetRegex()))
            {
                operandType = SegmentOperandValueEnum.REGEX_VALUE;
                operandValue = ExtractOperandValue(operand, SegmentOperandRegexEnum.REGEX_MATCH.GetRegex());
            }
            else if (SegmentUtil.MatchWithRegex(operand, SegmentOperandRegexEnum.GREATER_THAN_MATCH.GetRegex()))
            {
                operandType = SegmentOperandValueEnum.GREATER_THAN_VALUE;
                operandValue = ExtractOperandValue(operand, SegmentOperandRegexEnum.GREATER_THAN_MATCH.GetRegex());
            }
            else if (SegmentUtil.MatchWithRegex(operand, SegmentOperandRegexEnum.GREATER_THAN_EQUAL_TO_MATCH.GetRegex()))
            {
                operandType = SegmentOperandValueEnum.GREATER_THAN_EQUAL_TO_VALUE;
                operandValue = ExtractOperandValue(operand, SegmentOperandRegexEnum.GREATER_THAN_EQUAL_TO_MATCH.GetRegex());
            }
            else if (SegmentUtil.MatchWithRegex(operand, SegmentOperandRegexEnum.LESS_THAN_MATCH.GetRegex()))
            {
                operandType = SegmentOperandValueEnum.LESS_THAN_VALUE;
                operandValue = ExtractOperandValue(operand, SegmentOperandRegexEnum.LESS_THAN_MATCH.GetRegex());
            }
            else if (SegmentUtil.MatchWithRegex(operand, SegmentOperandRegexEnum.LESS_THAN_EQUAL_TO_MATCH.GetRegex()))
            {
                operandType = SegmentOperandValueEnum.LESS_THAN_EQUAL_TO_VALUE;
                operandValue = ExtractOperandValue(operand, SegmentOperandRegexEnum.LESS_THAN_EQUAL_TO_MATCH.GetRegex());
            }
            else
            {
                operandType = SegmentOperandValueEnum.EQUAL_VALUE;
                operandValue = operand;
            }

            var result = new Dictionary<string, object>
            {
                { "operandType", operandType },
                { "operandValue", operandValue }
            };
            return result;
        }

        public bool EvaluateUserDSL(string dslOperandValue, Dictionary<string, object> properties)
        {
            var users = dslOperandValue.Split(',');
            foreach (var user in users)
            {
                if (user.Trim().Replace("\"", "").Equals(properties["_vwoUserId"]))
                {
                    return true;
                }
            }
            return false;
        }

        public bool EvaluateUserAgentDSL(string dslOperandValue, VWOContext context)
        {
            if (context == null || context.UserAgent == null)
            {
                return false;
            }
            var tagValue = Uri.UnescapeDataString(context.UserAgent);
            var preProcessOperandValue = PreProcessOperandValue(dslOperandValue);
            var processedValues = ProcessValues(preProcessOperandValue["operandValue"], tagValue);
            tagValue = (string)processedValues["tagValue"];
            var operandType = (SegmentOperandValueEnum)preProcessOperandValue["operandType"];
            return ExtractResult(operandType, processedValues["operandValue"].ToString().Trim().Replace("\"", ""), tagValue);
        }

        public string PreProcessTagValue(string tagValue)
        {
            if (tagValue == null)
            {
                return "";
            }
            if (DataTypeUtil.IsBoolean(tagValue))
            {
                return tagValue.ToString().ToLower();
            }
            return tagValue.Trim();
        }

        private Dictionary<string, object> ProcessValues(object operandValue, object tagValue)
        {
            double processedOperandValue;
            double processedTagValue;
            var result = new Dictionary<string, object>();
            try
            {
                processedOperandValue = double.Parse(operandValue.ToString());
                processedTagValue = double.Parse(tagValue.ToString());
            }
            catch (FormatException)
            {
                result["operandValue"] = operandValue;
                result["tagValue"] = tagValue;
                return result;
            }
            result["operandValue"] = processedOperandValue.ToString();
            result["tagValue"] = processedTagValue.ToString();
            return result;
        }

        public bool ExtractResult(SegmentOperandValueEnum operandType, object operandValue, string tagValue)
        {
            bool result = false;

            switch (operandType)
            {
                case SegmentOperandValueEnum.LOWER_VALUE:
                    result = operandValue.ToString().Equals(tagValue, StringComparison.OrdinalIgnoreCase);
                    break;
                case SegmentOperandValueEnum.STARTING_ENDING_STAR_VALUE:
                    result = tagValue.Contains(operandValue.ToString());
                    break;
                case SegmentOperandValueEnum.STARTING_STAR_VALUE:
                    result = tagValue.EndsWith(operandValue.ToString());
                    break;
                case SegmentOperandValueEnum.ENDING_STAR_VALUE:
                    result = tagValue.StartsWith(operandValue.ToString());
                    break;
                case SegmentOperandValueEnum.REGEX_VALUE:
                    try
                    {
                        var pattern = new Regex(operandValue.ToString());
                        var matcher = pattern.Match(tagValue);
                        result = matcher.Success;
                    }
                    catch (Exception)
                    {
                        result = false;
                    }
                    break;
                case SegmentOperandValueEnum.GREATER_THAN_VALUE:
                    result = float.Parse(tagValue) > float.Parse(operandValue.ToString());
                    break;
                case SegmentOperandValueEnum.GREATER_THAN_EQUAL_TO_VALUE:
                    result = float.Parse(tagValue) >= float.Parse(operandValue.ToString());
                    break;
                case SegmentOperandValueEnum.LESS_THAN_VALUE:
                    result = float.Parse(tagValue) < float.Parse(operandValue.ToString());
                    break;
                case SegmentOperandValueEnum.LESS_THAN_EQUAL_TO_VALUE:
                    result = float.Parse(tagValue) <= float.Parse(operandValue.ToString());
                    break;
                default:
                    result = tagValue.Equals(operandValue.ToString());
                    break;
            }

            return result;
        }

        public string ExtractOperandValue(string operand, string regex)
        {
            var pattern = new Regex(regex);
            var matcher = pattern.Match(operand);
            if (matcher.Success)
            {
                return matcher.Groups[1].Value;
            }
            return operand;
        }
    }
}