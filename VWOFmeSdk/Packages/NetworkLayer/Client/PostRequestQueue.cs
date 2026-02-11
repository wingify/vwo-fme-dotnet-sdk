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
using System.Threading.Channels;
using System.Threading.Tasks;
using VWOFmeSdk.Interfaces.Batching;
using VWOFmeSdk.Packages.NetworkLayer.Models;
using VWOFmeSdk.Packages.NetworkLayer.Manager;
using VWOFmeSdk.Services;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Enums;
using ConstantsNamespace = VWOFmeSdk.Constants;


namespace VWOFmeSdk.Packages.NetworkLayer.Client
{
    /// <summary>
    /// Channel configuration for queuing POST requests.
    /// Uses a bounded channel (fixed capacity). When full, drops oldest items (DropOldest) to prevent blocking.
    ///
    /// IMPORTANT:
    /// - The channel is created lazily (first access to <see cref="Channel"/>).
    /// - Capacity can be configured via <see cref="Initialize(int?)"/>, but only BEFORE the channel is created.
    /// </summary>
    public static class PostRequestChannel
    {
        private static readonly object _lock = new object();
        private static Channel<QueuedPostRequest> _channel;
        private static int _capacity = ConstantsNamespace.Constants.DEFAULT_MAX_REQUEST_QUEUE_CAPACITY;

        /// <summary>
        /// Sets the POST request queue capacity.
        /// This only takes effect if called before the channel is created.
        ///
        /// Once something starts sending events, the channel must NOT be recreated (that would lose queued events).
        /// </summary>
        public static void Initialize(int? capacity)
        {
            if (!capacity.HasValue || capacity.Value <= 0) return;

            lock (_lock)
            {
                // Channel already created; do not recreate or change capacity.
                if (_channel != null) return;
                _capacity = capacity.Value;
            }
        }

        public static Channel<QueuedPostRequest> Channel
        {
            get
            {
                // Fast path: if already created, no locking needed.
                if (_channel != null) return _channel;

                lock (_lock)
                {
                    if (_channel == null)
                    {
                        // Create the bounded channel exactly once, using:
                        // - configured capacity (if Initialize was called early), otherwise
                        // - default constant capacity.
                        _channel = System.Threading.Channels.Channel.CreateBounded<QueuedPostRequest>(
                            new BoundedChannelOptions(_capacity)
                            {
                                FullMode = BoundedChannelFullMode.DropOldest,
                                SingleReader = false, // Multiple readers (worker tasks)
                                SingleWriter = false  // Multiple writers (concurrent POST calls)
                            });
                    }
                }

                return _channel;
            }
        }
    }

    /// <summary>
    /// Wrapper class for queued POST requests containing the request and callback.
    /// </summary>
    public class QueuedPostRequest
    {
        public RequestModel Request { get; set; }
        public IFlushInterface FlushCallback { get; set; }
        
        /// <summary>
        /// Context data needed for response handling (debug events/logging)
        /// </summary>
        public ResponseDataForDebugging ResponseDataForDebugging { get; set; }
    }

    /// <summary>
    /// Context data needed to handle the response after PostAsync completes.
    /// Contains all the data needed for debug events and logging.
    /// </summary>
    public class ResponseDataForDebugging
    {
        public Dictionary<string, object> Payload { get; set; }
        public string ApiName { get; set; }
        public string ExtraDataForMessage { get; set; }
    }

    /// <summary>
    /// Background worker that processes POST requests from the channel queue.
    /// Automatically dequeues and processes requests asynchronously.
    /// Uses MAX_CONCURRENT_THREADS worker tasks to limit concurrent processing.
    /// </summary>
    public sealed class PostRequestBackgroundWorker
    {
        private readonly Task[] workerTasks;

        public PostRequestBackgroundWorker()
        {
            // Get max concurrent threads from NetworkManager (configurable via init options)
            int maxThreads = NetworkManager.GetInstance().GetMaxConcurrentThreads();
            
            // Start multiple worker tasks to process requests concurrently
            // Each worker processes one request at a time, limiting concurrency naturally
            workerTasks = new Task[maxThreads];
            for (int i = 0; i < maxThreads; i++)
            {
                workerTasks[i] = Task.Run(ProcessQueueAsync);
            }
        }

        /// <summary>
        /// Waits for all worker tasks to complete processing.
        /// </summary>
        public async Task WaitForCompletionAsync()
        {
            if (workerTasks != null)
            {
                await Task.WhenAll(workerTasks).ConfigureAwait(false);
            }
        }

