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
using System.Collections.Concurrent;
using System.Threading;
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
        internal ConcurrentQueue<Dictionary<string, object>> batchQueue = new ConcurrentQueue<Dictionary<string, object>>();
        internal int eventsPerRequest = ConstantsNamespace.Constants.DEFAULT_EVENTS_PER_REQUEST ;
        internal int requestTimeInterval = ConstantsNamespace.Constants.DEFAULT_REQUEST_TIME_INTERVAL;
        internal Timer timer;
        private readonly object _lock = new object();
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

            LoggerService.Log(LogLevelEnum.DEBUG, "BatchEventQueue initialized with eventsPerRequest: " + eventsPerRequest + " and requestTimeInterval: " + requestTimeInterval);
        }

        /// <summary>
        /// Enqueues an event to the batch queue.
        /// </summary>
        /// <param name="eventData">The event data to enqueue.</param>
        public void Enqueue(Dictionary<string, object> eventData)
        {
            batchQueue.Enqueue(eventData);
            LoggerService.Log(LogLevelEnum.DEBUG, $"Event added to queue. Current queue size: {batchQueue.Count}");

            // Lazily create the timer when the first event is enqueued
            // Check outside the lock first to avoid unnecessary contention
            if(timer == null && !batchQueue.IsEmpty)
            {
                lock (_lock)
                {
                    // Check again inside the lock to handle the race condition
                    // where multiple threads might have passed the first check
                    if (timer == null && !batchQueue.IsEmpty)
                    {
                        CreateNewBatchTimer();
                    }
                }
            }

            // If batch size reaches the limit, trigger flush
            if (batchQueue.Count >= eventsPerRequest)
            {
                LoggerService.Log(LogLevelEnum.DEBUG, "Queue reached max capacity, flushing now...");
                Flush();
            }
        }

        /// <summary>
        /// Creates a new timer for batch flushing.
        /// </summary>
        private void CreateNewBatchTimer()
        {
            timer = new Timer(_ => Flush(), null, requestTimeInterval * 1000, requestTimeInterval * 1000);
        }

        /// <summary>
        /// Flushes the batch queue by sending all events to the server.
        /// </summary>
        /// <returns>True if events were successfully sent, false if skipped or failed to send</returns>
        public void Flush(bool isManualFlush = false)
        {
            lock (_lock)
            {
                if (isManualFlush)
                {
                    if (!batchQueue.IsEmpty)
                    {
                        int queueSize = batchQueue.Count;
                        LoggerService.Log(LogLevelEnum.DEBUG, $"Manual flush initiated with queue size: {queueSize}");

                        // In manual flush, flush all the events present in the queue at this moment in a single batch
                        var eventsToSend = DequeueBatch(queueSize);

                        LoggerService.Log(LogLevelEnum.INFO, $"Flushing {eventsToSend.Count} events manually.");

                        SendBatchEvents(eventsToSend);
                           
                        // Stop the timer only if the queue is empty after manual flush
                        if (batchQueue.IsEmpty)
                        {
                            StopTimer();
                        }

                    }
                    else
                    {
                        // If the queue is empty during a manual flush
                        LoggerService.Log(LogLevelEnum.DEBUG, "Queue is empty. No events to flush.");
                        // Stop the timer if there are no events
                        StopTimer();
                    }
                }
                else
                {   
                    //flush at most eventsPerRequest events in a single batch
                    List<Dictionary<string, object>> eventsToSend = DequeueBatch(eventsPerRequest);

                    SendBatchEvents(eventsToSend);
            
                    // Stop the timer only if the queue is empty after this automatic flush
                    if (batchQueue.IsEmpty)
                    {
                        StopTimer();
                    }

                }
            }
        }

        /// <summary>
        /// Flushes the queue and clears the timer
        /// </summary>
        public void FlushAndClearTimer()
        {
            // First, flush the events manually
            Flush(true);
            // Explicitly clear the timer irrespective of queue state (used during shutdown)
            StopTimer(force: true);
        }

        /// <summary>
        /// Dequeues a batch of events from the queue.
        /// </summary>
        /// <param name="maxBatchSize">The maximum number of events to dequeue </param>
        /// <returns>A list of events to dequeue.</returns>
        private List<Dictionary<string, object>> DequeueBatch(int maxBatchSize)
        {
            var eventsToSend = new List<Dictionary<string, object>>(maxBatchSize);

            // Dequeue up to maxBatchSize events from the queue
            for (int i = 0; i < maxBatchSize; i++)
            {
                if (batchQueue.TryDequeue(out var eventItem))
                {
                    eventsToSend.Add(eventItem);
                }
                else
                {
                    break;
                }
            }

            return eventsToSend;
        }

        /// <summary>
        /// Stops the timer if it is running.
        /// </summary>
        /// <param name="force">If true, stops the timer even if the queue is not empty.</param>
        private void StopTimer(bool force = false)
        {
            lock (_lock)
            {
                if (timer != null)
                {
                    if (force || batchQueue.IsEmpty)
                    {
                        timer.Dispose();
                        timer = null;
                    }
                }
            }
        }

        /// <summary>
        /// Sends a batch of events to the server.
        /// </summary>
        /// <param name="events">The batch of events to send.</param>
        private void SendBatchEvents(List<Dictionary<string, object>> events)
        {
            try
            {
                NetworkUtil.SendPostBatchRequest(events, accountId, sdkKey, flushCallback);
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "BATCH_FLUSH_FAILED", new Dictionary<string, string> { { "err", ex.Message } });
            }
        }
    }
}