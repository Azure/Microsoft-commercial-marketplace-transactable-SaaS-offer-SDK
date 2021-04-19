namespace Microsoft.Marketplace.SaasKit.Client.DataAccess.Contracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Marketplace.SaasKit.Client.DataAccess.Entities;

    /// <summary>
    /// SubscriptionsRepository Interface.
    /// </summary>
    /// <seealso cref="Microsoft.Marketplace.SaasKit.Client.DataAccess.Contracts.IBaseRepository{Microsoft.Marketplace.SaasKit.Client.DataAccess.Entities.Subscriptions}" />
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="Microsoft.Marketplace.SaasKit.DataAccess.Contracts.IBaseRepository{Microsoft.Marketplace.SaasKit.DataAccess.Entities.Subscriptions}" />
    public interface ISubscriptionsRepository : IDisposable, IBaseRepository<Subscriptions>
    {
        /// <summary>
        /// Gets the subscriptions by email address.
        /// </summary>
        /// <param name="partnerEmailAddress">The partner email address.</param>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="isIncludeDeactivated">if set to <c>true</c> [is include deactivated].</param>
        /// <returns> Subscriptions.</returns>
        IEnumerable<Subscriptions> GetSubscriptionsByEmailAddress(string partnerEmailAddress, Guid subscriptionId, bool isIncludeDeactivated = false);

        /// <summary>
        /// Gets the subscriptions by schedule identifier.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="isIncludeDeactivated">if set to <c>true</c> [is include deactivated].</param>
        /// <returns> Subscriptions.</returns>
        Subscriptions GetById(Guid subscriptionId, bool isIncludeDeactivated = false);

        /// <summary>
        /// Updates the status for subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="subscriptionStatus">The subscription status.</param>
        /// <param name="isActive">if set to <c>true</c> [is active].</param>
        void UpdateStatusForSubscription(Guid subscriptionId, string subscriptionStatus, bool isActive);

        /// <summary>
        /// Updates the plan for subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="planId">The plan identifier.</param>
        void UpdatePlanForSubscription(Guid subscriptionId, string planId);

        /// <summary>
        /// Updates the quantity for subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="quantity">The quantity identifier.</param>
        void UpdateQuantityForSubscription(Guid subscriptionId, int quantity);

        /// <summary>
        /// Gets the subscriptions parameters by identifier.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="planId">The plan identifier.</param>
        /// <returns>List of parameters related to the subscription.</returns>
        List<SubscriptionParametersOutput> GetSubscriptionsParametersById(Guid subscriptionId, Guid planId);

        /// <summary>
        /// Adds the subscription parameters.
        /// </summary>
        /// <param name="subscriptionParametersOutput">The subscription parameters output.</param>
        void AddSubscriptionParameters(SubscriptionParametersOutput subscriptionParametersOutput);
    }
}
