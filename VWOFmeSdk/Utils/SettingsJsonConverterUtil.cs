#pragma warning disable 1587
/**
 * Copyright 2024-2025 Wingify Software Pvt. Ltd.
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
#pragma warning restore 1587

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VWOFmeSdk.Utils
{
    /// <summary>
    /// Custom JSON converter that converts empty objects {} to empty arrays [] for List<T> properties
    /// </summary>
    /// <typeparam name="T">The type of items in the list</typeparam>
    public class EmptyObjectToArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(List<T>).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                // Skip empty object and return an empty list
                reader.Skip();
                return new List<T>();
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                return serializer.Deserialize<List<T>>(reader);
            }
            else if (reader.TokenType == JsonToken.Null)
            {
                return new List<T>();
            }

            return new List<T>();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
