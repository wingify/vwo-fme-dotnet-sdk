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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VWOFmeSdk.Packages.NetworkLayer.Models;
using VWOFmeSdk.Interfaces.Networking;
using VWOFmeSdk.Interfaces.Batching;
using Newtonsoft.Json;
using VWOFmeSdk.Services;
using VWOFmeSdk.Packages.Logger.Enums;
using ConstantsNamespace = VWOFmeSdk.Constants;
using VWOFmeSdk.Enums;
using VWOFmeSdk.Packages.Logger.Core;
using VWOFmeSdk.Packages.NetworkLayer.Manager;

namespace VWOFmeSdk.Packages.NetworkLayer.Client
{
    /// <summary>
    /// Network client implementing ThreadPoolExecutor pattern similar to Ruby SDK.
    /// Uses a fixed thread pool with bounded queue and caller-runs fallback policy.
    /// </summary>
    public class NetworkClient : NetworkClientInterface
    {
        /// <summary>
        /// Minimum number of threads in the pool (similar to Ruby's min_threads: 1).
        /// </summary>
        private const int MIN_THREAD_POOL_SIZE = 1;
        
        /// <summary>
        /// Maximum number of threads in the pool (similar to Ruby's max_threads).
        /// Default: 5 threads (matches Ruby SDK's MAX_POOL_SIZE).
        /// </summary>
        private const int MAX_THREAD_POOL_SIZE = 5;
        
        /// <summary>
        /// Maximum queue size before executing in caller's thread (similar to Ruby's max_queue).
        /// Default: 10000 (matches Ruby SDK's MAX_QUEUE_SIZE).
        /// </summary>
        private const int MAX_QUEUE_SIZE = 10000;
        
        /// <summary>
        /// Timeout for individual HTTP requests in milliseconds (30 seconds).
        /// </summary>
        private const int DEFAULT_CONNECTION_TIMEOUT = 30000;
            
        /// <summary>
        /// Thread-safe bounded queue that holds incoming POST requests waiting to be processed.
        /// Similar to Ruby's ThreadPoolExecutor queue with max_queue limit.
        /// </summary>
        private static ConcurrentQueue<QueuedRequest> requestQueue = new ConcurrentQueue<QueuedRequest>();
        
        /// <summary>
        /// Semaphore to limit concurrent HTTP requests to match thread pool size.
        /// This ensures we never have more concurrent network calls than worker threads.
        /// Critical for preventing connection exhaustion and network failures.
        /// </summary>
        private static SemaphoreSlim concurrentRequestSemaphore = new SemaphoreSlim(MAX_THREAD_POOL_SIZE, MAX_THREAD_POOL_SIZE);
        
        /// <summary>
        /// Event to signal when new requests are added to the queue.
        /// Worker threads wait on this when queue is empty to avoid busy-waiting.
        /// </summary>
        private static ManualResetEventSlim newRequestEvent = new ManualResetEventSlim(false);
        
        /// <summary>
        /// Array of background worker threads that process requests from the queue.
        /// One worker per thread in the pool (MAX_THREAD_POOL_SIZE threads).
        /// Similar to Ruby's ThreadPoolExecutor worker threads.
        /// </summary>
        private static Task[] workerTasks = new Task[MAX_THREAD_POOL_SIZE];
        
        /// <summary>
        /// Token source for graceful cancellation of worker threads.
        /// </summary>
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        
        /// <summary>
        /// Flag indicating whether worker threads are currently running (lazy initialization).
        /// </summary>
        private static volatile bool isProcessing = false;
        
        /// <summary>
        /// Lock object for thread-safe initialization of worker threads (double-checked locking).
        /// </summary>
        private static readonly object lockObject = new object();
        
        /// <summary>
        /// Counter to track total requests enqueued (for error logging and debugging).
        /// </summary>
        private static long totalRequestsEnqueued = 0;
        
        /// <summary>
        /// Thread-safe counter for current queue size to avoid race conditions.
        /// Updated atomically when enqueueing/dequeueing.
        /// </summary>
        private static long currentQueueSize = 0;

