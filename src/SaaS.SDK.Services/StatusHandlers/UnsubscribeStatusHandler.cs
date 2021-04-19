namespace Microsoft.Marketplace.SaaS.SDK.Services.StatusHandlers
{
    using System;
    using System.Text.Json;
    using Microsoft.Extensions.Logging;
    using Microsoft.Marketplace.SaaS.SDK.Services.Models;
    using Microsoft.Marketplace.SaasKit.Client.DataAccess.Contracts;
    using Microsoft.Marketplace.SaasKit.Client.DataAccess.Entities;
    using Microsoft.Marketplace.SaasKit.Contracts;

    /// <summary>
    /// Status handler to handle the unsubscription event.
    /// </summary>
    /// <seealso cref="Microsoft.Marketplace.SaasKit.Provisioning.Webjob.StatusHandlers.AbstractSubscriptionStatusHandler" />
    public class UnsubscribeStatusHandler : AbstractSubscriptionStatusHandler
    {
        /// <summary>
        /// The fulfillment apiclient.
        /// </summary>
        private readonly IFulfillmentApiClient fulfillmentApiClient;

        /// <summary>
        /// The subscription log repository.
        /// </summary>
        private readonly ISubscriptionLogRepository subscriptionLogRepository;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<UnsubscribeStatusHandler> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsubscribeStatusHandler" /> class.
        /// </summary>
        /// <param name="fulfillApiClient">The fulfill API client.</param>
        /// <param name="applicationConfigRepository">The application configuration repository.</param>
        /// <param name="subscriptionsRepository">The subscriptions repository.</param>
        /// <param name="subscriptionLogRepository">The subscription log repository.</param>
        /// <param name="plansRepository">The plans repository.</param>
        /// <param name="usersRepository">The users repository.</param>
        /// <param name="logger">The logger.</param>
        public UnsubscribeStatusHandler(
                                            IFulfillmentApiClient fulfillApiClient,
                                            ISubscriptionsRepository subscriptionsRepository,
                                            ISubscriptionLogRepository subscriptionLogRepository,
                                            IPlansRepository plansRepository,
                                            IUsersRepository usersRepository,
                                            ILogger<UnsubscribeStatusHandler> logger)
            : base(subscriptionsRepository, plansRepository, usersRepository)
        {
            this.fulfillmentApiClient = fulfillApiClient;
            this.subscriptionLogRepository = subscriptionLogRepository;
            this.logger = logger;
        }

        /// <summary>
        /// Processes the specified subscription identifier.
        /// </summary>
        /// <param name="subscriptionID">The subscription identifier.</param>
        public override void Process(Guid subscriptionID)
        {
            this.logger?.LogInformation("PendingActivationStatusHandler {0}", subscriptionID);
            var subscription = this.GetSubscriptionById(subscriptionID);
            this.logger?.LogInformation("Result subscription : {0}", JsonSerializer.Serialize(subscription.AmpplanId));

            this.logger?.LogInformation("Get User");
            var userDetails = this.GetUserById(subscription.UserId);
            string status = subscription.SubscriptionStatus;
            if (subscription.SubscriptionStatus == SubscriptionStatusEnumExtension.PendingUnsubscribe.ToString())
            {
                try
                {
                    var subscriptionData = this.fulfillmentApiClient.DeleteSubscriptionAsync(subscriptionID, subscription.AmpplanId).ConfigureAwait(false).GetAwaiter().GetResult();

                    this.subscriptionsRepository.UpdateStatusForSubscription(subscriptionID, SubscriptionStatusEnumExtension.Unsubscribed.ToString(), false);

                    SubscriptionAuditLogs auditLog = new SubscriptionAuditLogs()
                    {
                        Attribute = SubscriptionLogAttributes.Status.ToString(),
                        SubscriptionId = subscription.Id,
                        NewValue = SubscriptionStatusEnumExtension.Unsubscribed.ToString(),
                        OldValue = status,
                        CreateBy = userDetails.UserId,
                        CreateDate = DateTime.Now,
                    };
                    this.subscriptionLogRepository.Save(auditLog);

                    this.subscriptionLogRepository.LogStatusDuringProvisioning(subscriptionID, "Unsubscribe Failed", SubscriptionStatusEnumExtension.UnsubscribeFailed.ToString());
                }
                catch (Exception ex)
                {
                    string errorDescriptin = string.Format("Exception: {0} :: inner Exception:{1}", ex.Message, ex.InnerException);
                    this.subscriptionLogRepository.LogStatusDuringProvisioning(subscriptionID, errorDescriptin, SubscriptionStatusEnumExtension.UnsubscribeFailed.ToString());
                    this.logger?.LogInformation(errorDescriptin);

                    this.subscriptionsRepository.UpdateStatusForSubscription(subscriptionID, SubscriptionStatusEnumExtension.UnsubscribeFailed.ToString(), true);

                    SubscriptionAuditLogs auditLog = new SubscriptionAuditLogs()
                    {
                        Attribute = SubscriptionLogAttributes.Status.ToString(),
                        SubscriptionId = subscription.Id,
                        NewValue = SubscriptionStatusEnumExtension.UnsubscribeFailed.ToString(),
                        OldValue = subscription.SubscriptionStatus,
                        CreateBy = userDetails.UserId,
                        CreateDate = DateTime.Now,
                    };
                    this.subscriptionLogRepository.Save(auditLog);
                }
            }
        }
    }
}