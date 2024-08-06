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

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using VWOFmeSdk.Models.User;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.Storage;

namespace VWOFmeSdk.Services
{
    public class StorageService
    {
        /// <summary>
        /// Get data from storage
        /// </summary>
        /// <param name="featureKey"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetDataInStorage(string featureKey, VWOContext context)
        {
            var storageInstance = Storage.Instance.GetConnector() as Connector;
            if (storageInstance == null)
            {
                return null;
            }

            try
            {
                return storageInstance.Get(featureKey, context.Id) as Dictionary<string, object>;
            }
            catch (System.Exception e)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "STORED_DATA_ERROR", new Dictionary<string, string>
                {
                    {"err", e.ToString()}
                });
                return null;
            }
        }

        /// <summary>
        /// Set data in storage
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SetDataInStorage(Dictionary<string, object> data)
        {
            var storageInstance = Storage.Instance.GetConnector() as Connector;
            if (storageInstance == null)
            {
                return false;
            }

            try
            {
                storageInstance.Set(data);
                return true;
            }
            catch (Exception exception)
            {
                return false;
            }
        }
    }
}
