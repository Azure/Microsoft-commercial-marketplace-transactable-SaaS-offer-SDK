// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.
namespace Microsoft.Marketplace.SaasKit.Network
{
    using System.IO;
    using System.Net;
    using System.Text.Json;
    using Microsoft.Marketplace.SaasKit.Configurations;
    using Microsoft.Marketplace.SaasKit.Contracts;
    using Microsoft.Marketplace.SaasKit.Models;

    /// <summary>
    /// The Fulfillment Api RestClient.
    /// </summary>
    /// <typeparam name="T"> Generic Type.</typeparam>
    /// <seealso cref="Microsoft.Marketplace.SaasKit.Network.AbstractSaaSApiRestClient{T}" />
    public class FulfillmentApiRestClient<T>
                                            : AbstractSaaSApiRestClient<T>
                                             where T : SaaSApiResult, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FulfillmentApiRestClient{T}"/> class.
        /// </summary>
        /// <param name="clientConfiguration">The client configuration.</param>
        /// <param name="logger">The logger.</param>
        public FulfillmentApiRestClient(SaaSApiClientConfiguration clientConfiguration, ILogger logger)
            : base(clientConfiguration, logger)
        {
        }

        /// <summary>
        /// Processes the error response.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="ex">The ex.</param>
        /// <returns> Generic Value.</returns>
        /// <exception cref="FulfillmentException">
        /// Token expired. Please logout and login again.
        /// or NotFound
        /// or Conflict
        /// or BadRequest
        /// or Internal server error.
        /// </exception>
        protected override T ProcessErrorResponse(string url, WebException ex)
        {
            var httpResponse = ex.Response;
            if (httpResponse != null)
            {
                var webResponse = httpResponse as System.Net.HttpWebResponse;
                if (webResponse != null)
                {
                    this.logger?.Info("Error :: " + new StreamReader(ex.Response.GetResponseStream()).ReadToEnd());
                    if (webResponse.StatusCode == HttpStatusCode.Unauthorized || webResponse.StatusCode == HttpStatusCode.Forbidden)
                    {
                        throw new FulfillmentException("Token expired. Please logout and login again.", SaasApiErrorCode.Unauthorized);
                    }
                    else if (webResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        this.logger?.Warn("Returning the error as " + JsonSerializer.Serialize(new { Error = "Not Found" }));
                        throw new FulfillmentException(string.Format("Unable to find the request {0}", url), SaasApiErrorCode.NotFound);
                    }
                    else if (webResponse.StatusCode == HttpStatusCode.Conflict)
                    {
                        this.logger?.Warn("Returning the error as " + JsonSerializer.Serialize(new { Error = "Conflict" }));
                        throw new FulfillmentException(string.Format("Conflict came for {0}", url), SaasApiErrorCode.Conflict);
                    }
                    else if (webResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        this.logger?.Warn("Returning the error as " + JsonSerializer.Serialize(new { Error = "Bad Request" }));
                        throw new FulfillmentException(string.Format("Unable to process the request {0}, server responding as BadRequest. Please verify the post data. ", url), SaasApiErrorCode.BadRequest);
                    }
                }
            }

            if (httpResponse != null && httpResponse.GetResponseStream() != null)
            {
                using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseAsString = reader.ReadToEnd();
                    var errorFromAPI = JsonSerializer.Deserialize<FulfillmentErrorResult>(responseAsString);

                    this.logger?.Warn("Returning the error as " + JsonSerializer.Serialize(new { Error = responseAsString }));

                    throw new FulfillmentException(errorFromAPI.Error.Message, SaasApiErrorCode.InternalServerError);
                }
            }

            this.logger?.Error("Error while completing the request as " + JsonSerializer.Serialize(new
            {
                Error = httpResponse,
            }));

            return null;
        }
    }
}
