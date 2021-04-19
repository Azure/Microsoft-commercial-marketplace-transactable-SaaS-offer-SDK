namespace Microsoft.Marketplace.SaaS.SDK.Services.StatusHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using Microsoft.Extensions.Logging;
    using Microsoft.Marketplace.SaaS.SDK.Services.Contracts;
    using Microsoft.Marketplace.SaaS.SDK.Services.Helpers;
    using Microsoft.Marketplace.SaaS.SDK.Services.Models;
    using Microsoft.Marketplace.SaaS.SDK.Services.Services;
    using Microsoft.Marketplace.SaasKit.Client.DataAccess.Contracts;
    using Microsoft.Marketplace.SaasKit.Contracts;

    /// <summary>
    /// Status handler to send out notifications based on the last status of the subscription.
    /// </summary>
    /// <seealso cref="Microsoft.Marketplace.SaasKit.Provisioning.Webjob.StatusHandlers.AbstractSubscriptionStatusHandler" />
    public class NotificationStatusHandler : AbstractSubscriptionStatusHandler
    {
        /// <summary>
        /// The fulfillment API client.
        /// </summary>
        private readonly IFulfillmentApiClient fulfillmentApiClient;

        /// <summary>
        /// The subscription repository.
        /// </summary>
        private readonly ISubscriptionsRepository subscriptionRepository;

        /// <summary>
        /// The application configuration repository.
        /// </summary>
        private readonly IApplicationConfigRepository applicationConfigRepository;

        /// <summary>
        /// The email template repository.
        /// </summary>
        private readonly IEmailTemplateRepository emailTemplateRepository;

        /// <summary>
        /// The plan events mapping repository.
        /// </summary>
        private readonly IPlanEventsMappingRepository planEventsMappingRepository;

        /// <summary>
        /// The offer attributes repository.
        /// </summary>
        private readonly IOfferAttributesRepository offerAttributesRepository;

        /// <summary>
        /// The plan repository.
        /// </summary>
        private readonly IPlansRepository planRepository;

        /// <summary>
        /// The offers repository.
        /// </summary>
        private readonly IOffersRepository offersRepository;

        /// <summary>
        /// The events repository.
        /// </summary>
        private readonly IEventsRepository eventsRepository;

        /// <summary>
        /// The email service.
        /// </summary>
        private readonly IEmailService emailService;

        /// <summary>
        /// The email helper.
        /// </summary>
        private readonly EmailHelper emailHelper;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<NotificationStatusHandler> logger;

        /// <summary>
        /// The subscription service.
        /// </summary>
        private SubscriptionService subscriptionService = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationStatusHandler"/> class.
        /// </summary>
        /// <param name="fulfillApiClient">The fulfill API client.</param>
        /// <param name="planRepository">The plan repository.</param>
        /// <param name="applicationConfigRepository">The application configuration repository.</param>
        /// <param name="emailTemplateRepository">The email template repository.</param>
        /// <param name="planEventsMappingRepository">The plan events mapping repository.</param>
        /// <param name="offerAttributesRepository">The offer attributes repository.</param>
        /// <param name="eventsRepository">The events repository.</param>
        /// <param name="subscriptionRepository">The subscription repository.</param>
        /// <param name="usersRepository">The users repository.</param>
        /// <param name="offersRepository">The offers repository.</param>
        /// <param name="subscriptionTemplateParametersRepository">The subscription template parameters repository.</param>
        /// <param name="emailService">The email service.</param>
        /// <param name="emailHelper">The email helper.</param>
        /// <param name="logger">The logger.</param>
        public NotificationStatusHandler(
                                            IFulfillmentApiClient fulfillApiClient,
                                            IPlansRepository planRepository,
                                            IApplicationConfigRepository applicationConfigRepository,
                                            IEmailTemplateRepository emailTemplateRepository,
                                            IPlanEventsMappingRepository planEventsMappingRepository,
                                            IOfferAttributesRepository offerAttributesRepository,
                                            IEventsRepository eventsRepository,
                                            ISubscriptionsRepository subscriptionRepository,
                                            IUsersRepository usersRepository,
                                            IOffersRepository offersRepository,
                                            IEmailService emailService,
                                            ILogger<NotificationStatusHandler> logger)
                                            : base(subscriptionRepository, planRepository, usersRepository)
        {
            this.fulfillmentApiClient = fulfillApiClient;
            this.applicationConfigRepository = applicationConfigRepository;
            this.planEventsMappingRepository = planEventsMappingRepository;
            this.offerAttributesRepository = offerAttributesRepository;
            this.eventsRepository = eventsRepository;
            this.emailTemplateRepository = emailTemplateRepository;
            this.subscriptionRepository = subscriptionRepository;
            this.planRepository = planRepository;
            this.subscriptionService = new SubscriptionService(this.subscriptionRepository, this.planRepository);
            this.offersRepository = offersRepository;
            this.emailService = emailService;
            this.emailHelper = new EmailHelper(applicationConfigRepository, subscriptionRepository, emailTemplateRepository, planEventsMappingRepository, eventsRepository);
            this.logger = logger;
        }

        /// <summary>
        /// Processes the specified subscription identifier.
        /// </summary>
        /// <param name="subscriptionID">The subscription identifier.</param>
        public override void Process(Guid subscriptionID)
        {
            this.logger?.LogInformation("ResourceDeploymentStatusHandler Process...");
            this.logger?.LogInformation("Get SubscriptionById");
            var subscription = this.GetSubscriptionById(subscriptionID);
            this.logger?.LogInformation("Get PlanById");
            var planDetails = this.GetPlanById(subscription.AmpplanId);
            this.logger?.LogInformation("Get User");
            var userDetails = this.GetUserById(subscription.UserId);

            string planEventName = "Activate";

            if (
             subscription.SubscriptionStatus == SubscriptionStatusEnumExtension.Unsubscribed.ToString() ||
                subscription.SubscriptionStatus == SubscriptionStatusEnumExtension.UnsubscribeFailed.ToString())
            {
                planEventName = "Unsubscribe";
            }

            string processStatus = "success";
            if (
                subscription.SubscriptionStatus == SubscriptionStatusEnumExtension.ActivationFailed.ToString() ||
                subscription.SubscriptionStatus == SubscriptionStatusEnumExtension.UnsubscribeFailed.ToString())
            {
                processStatus = "failure";
            }

            int? eventId = this.eventsRepository.GetByName(planEventName)?.EventsId;
            var planEvents = this.planEventsMappingRepository.GetPlanEvent(planDetails.PlanGuid, eventId.GetValueOrDefault());
            bool isEmailEnabledForUnsubscription = Convert.ToBoolean(this.applicationConfigRepository.GetValueByName("IsEmailEnabledForUnsubscription"));
            bool isEmailEnabledForPendingActivation = Convert.ToBoolean(this.applicationConfigRepository.GetValueByName("IsEmailEnabledForPendingActivation"));
            bool isEmailEnabledForSubscriptionActivation = Convert.ToBoolean(this.applicationConfigRepository.GetValueByName("IsEmailEnabledForSubscriptionActivation"));

            bool triggerEmail = false;
            if (planEvents != null && planEvents.Isactive == true)
            {
                if (planEventName == "Activate" && isEmailEnabledForPendingActivation && subscription.SubscriptionStatus == SubscriptionStatusEnumExtension.PendingActivation.ToString())
                {
                    triggerEmail = true;
                }

                if (planEventName == "Activate" && isEmailEnabledForSubscriptionActivation && subscription.SubscriptionStatus != SubscriptionStatusEnumExtension.PendingActivation.ToString())
                {
                    triggerEmail = true;
                }

                if (planEventName == "Unsubscribe" && isEmailEnabledForUnsubscription)
                {
                    triggerEmail = true;
                }
            }

            if (triggerEmail)
            {
                var emailContent = this.emailHelper.PrepareEmailContent(subscriptionID, planDetails.PlanGuid, processStatus, planEventName, subscription.SubscriptionStatus);

                if (!string.IsNullOrWhiteSpace(emailContent.ToEmails) || !string.IsNullOrWhiteSpace(emailContent.BCCEmails))
                {
                    this.emailService.SendEmail(emailContent);
                }

                if (emailContent.CopyToCustomer && !string.IsNullOrEmpty(userDetails.EmailAddress))
                {
                    emailContent.ToEmails = userDetails.EmailAddress;

                    if (!string.IsNullOrWhiteSpace(emailContent.ToEmails) || !string.IsNullOrWhiteSpace(emailContent.BCCEmails))
                    {
                        this.emailService.SendEmail(emailContent);
                    }
                }
            }
        }
    }
}