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
#pragma warning disable 1587

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using VWOFmeSdk.Packages.NetworkLayer.Models;
using VWOFmeSdk.Interfaces.Networking;
using Newtonsoft.Json;
using VWOFmeSdk.Services;
using VWOFmeSdk.Packages.Logger.Enums;

namespace VWOFmeSdk.Packages.NetworkLayer.Client
{
    public class NetworkClient : NetworkClientInterface
    {
        /// <summary>
        /// Constructs the URL from the network options
        /// </summary>
        /// <param name="networkOptions"></param>
        /// <returns></returns>
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
            ResponseModel responseModel = new ResponseModel();

            try
            {
                Dictionary<string, object> networkOptions = request.GetOptions();
                string url = ConstructUrl(networkOptions);

                HttpWebRequest connection = WebRequest.CreateHttp(url);
                connection.Method = "POST";
                connection.Accept = "application/json";
                connection.Timeout = 5000;

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
                using (var responseStream = webEx.Response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    string responseText = reader.ReadToEnd();
                    LoggerService.Log(LogLevelEnum.ERROR, "NETWORK_CALL_FAILED", new Dictionary<string, string>
                    {
                        { "method", "POST" },
                        { "err", webEx.Message },
                        { "response", responseText }
                    });
                }
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
}
