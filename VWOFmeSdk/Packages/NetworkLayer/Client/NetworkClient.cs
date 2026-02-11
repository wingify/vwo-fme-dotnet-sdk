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
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
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
    /// Network client for making HTTP requests.
    /// </summary>
    public class NetworkClient : NetworkClientInterface
    {
        /// <summary>
        /// Timeout for individual HTTP requests in milliseconds (30 seconds).
        /// </summary>
        private const int DEFAULT_CONNECTION_TIMEOUT = 30000;

        /// <summary>
        /// Background worker that processes POST requests from the queue.
        /// </summary>
        private static readonly PostRequestBackgroundWorker backgroundWorker;

        /// <summary>
        /// Gets the background worker instance
        /// </summary>
        public static PostRequestBackgroundWorker GetBackgroundWorker()
        {
            return backgroundWorker;
        }

        /// <summary>
        /// Static constructor that configures .NET's ServicePointManager and initializes the background worker.
        /// </summary>
        static NetworkClient()
        {
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.MaxServicePointIdleTime = DEFAULT_CONNECTION_TIMEOUT;
            
            // Start background worker to process queued POST requests
            backgroundWorker = new PostRequestBackgroundWorker();
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
                        if (statusCode == ConstantsNamespace.Constants.HTTP_BAD_REQUEST)
                        {
                            LogManager.GetInstance().ErrorLog("NETWORK_CALL_FAILED", new Dictionary<string, string> { { "method", "GET" }, { "err", "GET request failed with 400 (Bad Request). No retries." } }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() }, { "endPoint", endpoint } }, false);
                            responseModel.SetTotalAttempts(attempt);
                            return responseModel;
                        }

                        string contentType = response.ContentType;

                        if (statusCode >= ConstantsNamespace.Constants.HTTP_SUCCESS_MIN && statusCode <= ConstantsNamespace.Constants.HTTP_SUCCESS_MAX && contentType.Contains("application/json"))
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
        /// Sends a POST request to the server synchronously.
        /// This method executes the request directly without queuing.
        /// For async queue-based processing, enqueue the request using PostRequestChannel before calling this.
        /// </summary>
        /// <param name="request">The RequestModel containing the URL, headers, and body of the POST request.</param>
        /// <param name="flushCallback">Optional callback for batch operations</param>
        /// <returns>A ResponseModel containing the response data.</returns>
        public ResponseModel POST(RequestModel request, IFlushInterface flushCallback)
        {
            try
            {
                // Execute the HTTP request directly
                var response = ExecutePostRequest(request);
                
                // Invoke flush callback based on response status
                HandleFlushCallback(response, request, flushCallback);
                return response;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                LogManager.GetInstance().ErrorLog("REQUEST_PROCESSING_ERROR", new Dictionary<string, string> 
                { 
                    { "method", "POST" }, 
                    { "err", ex.Message }
                });

                // Notify flush callback of the error
                flushCallback?.OnFlush($"Error occurred while processing request: {ex.Message}", null);

                // Create error response
                var errorResponse = new ResponseModel();
                errorResponse.SetError(ex);
                return errorResponse;
            }
        }

        /// <summary>
        /// Handles the flush callback based on response status.
        /// </summary>
        private static void HandleFlushCallback(ResponseModel response, RequestModel request, IFlushInterface flushCallback)
        {
            if (flushCallback == null) return;

            if (response != null && response.GetStatusCode() >= ConstantsNamespace.Constants.HTTP_SUCCESS_MIN && response.GetStatusCode() <= ConstantsNamespace.Constants.HTTP_SUCCESS_MAX)
            {
                flushCallback.OnFlush(null, request.GetBody());
            }
            else
            {
                flushCallback.OnFlush($"Failed with status code: {response?.GetStatusCode()}", null);
            }
        }

        internal static ResponseModel ExecutePostRequest(RequestModel request)
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
                        if (statusCode == ConstantsNamespace.Constants.HTTP_BAD_REQUEST)
                        {
                            LogManager.GetInstance().ErrorLog("NETWORK_CALL_FAILED", new Dictionary<string, string> { { "method", "POST" }, { "err", "POST request failed with 400 (Bad Request). No retries." } }, new Dictionary<string, object> { { "an", ApiEnum.GET_FLAG.GetValue() }, { "endPoint", endpoint } }, false);
                            responseModel.SetTotalAttempts(attempt);
                            return responseModel;
                        }

                        string responseData = reader.ReadToEnd();
                        responseModel.SetData(responseData);

                        if (statusCode >= ConstantsNamespace.Constants.HTTP_SUCCESS_MIN && statusCode < ConstantsNamespace.Constants.HTTP_SUCCESS_UPPER_BOUND)
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
}