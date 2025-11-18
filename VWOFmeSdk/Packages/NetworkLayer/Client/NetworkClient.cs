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

namespace VWOFmeSdk.Packages.NetworkLayer.Client
{
    public class NetworkClient : NetworkClientInterface
    {
        /// Maximum number of concurrent connections to the server.
        private const int DEFAULT_MAX_CONNECTIONS = 20;
        
        /// Timeout for individual HTTP requests in milliseconds (30 seconds).
        private const int DEFAULT_CONNECTION_TIMEOUT = 30000;

            
        /// Thread-safe queue that holds incoming POST requests waiting to be processed.
        private static ConcurrentQueue<QueuedRequest> requestQueue = new ConcurrentQueue<QueuedRequest>();
        
        /// Semaphore that controls the maximum number of concurrent network requests.
        private static SemaphoreSlim connectionSemaphore = new SemaphoreSlim(GetMaxConnections(), GetMaxConnections());
        
        /// Event to signal when new requests are added to the queue.
        private static ManualResetEventSlim newRequestEvent = new ManualResetEventSlim(false);
        
        /// Array of background worker tasks that process requests from the queue.
        private static Task[] workerTasks = new Task[GetMaxConnections()];
        
        /// Token source for graceful cancellation of worker tasks.
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        
        private static volatile bool isProcessing = false;
        
        private static readonly object lockObject = new object();
        
        /// Counter to track total requests enqueued (for error logging).
        private static long totalRequestsEnqueued = 0;

        static NetworkClient()
        {
            ServicePointManager.DefaultConnectionLimit = GetMaxConnections();
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.MaxServicePointIdleTime = 30000;
        }

