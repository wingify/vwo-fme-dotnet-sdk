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
#pragma warning disable 1587

using System;
using System.Threading.Tasks;
using VWOFmeSdk.Interfaces.Networking;
using VWOFmeSdk.Packages.NetworkLayer.Client;
using VWOFmeSdk.Packages.NetworkLayer.Handlers;
using VWOFmeSdk.Packages.NetworkLayer.Models;
using VWOFmeSdk.Services;
using VWOFmeSdk.Packages.Logger.Enums;

namespace VWOFmeSdk.Packages.NetworkLayer.Manager
{
    public class NetworkManager
    {
        private static NetworkManager instance;

        private GlobalRequestModel config;
        private NetworkClientInterface client;
        private TaskFactory executorService;

        private NetworkManager()
        {
            // Initialize default executor service
            this.executorService = new TaskFactory(TaskScheduler.Default);
        }

        public static NetworkManager GetInstance()
        {
            if (instance == null)
            {
                instance = new NetworkManager();
            }
            return instance;
        }

        public void AttachClient(NetworkClientInterface client)
        {
            this.client = client;
            this.config = new GlobalRequestModel(null, null, null, null); // Initialize with default config
        }

        public void AttachClient()
        {
            this.client = new NetworkClient();
            this.config = new GlobalRequestModel(null, null, null, null); // Initialize with default config
        }

        public void SetConfig(GlobalRequestModel config)
        {
            this.config = config;
        }

        public GlobalRequestModel GetConfig()
        {
            return this.config;
        }

        public RequestModel CreateRequest(RequestModel request)
        {
            var handler = new RequestHandler();
            return handler.CreateRequest(request, this.config); // Merge and create request
        }

        public ResponseModel Get(RequestModel request)
        {
            try
            {
                var networkOptions = CreateRequest(request);
                if (networkOptions == null)
                {
                    return null;
                }
                else
                {
                    return client.GET(request);
                }
            }
            catch (Exception error)
            {
                LoggerService.Log(LogLevelEnum.ERROR, $"Error when creating get request, error: {error}");
                return null;
            }
        }

        /// <summary>
        /// Synchronously sends a POST request to the server.
        /// </summary>
        /// <param name="request">The RequestModel containing the URL, headers, and body of the POST request.</param>
        /// <returns></returns>
        public ResponseModel Post(RequestModel request)
        {
            try
            {
                var networkOptions = CreateRequest(request);
                if (networkOptions == null)
                {
                    return null;
                }
                else
                {
                    return client.POST(request);
                }
            }
            catch (Exception error)
            {
                LoggerService.Log(LogLevelEnum.ERROR, $"Error when creating post request, error: {error}");
                return null;
            }
        }

        /// <summary>
        /// Asynchronously sends a POST request to the server.
        /// </summary>
        /// <param name="request">The RequestModel containing the URL, headers, and body of the POST request.</param>
        public void PostAsync(RequestModel request)
        {
            executorService.StartNew(() => Post(request));
        }
    }
}