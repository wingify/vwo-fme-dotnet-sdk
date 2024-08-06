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
#pragma warning restore 1587

using System.Collections.Generic;
using VWOFmeSdk.Models;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Services;

namespace VWOFmeSdk.Interfaces.Storage
{
    public interface IStorageDecorator
    {
        /// <summary>
        /// Sets data in storage.
        /// </summary>
        /// <param name="data">The data to be stored.</param>
        /// <param name="storageService">The storage service instance.</param>
        /// <returns>The stored VariationModel.</returns>
        Variation SetDataInStorage(Dictionary<string, object> data, StorageService storageService);

        /// <summary>
        /// Retrieves a feature from storage.
        /// </summary>
        /// <param name="featureKey">The key of the feature to retrieve.</param>
        /// <param name="context">The context model.</param>
        /// <param name="storageService">The storage service instance.</param>
        /// <returns>The retrieved feature or relevant status.</returns>
        Dictionary<string, object> GetFeatureFromStorage(string featureKey, VWOContext context, StorageService storageService);
    }
}