        private async Task ProcessQueueAsync()
        {
            var reader = PostRequestChannel.Channel.Reader;
            
            // DEQUEUEING LOOP: This continuously reads from the channel
            // WaitToReadAsync waits for items to be available, then TryRead dequeues them
            while (await reader.WaitToReadAsync())
            {
                // DEQUEUEING HAPPENS HERE: TryRead automatically dequeues items from the channel
                while (reader.TryRead(out var queuedRequest))
                {
                    ResponseModel response = null;
                    try
                    {
                        // PostAsync executes synchronously, so we can call it directly
                        var networkManager = NetworkManager.GetInstance();
                        response = networkManager.PostAsync(queuedRequest.Request, queuedRequest.FlushCallback, null, null);
                        
                        // Handle response if context is provided (for debug events, logging, etc.)
                        if (queuedRequest.ResponseDataForDebugging != null && response != null)
                        {
                            HandleResponse(response, queuedRequest.Request, queuedRequest.ResponseDataForDebugging);
                        }

                        // if network call failed, re enqueue the data
                        if (response.GetStatusCode() < ConstantsNamespace.Constants.HTTP_SUCCESS_MIN || response.GetStatusCode() >= ConstantsNamespace.Constants.HTTP_SUCCESS_UPPER_BOUND)
                        {
                            bool reEnqueue = PostRequestChannel.Channel.Writer.TryWrite(queuedRequest);
                            if(reEnqueue)
                            {
                                LoggerService.Log(LogLevelEnum.INFO, "RE_ENQUEUE_SUCCESS", null);
                            } 
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error for debugging - swallow exception, never throw to user
                        LogManager.GetInstance().ErrorLog("REQUEST_PROCESSING_ERROR", new Dictionary<string, string> 
                        { 
                            { "method", "POST" }, 
                            { "err", ex.Message }
                        });

                        // Notify flush callback of the error
                        queuedRequest.FlushCallback?.OnFlush($"Error occurred while processing request: {ex.Message}", null);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the response after PostAsync completes.
        /// Executes the response handling logic (debug events, logging, etc.)
        /// </summary>
        private void HandleResponse(ResponseModel response, RequestModel request, ResponseDataForDebugging responseDataForDebugging)
        {
            try
            {
                // Handle response after PostAsync completes
                if (response != null && response.GetTotalAttempts() > 0)
                {
                    var debugEventProps = NetworkUtil.CreateNetworkAndRetryDebugEvent(
                        response,
                        responseDataForDebugging.Payload,
                        responseDataForDebugging.ApiName,
                        responseDataForDebugging.ExtraDataForMessage
                    );
                    debugEventProps["uuid"] = request.GetUuid();
                    DebuggerServiceUtil.SendDebugEventToVWO(debugEventProps);

                    LoggerService.Log(LogLevelEnum.INFO, "NETWORK_CALL_SUCCESS_WITH_RETRIES", new Dictionary<string, string>
                    {
                        { "extraData", $"POST {SettingsManager.GetInstance().hostname + UrlService.GetEndpointWithCollectionPrefix(UrlEnum.EVENTS.GetUrl())}" },
                        { "attempts", response.GetTotalAttempts().ToString() },
                        { "err", FunctionUtil.GetFormattedErrorMessage(response.GetError() as Exception) }
                    });
                }

                // Log error if status code is not 2xx (regardless of retries)
                if (response != null && (response.GetStatusCode() < ConstantsNamespace.Constants.HTTP_SUCCESS_MIN || response.GetStatusCode() > ConstantsNamespace.Constants.HTTP_SUCCESS_MAX))
                {
                    var responseModel = new ResponseModel();
                    responseModel.SetError(response.GetError() as Exception);
                    responseModel.SetStatusCode(response.GetStatusCode());

                    var debugEventProps = NetworkUtil.CreateNetworkAndRetryDebugEvent(responseModel, responseDataForDebugging.Payload, responseDataForDebugging.ApiName, responseDataForDebugging.ExtraDataForMessage);
                    debugEventProps["uuid"] = request.GetUuid();
                    DebuggerServiceUtil.SendDebugEventToVWO(debugEventProps);
                    LogManager.GetInstance().ErrorLog("NETWORK_CALL_FAILED", new Dictionary<string, string> { { "method", "POST" }, { "err", FunctionUtil.GetFormattedErrorMessage(response?.GetError() as Exception) ?? "No response" } }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() } }, false);
                }
            }
            catch (Exception ex)
            {
                // Swallow errors in response handling - don't break the queue processing
                LogManager.GetInstance().ErrorLog("RESPONSE_HANDLING_ERROR", new Dictionary<string, string> 
                { 
                    { "err", ex.Message } 
                });
            }
        }
    }
}
