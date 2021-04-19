// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.
namespace Microsoft.Marketplace.SaasKit.Network
{
    using System.IO;
    using System.Net;
    using System.Text.Json;
    using Microsoft.Marketplace.SaasKit.Configurations;
    using Microsoft.Marketplace.SaasKit.Contracts;
    using Microsoft.Marketplace.SaasKit.Exceptions;
    using Microsoft.Marketplace.SaasKit.Models;

    /// <summary>
    /// Metering Api RestClient.
    /// </summary>
    /// <typeparam name="T"> Generic.</typeparam>
    /// <seealso cref="Microsoft.Marketplace.SaasKit.Network.AbstractSaaSApiRestClient{T}" />
    public class MeteringApiRestClient<T> : AbstractSaaSApiRestClient<T>
                                  where T : SaaSApiResult, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MeteringApiRestClient{T}" /> class.
        /// </summary>
        /// <param name="clientConfiguration">The client configuration.</param>
        /// <param name="logger">The logger.</param>
        public MeteringApiRestClient(SaaSApiClientConfiguration clientConfiguration, ILogger logger)
                                                                    : base(clientConfiguration, logger)
        {
        }

        /// <summary>
        /// Processes the error response.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="ex">The ex.</param>
        /// <returns>
        /// Error result built using the data in the response.
        /// </returns>
        /// <exception cref="MeteredBillingException">Exception.</exception>
        /// <exception cref="Microsoft.Marketplace.SaasKit.Exceptions.MeteredBillingException">Token expired. Please logout and login again.
        /// Not Found.
        /// Bad Request.
        /// Internal Server error.</exception>
        protected override T ProcessErrorResponse(string url, WebException ex)
        {
            var webResponse = ex.Response as System.Net.HttpWebResponse;
            if (webResponse != null)
            {
                string responseString = string.Empty;
                MeteringErrorResult meteredBillingErrorResult = new MeteringErrorResult();

                using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
                {
                    responseString = reader.ReadToEnd();
                    meteredBillingErrorResult = JsonSerializer.Deserialize<MeteringErrorResult>(responseString);
                }

                this.logger?.Info("Error :: " + responseString);

                if (webResponse.StatusCode == HttpStatusCode.Unauthorized || webResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new MeteredBillingException("Token expired. Please logout and login again.", SaasApiErrorCode.Unauthorized, meteredBillingErrorResult);
                }
                else if (webResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    this.logger?.Warn("Returning the error as " + JsonSerializer.Serialize(new { Error = "Not Found" }));
                    throw new MeteredBillingException(string.Format("Unable to find the request {0}", url), SaasApiErrorCode.NotFound, meteredBillingErrorResult);
                }
                else if (webResponse.StatusCode == HttpStatusCode.Conflict)
                {
                    this.logger?.Warn("Returning the error as " + JsonSerializer.Serialize(new { Error = "Conflict" }));
                    throw new MeteredBillingException(string.Format("Conflict came for {0}", url), SaasApiErrorCode.Conflict, meteredBillingErrorResult);
                }
                else if (webResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    this.logger?.Warn("Returning the error as " + JsonSerializer.Serialize(new { Error = "Bad Request" }));
                    throw new MeteredBillingException(string.Format("Unable to process the request {0}, server responding as BadRequest. Please verify the post data. ", url), SaasApiErrorCode.BadRequest, meteredBillingErrorResult);
                }
            }

            return null;
        }
    }
}