        private static int GetMaxConnections()
        {
            return DEFAULT_MAX_CONNECTIONS;
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
        /// Makes a GET request to the given URL
        /// </summary>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        public ResponseModel GET(RequestModel requestModel)
        {
            ResponseModel responseModel = new ResponseModel();

            try
            {
                Dictionary<string, object> networkOptions = requestModel.GetOptions();
                string url = ConstructUrl(networkOptions);

                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = "GET";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    int statusCode = (int)response.StatusCode;
                    responseModel.SetStatusCode(statusCode);

                    string contentType = response.ContentType;

                    if (statusCode != 200 || !contentType.Contains("application/json"))
                    {
                        string error = $"Invalid response. Status Code: {statusCode}, Response: {response.StatusDescription}";
                        responseModel.SetError(new Exception(error));
                        return responseModel;
                    }

                    string responseData = reader.ReadToEnd();
                    responseModel.SetData(responseData);
                }

                return responseModel;
            }
            catch (Exception exception)
            {
                responseModel.SetError(exception);
                return responseModel;
            }
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

        public ResponseModel POST(RequestModel request, IFlushInterface flushCallback)
        {
            var requestNumber = Interlocked.Increment(ref totalRequestsEnqueued);
            var enqueueTime = DateTime.UtcNow;
            
            var queuedRequest = new QueuedRequest
            {
                Request = request,
                FlushCallback = flushCallback,
                CompletionSource = new TaskCompletionSource<ResponseModel>(),
                RequestNumber = requestNumber,
                EnqueueTime = enqueueTime
            };

            requestQueue.Enqueue(queuedRequest);
            
            // Signal that a new request is available
            newRequestEvent.Set();
            
            if (!isProcessing)
            {
                StartProcessing();
            }

            try
            {
                return queuedRequest.CompletionSource.Task.Result;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is TimeoutException)
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "REQUEST_TIMEOUT", new Dictionary<string, string>
                    {
                        { "method", "POST" },
                        { "requestNumber", requestNumber.ToString() },
                        { "err", "Request timed out after waiting in queue" }
                    });
                    var errorResponse = new ResponseModel();
                    errorResponse.SetError(ex.InnerException);
                    return errorResponse;
                }
                throw ex.InnerException;
            }
        }

        private static void StartProcessing()
        {
            if (isProcessing) return;

            lock (lockObject)
            {
                if (isProcessing) return;
                isProcessing = true;
                
                for (int i = 0; i < GetMaxConnections(); i++)
                {
                    workerTasks[i] = Task.Run(ProcessRequestsAsync, cancellationTokenSource.Token);
                }
            }
        }

        private static async Task ProcessRequestsAsync()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // First, try to get a request from the queue (without blocking on semaphore)
                    if (requestQueue.TryDequeue(out QueuedRequest queuedRequest))
                    {
                        // We have a request, now wait for a connection slot to become available
                        await connectionSemaphore.WaitAsync(cancellationTokenSource.Token);
                        
                        // Process the request (semaphore will be released in ProcessRequestAsync)
                        await ProcessRequestAsync(queuedRequest);
                    }
                    else
                    {
                        // No requests in queue, wait for signal that a new request arrived
                        // Use a timeout to periodically check for cancellation
                        try
                        {
                            bool signaled = newRequestEvent.Wait(1000, cancellationTokenSource.Token);
                            newRequestEvent.Reset();
                            // If not signaled, it timed out - continue loop to check queue again
                            if (!signaled)
                            {
                                continue;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected when cancellation is requested
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    LoggerService.Log(LogLevelEnum.ERROR, "QUEUE_PROCESSING_ERROR", new Dictionary<string, string>
                    {
                        { "err", ex.Message }
                    });
                }
            }
        }

        private static async Task ProcessRequestAsync(QueuedRequest queuedRequest)
        {
            try
            {
                var response = await Task.Run(() => ExecutePostRequest(queuedRequest.Request));
                
                if (response != null && response.GetStatusCode() >= 200 && response.GetStatusCode() < 300)
                {
                    queuedRequest.FlushCallback?.OnFlush(null, queuedRequest.Request.GetBody());
                }
                else
                {
                    queuedRequest.FlushCallback?.OnFlush($"Failed with status code: {response?.GetStatusCode()}", null);
                }

                queuedRequest.CompletionSource.SetResult(response);
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "REQUEST_PROCESSING_ERROR", new Dictionary<string, string>
                {
                    { "method", "POST" },
                    { "requestNumber", queuedRequest.RequestNumber.ToString() },
                    { "err", ex.Message }
                });

                queuedRequest.FlushCallback?.OnFlush($"Error occurred while processing request: {ex.Message}", null);

                var errorResponse = new ResponseModel();
                errorResponse.SetError(ex);
                queuedRequest.CompletionSource.SetResult(errorResponse);
            }
            finally
            {
                connectionSemaphore.Release();
            }
        }

        private static ResponseModel ExecutePostRequest(RequestModel request)
        {
            ResponseModel responseModel = new ResponseModel();

            try
            {
                Dictionary<string, object> networkOptions = request.GetOptions();
                string url = ConstructUrl(networkOptions);

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

                    string responseData = reader.ReadToEnd();
                    responseModel.SetData(responseData);

                    if (statusCode != 200)
                    {
                        string error = $"Request failed. Status Code: {statusCode}, Response: {responseData}";
                        responseModel.SetError(new Exception(error));
                    }
                }

                return responseModel;
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

                LoggerService.Log(LogLevelEnum.ERROR, "NETWORK_CALL_FAILED", new Dictionary<string, string>
                {
                    { "method", "POST" },
                    { "err", webEx.Message },
                    { "response", responseText }
                });
                responseModel.SetError(webEx);
                return responseModel;
            }
            catch (Exception exception)
            {
                LoggerService.Log(LogLevelEnum.ERROR, "NETWORK_CALL_FAILED", new Dictionary<string, string>
                {
                    { "method", "POST" },
                    { "err", exception.Message }
                });
                responseModel.SetError(exception);
                return responseModel;
            }
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