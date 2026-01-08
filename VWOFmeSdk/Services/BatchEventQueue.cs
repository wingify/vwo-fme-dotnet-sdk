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
using System.Threading;
using System.Threading.Tasks;
using VWOFmeSdk.Utils;
using VWOFmeSdk.Packages.Logger.Enums;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Constants;
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Interfaces.Batching;
using VWOFmeSdk.Enums;

namespace VWOFmeSdk.Services
{
    public class BatchEventQueue
    {
        internal Queue<Dictionary<string, object>> batchQueue = new Queue<Dictionary<string, object>>();
        internal int eventsPerRequest = ConstantsNamespace.Constants.DEFAULT_EVENTS_PER_REQUEST ;
        internal int requestTimeInterval = ConstantsNamespace.Constants.DEFAULT_REQUEST_TIME_INTERVAL;
        internal Timer timer;
        internal bool isBatchProcessing = false;
        private readonly int accountId;
        private readonly string sdkKey;
        private IFlushInterface flushCallback;

        public BatchEventQueue(int eventsPerRequest, int requestTimeInterval, int accountId, string sdkKey, IFlushInterface flushCallback = null)
        {
            this.eventsPerRequest = eventsPerRequest;
            this.requestTimeInterval = requestTimeInterval;
            this.accountId = accountId;
            this.sdkKey = sdkKey;
            this.flushCallback = flushCallback;

            CreateNewBatchTimer();
            LoggerService.Log(LogLevelEnum.DEBUG, "BatchEventQueue initialized with eventsPerRequest: " + eventsPerRequest + " and requestTimeInterval: " + requestTimeInterval);
        }

        public void Enqueue(Dictionary<string, object> eventData)
        {
            batchQueue.Enqueue(eventData);
            LoggerService.Log(LogLevelEnum.DEBUG, $"Event added to queue. Current queue size: {batchQueue.Count}");

            // If batch size reaches the limit, trigger flush
            if (batchQueue.Count >= eventsPerRequest)
            {
                LoggerService.Log(LogLevelEnum.DEBUG, "Queue reached max capacity, flushing now...");
                Flush();
            }
        }

        private void CreateNewBatchTimer()
        {
            timer = new Timer(_ => Flush(), null, requestTimeInterval * 1000, requestTimeInterval * 1000);
        }

        /// <summary>
        /// Flushes the batch queue by sending all events to the server.
        /// </summary>
        /// <returns>True if events were successfully sent, false if skipped or failed to send</returns>
        public bool Flush(bool isManualFlush = false)
        {
              
            if (isManualFlush)
            {
                if (batchQueue.Count > 0)
                {
                    LoggerService.Log(LogLevelEnum.DEBUG, "Manual flush initiated.");
                    LoggerService.Log(LogLevelEnum.DEBUG, $"Queue size: {batchQueue.Count}");

                    // Create a temporary list to hold the events for the batch
                    List<Dictionary<string, object>> eventsToSend = new List<Dictionary<string, object>>(batchQueue);
                    batchQueue.Clear(); // Clear the queue after taking a snapshot

                    // Log before sending batch events
                    LoggerService.Log(LogLevelEnum.DEBUG, $"Flushing {eventsToSend.Count} events manually.");
                    bool isSentSuccessfully = false;

                    Task.Run(() =>
                    {
                        try
                        {
                            isSentSuccessfully = SendBatchEvents(eventsToSend);
                            if (isSentSuccessfully)
                            {
                                LoggerService.Log(LogLevelEnum.INFO, $"Batch flush successful. Sent {eventsToSend.Count} events.");
                            }
                            else
                            {
                                LogManager.GetInstance().ErrorLog("BATCH_FLUSH_FAILED", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.BATCH_FLUSH.GetValue() } });
                                
                                // Re-enqueue events in case of failure
                                foreach (var eventItem in eventsToSend)
                                {
                                    batchQueue.Enqueue(eventItem);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetInstance().ErrorLog("BATCH_FLUSH_FAILED", new Dictionary<string, string> { { "err", FunctionUtil.GetFormattedErrorMessage(ex) } }, new Dictionary<string, object> { { "an", ApiEnum.BATCH_FLUSH.GetValue() } });
                            isSentSuccessfully = false;
                        }
                    });
                    // Clear the timer and set it to null
                    if (timer != null)
                    {
                        timer.Dispose();
                        timer = null;
                    }
                    CreateNewBatchTimer();

                    return isSentSuccessfully;
                }
                else
                {
                    // If the queue is empty during a manual flush
                    LoggerService.Log(LogLevelEnum.DEBUG, "Queue is empty. No events to flush.");
                    return true;
                }
            }

            
            if(!isBatchProcessing) 
            {
                isBatchProcessing = true;

                // Create a temporary list to hold the events for the batch
                List<Dictionary<string, object>> eventsToSend = new List<Dictionary<string, object>>(batchQueue);
                batchQueue.Clear(); // Clear the queue after taking a snapshot

                // Log before sending batch events
                LoggerService.Log(LogLevelEnum.DEBUG, $"Flushing {eventsToSend.Count} events.");

                bool isSentSuccessfully = false;

                // Send the batch events in a background task
                Task.Run(() =>
                {
                    try
                    {
                        isSentSuccessfully = SendBatchEvents(eventsToSend);
                        if (isSentSuccessfully)
                        {
                            LoggerService.Log(LogLevelEnum.INFO, $"Batch flush successful. Sent {eventsToSend.Count} events.");
                        }
                        else
                        {
                            LogManager.GetInstance().ErrorLog("BATCH_FLUSH_FAILED", new Dictionary<string, string> { }, new Dictionary<string, object> { { "an", ApiEnum.BATCH_FLUSH.GetValue() } });
                            // Re-enqueue events in case of failure
                            foreach (var eventItem in eventsToSend)
                            {
                                batchQueue.Enqueue(eventItem);
                            }
                            isSentSuccessfully = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.GetInstance().ErrorLog("BATCH_FLUSH_FAILED", new Dictionary<string, string> { { "err", FunctionUtil.GetFormattedErrorMessage(ex) } }, new Dictionary<string, object> { { "an", ApiEnum.BATCH_FLUSH.GetValue() } });
                        isSentSuccessfully = false;
                    }
                    finally
                    {
                        isBatchProcessing = false;
                    }
                });
                // Clear the timer and set it to null
                if (timer != null)
                {
                    timer.Dispose();
                    timer = null;
                }
                CreateNewBatchTimer();

                return isSentSuccessfully;
            }
            else {
                // Prevent flushing if another flush is in progress
                LoggerService.Log(LogLevelEnum.DEBUG, "Flush skipped. Another flush is already in progress.");
                return false;
            }
        }

        /// <summary>
        /// Flushes the queue and clears the timer
        /// </summary>
        /// <returns>True if flush was successful, false otherwise</returns>
        public bool FlushAndClearTimer()
        {
            bool flushResult = false;

            // First, flush the events manually
            flushResult = Flush(true);

            return flushResult;
        }

        private bool SendBatchEvents(List<Dictionary<string, object>> events)
        {
            try
            {
                bool isSentSuccessfully = NetworkUtil.SendPostBatchRequest(events, accountId, sdkKey, flushCallback);
                return isSentSuccessfully;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}