        /// <summary>
        /// Static constructor that configures .NET's ServicePointManager for optimal HTTP performance.
        /// Sets connection limit to match thread pool size to prevent connection exhaustion.
        /// </summary>
        static NetworkClient()
        {
            // Set connection limit to match thread pool size
            // This ensures we don't create more connections than we have threads
            // Connection pooling will reuse connections efficiently
            ServicePointManager.DefaultConnectionLimit = MAX_THREAD_POOL_SIZE;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.MaxServicePointIdleTime = DEFAULT_CONNECTION_TIMEOUT;
        }

        public static string ConstructUrl(Dictionary<string, object> networkOptions)
        {
            string hostname = (string)networkOptions["hostname"];
            string path = (string)networkOptions["path"];

            if (networkOptions.ContainsKey("port") && (int)networkOptions["port"] != 0)
            {
                hostname += ":" + networkOptions["port"];
            }

            return networkOptions["scheme"].ToString().ToLower() + "://" + hostname + path;
        }

        /// <summary>
        /// Makes a GET request to the given URL with retry logic
        /// </summary>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        public ResponseModel GET(RequestModel requestModel)
        {
            string endpoint = "";
            string previousError = "";

            // use retryConfig data from requestModel
            var retryConfig = NetworkManager.GetInstance().GetRetryConfig();
            var maxRetries = (int)retryConfig[ConstantsNamespace.Constants.RETRY_MAX_RETRIES];
            var shouldRetry = (bool)retryConfig[ConstantsNamespace.Constants.RETRY_SHOULD_RETRY];

            // Calculate retry delays: baseDelay * (i === 0 ? 1 : multiplier^i) in seconds, convert to milliseconds
            var retryDelays = new List<int>();
            int baseDelay = (int)retryConfig[ConstantsNamespace.Constants.RETRY_INITIAL_DELAY];
            int multiplier = (int)retryConfig[ConstantsNamespace.Constants.RETRY_BACKOFF_MULTIPLIER];

            for (int i = 0; i < maxRetries; i++)
            {
                int delayInSeconds = baseDelay * (i == 0 ? 1 : (int)Math.Pow(multiplier, i));
                retryDelays.Add(delayInSeconds * 1000); // Convert to milliseconds for Thread.Sleep
            }
            
            for (int attempt = 0; attempt <= maxRetries && shouldRetry; attempt++)
            {
                ResponseModel responseModel = new ResponseModel();
                
                try
                {
                    Dictionary<string, object> networkOptions = requestModel.GetOptions();
                    string url = ConstructUrl(networkOptions);
                    
                    // Extract endpoint for logging
                    Uri uri = new Uri(url);
                    endpoint = uri.AbsolutePath;

                    HttpWebRequest request = WebRequest.CreateHttp(url);
                    request.Method = "GET";
                    request.Timeout = DEFAULT_CONNECTION_TIMEOUT;

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        int statusCode = (int)response.StatusCode;
                        responseModel.SetStatusCode(statusCode);

                        // Do NOT retry on 400 (Bad Request)
                        if (statusCode == 400)
                        {
                            LogManager.GetInstance().ErrorLog("NETWORK_CALL_FAILED", new Dictionary<string, string> { { "method", "GET" }, { "err", "GET request failed with 400 (Bad Request). No retries." } }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() }, { "endPoint", endpoint } }, false);
                            responseModel.SetTotalAttempts(attempt);
                            return responseModel;
                        }

                        string contentType = response.ContentType;

                        if (statusCode >= 200 && statusCode < 300 && contentType.Contains("application/json"))
                        {
                            string responseData = reader.ReadToEnd();
                            responseModel.SetData(responseData);
                            
                            // Log success with retries if there were previous failures
                            if (attempt > 0 && !string.IsNullOrEmpty(requestModel.GetLastError()))
                            {
                                LoggerService.Log(LogLevelEnum.INFO, "NETWORK_CALL_SUCCESS_WITH_RETRIES", new Dictionary<string, string>
                                {
                                    { "extraData", $"GET {endpoint}" },
                                    { "attempts", attempt.ToString() },
                                    { "err", requestModel.GetLastError() }
                                });
                                responseModel.SetTotalAttempts(attempt);
                                responseModel.SetError(requestModel.GetLastError());
                            }
                            
                            return responseModel;
                        }

                        string error = $"Invalid response. Status Code: {statusCode}, Response: {response.StatusDescription}";
                        responseModel.SetError(new Exception(error));
                        responseModel.SetTotalAttempts(attempt);
                    }
                }
                catch (WebException webEx)
                {
                    responseModel.SetError(webEx);
                    responseModel.SetTotalAttempts(attempt);
                }
                catch (Exception exception)
                {
                    responseModel.SetError(exception);
                    responseModel.SetTotalAttempts(attempt + 1);
                }

                // Store error message for potential success logging
                var errorObj = responseModel.GetError();
                if (errorObj is Exception ex)
                {
                    requestModel.SetLastError(ex);
                }
                else if (errorObj != null)
                {
                    // Wrap non-exception errors so we still get a meaningful message
                    requestModel.SetLastError(new Exception(errorObj.ToString()));
                }

                // If this is not the last attempt, wait before retrying
                if (attempt < maxRetries)
                {
                    int delay = retryDelays[attempt];
                    
                    // Log retry attempt (delay in seconds)
                    LoggerService.Log(LogLevelEnum.ERROR, "NETWORK_CALL_RETRY_ATTEMPT", new Dictionary<string, string>
                    {
                        { "endPoint", endpoint },
                        { "err", requestModel.GetLastError() },
                        { "delay", (delay / 1000).ToString() },
                        { "attempt", (attempt + 1).ToString() },
                        { "maxRetries", maxRetries.ToString() }
                    });

                    // Wait before retrying (delay is in milliseconds)
                    Thread.Sleep(delay);
                }
                else
                {
                    LogManager.GetInstance().ErrorLog("NETWORK_CALL_RETRY_FAILED", new Dictionary<string, string> { { "endPoint", endpoint }, { "err", requestModel.GetLastError() } }, new Dictionary<string, object> {}, false);
                    return responseModel;
                }
            }
            
            // Should never reach here, but return null if it does
            return null;
        }

        /// <summary>
        /// Makes a POST request to the given URL
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public ResponseModel POST(RequestModel request)
        {
            return POST(request, null);
        }

        /// <summary>
        /// Enqueues a POST request for processing via thread pool executor.
        /// Implements caller_runs fallback policy: if queue is full, executes synchronously in caller's thread.
        /// Similar to Ruby SDK's ThreadPoolExecutor.post() method.
        /// </summary>
        public ResponseModel POST(RequestModel request, IFlushInterface flushCallback)
        {
            var requestNumber = Interlocked.Increment(ref totalRequestsEnqueued);
            var enqueueTime = DateTime.UtcNow;
            
            // Atomically check and increment queue size to avoid race conditions
            // This ensures thread-safe queue size tracking
            long queueSize = Interlocked.Read(ref currentQueueSize);
            if (queueSize >= MAX_QUEUE_SIZE)
            {
                // Queue is full: execute in caller's thread (caller_runs fallback policy)
                // This matches Ruby's fallback_policy: :caller_runs
                LoggerService.Log(LogLevelEnum.DEBUG, "QUEUE_FULL_EXECUTING_SYNC", new Dictionary<string, string>
                {
                    { "method", "POST" },
                    { "queueSize", queueSize.ToString() },
                    { "maxQueueSize", MAX_QUEUE_SIZE.ToString() },
                    { "requestNumber", requestNumber.ToString() }
                });
                
                // Execute synchronously in caller's thread (caller_runs policy)
                var response = ExecutePostRequest(request);
                
                // Invoke flush callback (same logic as async processing)
                if (response != null && response.GetStatusCode() >= 200 && response.GetStatusCode() < 300)
                {
                    flushCallback?.OnFlush(null, request.GetBody());
                }
                else
                {
                    flushCallback?.OnFlush($"Failed with status code: {response?.GetStatusCode()}", null);
                }
                
                return response;
            }
            
            // Queue has space: enqueue for processing by thread pool
            var queuedRequest = new QueuedRequest
            {
                Request = request,
                FlushCallback = flushCallback,
                CompletionSource = new TaskCompletionSource<ResponseModel>(),
                RequestNumber = requestNumber,
                EnqueueTime = enqueueTime
            };

            // Atomically increment queue size before enqueueing (thread-safe)
            Interlocked.Increment(ref currentQueueSize);
            requestQueue.Enqueue(queuedRequest);
            
            // Signal worker threads that a new request is available
            newRequestEvent.Set();
            
            // Lazy initialization: start worker threads if not already running
            if (!isProcessing)
            {
                StartProcessing();
            }

            // Block until the request is processed by a worker thread
            try
            {
                return queuedRequest.CompletionSource.Task.Result;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is TimeoutException)
                {
                    LogManager.GetInstance().ErrorLog("REQUEST_TIMEOUT", new Dictionary<string, string> { { "method", "POST" }, { "requestNumber", requestNumber.ToString() }, { "err", "Request timed out after waiting in queue" } });
                    var errorResponse = new ResponseModel();
                    errorResponse.SetError(ex.InnerException);
                    return errorResponse;
                }
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Starts the thread pool worker threads that process requests from the queue.
        /// Creates MAX_THREAD_POOL_SIZE worker threads (similar to Ruby's ThreadPoolExecutor).
        /// Uses double-checked locking to ensure workers are only started once.
        /// </summary>
        private static void StartProcessing()
        {
            // Fast path: if already processing, return immediately
            if (isProcessing) return;

            // Double-checked locking: acquire lock and check again
            lock (lockObject)
            {
                if (isProcessing) return;
                isProcessing = true;
                
                // Create worker threads (one per thread in the pool)
                // Similar to Ruby's ThreadPoolExecutor which creates max_threads workers
                for (int i = 0; i < MAX_THREAD_POOL_SIZE; i++)
                {
                    workerTasks[i] = Task.Run(ProcessRequestsAsync, cancellationTokenSource.Token);
                }
            }
        }

        /// <summary>
        /// Main loop for worker threads that continuously process requests from the queue.
        /// Each worker thread:
        /// 1. Tries to dequeue a request (non-blocking)
        /// 2. Waits for semaphore slot (limits concurrent HTTP requests)
        /// 3. Processes the request synchronously (like Ruby's Net::HTTP which is blocking)
        /// 4. Releases semaphore slot
        /// 5. If queue is empty, waits for signal that a new request arrived
        /// 
        /// This implements the ThreadPoolExecutor pattern similar to Ruby SDK.
        /// CRITICAL: Process directly in worker thread - NO Task.Run to avoid creating extra threads!
        /// </summary>
        private static async Task ProcessRequestsAsync()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Try to dequeue a request from the queue
                    if (requestQueue.TryDequeue(out QueuedRequest queuedRequest))
                    {
                        // Atomically decrement queue size counter (thread-safe)
                        Interlocked.Decrement(ref currentQueueSize);
                        
                        // Wait for semaphore slot to limit concurrent HTTP requests
                        // This ensures we never exceed MAX_THREAD_POOL_SIZE concurrent requests
                        await concurrentRequestSemaphore.WaitAsync(cancellationTokenSource.Token);
                        
                        try
                        {
                            // Process the request directly in this worker thread
                            // DO NOT use Task.Run - that would create extra threads and break the queue!
                            // This matches Ruby's behavior where worker threads block on HTTP calls
                            ProcessRequest(queuedRequest);
                        }
                        finally
                        {
                            // Always release semaphore slot, allowing another request to proceed
                            concurrentRequestSemaphore.Release();
                        }
                    }
                    else
                    {
                        // Queue is empty: wait for signal that a new request arrived
                        // Use a timeout (1 second) to periodically check for cancellation
                        // This prevents workers from blocking indefinitely
                        try
                        {
                            bool signaled = newRequestEvent.Wait(1000, cancellationTokenSource.Token);
                            newRequestEvent.Reset();
                            
                            // If not signaled, it timed out - continue loop to check queue again
                            // This handles race conditions where a request was added between
                            // the TryDequeue check and the Wait call
                            if (!signaled)
                            {
                                continue;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected when cancellation is requested - exit the loop
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested - exit the loop
                    break;
                }
                catch (Exception ex)
                {
                    // Log unexpected errors but continue processing
                    // This prevents one bad request from stopping all workers
                    LogManager.GetInstance().ErrorLog("QUEUE_PROCESSING_ERROR", new Dictionary<string, string> { { "err", ex.Message }, { "stackTrace", ex.StackTrace ?? "" } });
                }
            }
        }

        /// <summary>
        /// Processes a single queued request by executing the HTTP POST synchronously.
        /// Invokes the flush callback on success/failure and sets the completion source result.
        /// Similar to Ruby's ThreadPoolExecutor task execution.
        /// </summary>
        /// <param name="queuedRequest">The queued request to process</param>
        private static void ProcessRequest(QueuedRequest queuedRequest)
        {
            try
            {
                // Execute the HTTP request synchronously (like Ruby's blocking Net::HTTP)
                var response = ExecutePostRequest(queuedRequest.Request);
                
                // Invoke flush callback based on response status
                // Success (2xx): pass null error and the request body
                // Failure: pass error message and null body
                if (response != null && response.GetStatusCode() >= 200 && response.GetStatusCode() < 300)
                {
                    queuedRequest.FlushCallback?.OnFlush(null, queuedRequest.Request.GetBody());
                }
                else
                {
                    queuedRequest.FlushCallback?.OnFlush($"Failed with status code: {response?.GetStatusCode()}", null);
                }

                // Signal the waiting caller that the request is complete
                queuedRequest.CompletionSource.SetResult(response);
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                LogManager.GetInstance().ErrorLog("REQUEST_PROCESSING_ERROR", new Dictionary<string, string> { { "method", "POST" }, { "requestNumber", queuedRequest.RequestNumber.ToString() }, { "err", ex.Message } });

                // Notify flush callback of the error
                queuedRequest.FlushCallback?.OnFlush($"Error occurred while processing request: {ex.Message}", null);

                // Create error response and signal completion
                var errorResponse = new ResponseModel();
                errorResponse.SetError(ex);
                queuedRequest.CompletionSource.SetResult(errorResponse);
            }
        }

        private static ResponseModel ExecutePostRequest(RequestModel request)
        {
            string endpoint = "";

            // use retryConfig data from NetworkManager
            var retryConfig = NetworkManager.GetInstance().GetRetryConfig();
            var maxRetries = (int)retryConfig[ConstantsNamespace.Constants.RETRY_MAX_RETRIES];
            var shouldRetry = (bool)retryConfig[ConstantsNamespace.Constants.RETRY_SHOULD_RETRY];

            // Calculate retry delays: baseDelay * (i === 0 ? 1 : multiplier^i) in seconds, convert to milliseconds
            var retryDelays = new List<int>();
            int baseDelay = (int)retryConfig[ConstantsNamespace.Constants.RETRY_INITIAL_DELAY];
            int multiplier = (int)retryConfig[ConstantsNamespace.Constants.RETRY_BACKOFF_MULTIPLIER];

            for (int i = 0; i < maxRetries; i++)
            {
                int delayInSeconds = baseDelay * (i == 0 ? 1 : (int)Math.Pow(multiplier, i));
                retryDelays.Add(delayInSeconds * 1000); // Convert to milliseconds for Thread.Sleep
            }
            
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                ResponseModel responseModel = new ResponseModel();

                try
                {
                    Dictionary<string, object> networkOptions = request.GetOptions();
                    string url = ConstructUrl(networkOptions);
                    
                    // Extract endpoint for logging
                    Uri uri = new Uri(url);
                    endpoint = uri.AbsolutePath;

                    HttpWebRequest connection = WebRequest.CreateHttp(url);
                    connection.Method = "POST";
                    connection.Accept = "application/json";
                    connection.Timeout = DEFAULT_CONNECTION_TIMEOUT;

                    if (networkOptions.ContainsKey("headers"))
                    {
                        Dictionary<string, string> headers = (Dictionary<string, string>)networkOptions["headers"];
                        foreach (KeyValuePair<string, string> header in headers)
                        {
                            connection.Headers.Add(header.Key, header.Value);
                        }
                    }

                    using (StreamWriter streamWriter = new StreamWriter(connection.GetRequestStream()))
                    {
                        object body = networkOptions["body"];
                        string jsonBody = JsonConvert.SerializeObject(body);
                        streamWriter.Write(jsonBody);
                    }

                    using (HttpWebResponse httpResponse = (HttpWebResponse)connection.GetResponse())
                    using (Stream responseStream = httpResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        int statusCode = (int)httpResponse.StatusCode;
                        responseModel.SetStatusCode(statusCode);

                        // Do NOT retry on 400 (Bad Request)
                        if (statusCode == 400)
                        {
                            LogManager.GetInstance().ErrorLog("NETWORK_CALL_FAILED", new Dictionary<string, string> { { "method", "POST" }, { "err", "POST request failed with 400 (Bad Request). No retries." } }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() }, { "endPoint", endpoint } }, false);
                            responseModel.SetTotalAttempts(attempt);
                            return responseModel;
                        }

                        string responseData = reader.ReadToEnd();
                        responseModel.SetData(responseData);

                        if (statusCode >= 200 && statusCode < 300)
                        {
                            // Log success with retries if there were previous failures
                            if (attempt > 0 && !string.IsNullOrEmpty(request.GetLastError()))
                            {
                                LoggerService.Log(LogLevelEnum.INFO, "NETWORK_CALL_SUCCESS_WITH_RETRIES", new Dictionary<string, string>
                                {
                                    { "extraData", $"POST {endpoint}" },
                                    { "attempts", attempt.ToString() },
                                    { "err", request.GetLastError() }
                                });
                                responseModel.SetTotalAttempts(attempt);
                                responseModel.SetError(request.GetLastError());
                            }
                            
                            return responseModel;
                        }

                        string error = $"Request failed. Status Code: {statusCode}, Response: {responseData}";
                        responseModel.SetError(new Exception(error));
                        responseModel.SetTotalAttempts(attempt);
                    }
                }
                catch (WebException webEx)
                {
                    string responseText = "";
                    try
                    {
                        if (webEx.Response != null)
                        {
                            using (var responseStream = webEx.Response.GetResponseStream())
                            using (var reader = new StreamReader(responseStream))
                            {
                                responseText = reader.ReadToEnd();
                            }
                        }
                    }
                    catch { }
                    responseModel.SetError(webEx);
                    responseModel.SetTotalAttempts(attempt);
                }
                catch (Exception exception)
                {
                    responseModel.SetError(exception);
                    responseModel.SetTotalAttempts(attempt);
                }

                // Store error message for potential success logging
                request.SetLastError((responseModel.GetError() as Exception));

                // If this is not the last attempt, wait before retrying
                if (attempt < maxRetries && shouldRetry)
                {
                    int delay = retryDelays[attempt];
                    
                    // Log retry attempt (delay in seconds)
                    LoggerService.Log(LogLevelEnum.ERROR, "NETWORK_CALL_RETRY_ATTEMPT", new Dictionary<string, string>
                    {
                        { "endPoint", endpoint },
                        { "err", request.GetLastError() },
                        { "delay", (delay / 1000).ToString() },
                        { "attempt", (attempt + 1).ToString() },
                        { "maxRetries", maxRetries.ToString() }
                    });

                    // Wait before retrying (delay is in milliseconds)
                    Thread.Sleep(delay);
                }
                else
                {
                    // Check if this is a debugger event
                    bool isDebuggerEvent = false;
                    Dictionary<string, object> networkOptions = request.GetOptions();
                    if (networkOptions.ContainsKey("path"))
                    {
                        string path = networkOptions["path"].ToString();
                        if (path.Contains(EventEnum.VWO_DEBUGGER_EVENT.GetValue()))
                        {
                            isDebuggerEvent = true;
                        }
                    }

                    // // Only log failure if it's NOT a debugger event
                    if (!isDebuggerEvent)
                    {
                        LogManager.GetInstance().ErrorLog("NETWORK_CALL_RETRY_FAILED", new Dictionary<string, string> 
                        { 
                            { "endPoint", endpoint }, 
                            { "err", request.GetLastError() },
                            { "attempts", attempt.ToString() }
                        }, new Dictionary<string, object> {}, false);
                    }
                    
                    responseModel.SetTotalAttempts(attempt);
                    return responseModel;
                }
            }
            
            // Should never reach here, but return null if it does
            return null;
        }
    }

    internal class QueuedRequest
    {
        public RequestModel Request { get; set; }
        public IFlushInterface FlushCallback { get; set; }
        public TaskCompletionSource<ResponseModel> CompletionSource { get; set; }
        public long RequestNumber { get; set; }
        public DateTime EnqueueTime { get; set; }
    }
}