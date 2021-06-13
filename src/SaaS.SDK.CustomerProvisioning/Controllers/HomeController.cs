namespace Microsoft.Marketplace.SaasKit.Client.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Diagnostics;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Marketplace.SaaS.SDK.Services.Contracts;
    using Microsoft.Marketplace.SaaS.SDK.Services.Models;
    using Microsoft.Marketplace.SaaS.SDK.Services.Services;
    using Microsoft.Marketplace.SaaS.SDK.Services.StatusHandlers;
    using Microsoft.Marketplace.SaasKit.Client.DataAccess.Contracts;
    using Microsoft.Marketplace.SaasKit.Client.DataAccess.Entities;
    using Microsoft.Marketplace.SaasKit.Contracts;
    using Microsoft.Marketplace.SaasKit.Models;
    using SaasKitModels = Microsoft.Marketplace.SaasKit.Models;

    /// <summary>Home Controller.</summary>
    /// <seealso cref="Microsoft.Marketplace.SaasKit.Web.Controllers.BaseController"/>
    public class HomeController : BaseController
    {
        /// <summary>
        /// Defines the  API Client.
        /// </summary>
        private readonly IFulfillmentApiClient apiClient;

        /// <summary>
        /// The subscription repository..
        /// </summary>
        private readonly ISubscriptionsRepository subscriptionRepository;

        /// <summary>
        /// The subscription logs repository.
        /// </summary>
        private readonly ISubscriptionLogRepository subscriptionLogRepository;

        /// <summary>
        /// The application log repository..
        /// </summary>
        private readonly IApplicationLogRepository applicationLogRepository;

        /// <summary>
        /// The plan repository.
        /// </summary>
        private readonly IPlansRepository planRepository;

        /// <summary>
        /// The plan repository.
        /// </summary>
        private readonly IOffersRepository offersRepository;

        /// <summary>
        /// The user repository.
        /// </summary>
        private readonly IUsersRepository userRepository;

        private readonly ILogger<HomeController> logger;

        private readonly IApplicationConfigRepository applicationConfigRepository;

        private readonly IEmailTemplateRepository emailTemplateRepository;

        private readonly IPlanEventsMappingRepository planEventsMappingRepository;

        private readonly IOfferAttributesRepository offerAttributesRepository;

        private readonly IEventsRepository eventsRepository;

        private readonly IEmailService emailService;

        private readonly ISubscriptionStatusHandler pendingFulfillmentStatusHandlers;

        private readonly ISubscriptionStatusHandler pendingActivationStatusHandlers;

        private readonly ISubscriptionStatusHandler unsubscribeStatusHandlers;

        private readonly ISubscriptionStatusHandler notificationStatusHandlers;

        private readonly ILoggerFactory loggerFactory;

        private SubscriptionService subscriptionService = null;

        private ApplicationLogService applicationLogService = null;

        private PlanService planService = null;

        /// <summary>
        /// The user service.
        /// </summary>
        private UserService userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="apiClient">The API Client<see cref="IFulfilmentApiClient" />.</param>
        /// <param name="subscriptionRepo">The subscription repository.</param>
        /// <param name="planRepository">The plan repository.</param>
        /// <param name="userRepository">The user repository.</param>
        /// <param name="applicationLogRepository">The application log repository.</param>
        /// <param name="subscriptionLogsRepo">The subscription logs repository.</param>
        /// <param name="applicationConfigRepository">The application configuration repository.</param>
        /// <param name="emailTemplateRepository">The email template repository.</param>
        /// <param name="offersRepository">The offers repository.</param>
        /// <param name="planEventsMappingRepository">The plan events mapping repository.</param>
        /// <param name="offerAttributesRepository">The offer attributes repository.</param>
        /// <param name="eventsRepository">The events repository.</param>
        /// <param name="cloudConfigs">The cloud configs.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="emailService">The email service.</param>
        public HomeController(ILogger<HomeController> logger, IFulfillmentApiClient apiClient, ISubscriptionsRepository subscriptionRepo, IPlansRepository planRepository, IUsersRepository userRepository, IApplicationLogRepository applicationLogRepository, ISubscriptionLogRepository subscriptionLogsRepo, IApplicationConfigRepository applicationConfigRepository, IEmailTemplateRepository emailTemplateRepository, IOffersRepository offersRepository, IPlanEventsMappingRepository planEventsMappingRepository, IOfferAttributesRepository offerAttributesRepository, IEventsRepository eventsRepository, ILoggerFactory loggerFactory, IEmailService emailService)
        {
            this.apiClient = apiClient;
            this.subscriptionRepository = subscriptionRepo;
            this.subscriptionLogRepository = subscriptionLogsRepo;
            this.applicationLogRepository = applicationLogRepository;
            this.planRepository = planRepository;
            this.userRepository = userRepository;
            this.userService = new UserService(this.userRepository);
            this.subscriptionService = new SubscriptionService(this.subscriptionRepository, this.planRepository);
            this.applicationLogService = new ApplicationLogService(this.applicationLogRepository);
            this.applicationConfigRepository = applicationConfigRepository;
            this.emailTemplateRepository = emailTemplateRepository;
            this.planEventsMappingRepository = planEventsMappingRepository;
            this.offerAttributesRepository = offerAttributesRepository;
            this.logger = logger;
            this.offersRepository = offersRepository;
            this.planService = new PlanService(this.planRepository, this.offerAttributesRepository, this.offersRepository);
            this.eventsRepository = eventsRepository;
            this.emailService = emailService;
            this.loggerFactory = loggerFactory;

            this.pendingActivationStatusHandlers = new PendingActivationStatusHandler(
                                                                          apiClient,
                                                                          subscriptionRepo,
                                                                          subscriptionLogsRepo,
                                                                          planRepository,
                                                                          userRepository,
                                                                          loggerFactory.CreateLogger<PendingActivationStatusHandler>());

            this.pendingFulfillmentStatusHandlers = new PendingFulfillmentStatusHandler(
                                                                           apiClient,
                                                                           applicationConfigRepository,
                                                                           subscriptionRepo,
                                                                           subscriptionLogsRepo,
                                                                           planRepository,
                                                                           userRepository,
                                                                           this.loggerFactory.CreateLogger<PendingFulfillmentStatusHandler>());

            this.notificationStatusHandlers = new NotificationStatusHandler(
                                                                        apiClient,
                                                                        planRepository,
                                                                        applicationConfigRepository,
                                                                        emailTemplateRepository,
                                                                        planEventsMappingRepository,
                                                                        offerAttributesRepository,
                                                                        eventsRepository,
                                                                        subscriptionRepo,
                                                                        userRepository,
                                                                        offersRepository,
                                                                        emailService,
                                                                        this.loggerFactory.CreateLogger<NotificationStatusHandler>());

            this.unsubscribeStatusHandlers = new UnsubscribeStatusHandler(
                                                                        apiClient,
                                                                        subscriptionRepo,
                                                                        subscriptionLogsRepo,
                                                                        planRepository,
                                                                        userRepository,
                                                                        this.loggerFactory.CreateLogger<UnsubscribeStatusHandler>());
        }

        /// <summary>
        /// Get All Subscription List for Current Logged in User.
        /// </summary>
        /// <param name="token">The MS Token<see cref="string" />..</param>
        /// <returns>
        /// The <see cref="IActionResult" />.
        /// </returns>
        public IActionResult Index(string token = null)
        {
            try
            {
                this.logger.LogInformation($"Landing page with token {token}");
                SubscriptionResult subscriptionDetail = new SaasKitModels.SubscriptionResult();
                SubscriptionResultExtension subscriptionExtension = new SubscriptionResultExtension();

                if (this.User.Identity.IsAuthenticated)
                {
                    var userId = this.userService.AddUser(this.GetCurrentUserDetail());
                    var currentUserId = this.userService.GetUserIdFromEmailAddress(this.CurrentUserEmailAddress);
                    this.subscriptionService = new SubscriptionService(this.subscriptionRepository, this.planRepository, userId);
                    this.logger.LogInformation("User authenticated successfully");
                    if (!string.IsNullOrEmpty(token))
                    {
                        this.TempData["ShowWelcomeScreen"] = null;
                        token = token.Replace(' ', '+');
                        var newSubscription = this.apiClient.ResolveAsync(token).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (newSubscription != null && newSubscription.SubscriptionId != default)
                        {
                            var subscriptionPlanDetail = this.apiClient.GetAllPlansForSubscriptionAsync(newSubscription.SubscriptionId).ConfigureAwait(false).GetAwaiter().GetResult();
                            Offers offers = new Offers()
                            {
                                OfferId = newSubscription.OfferId,
                                OfferName = newSubscription.OfferId,
                                UserId = currentUserId,
                                CreateDate = DateTime.Now,
                                OfferGuid = Guid.NewGuid(),
                            };
                            Guid newOfferId = this.offersRepository.Add(offers);
                            List<PlanDetailResultExtension> planList = new List<PlanDetailResultExtension>();
                            var serializedPlans = JsonSerializer.Serialize(subscriptionPlanDetail);
                            planList = JsonSerializer.Deserialize<List<PlanDetailResultExtension>>(serializedPlans);
                            planList.ForEach(x =>
                            {
                                x.OfferId = newOfferId;
                                x.PlanGUID = Guid.NewGuid();
                            });
                            this.subscriptionService.AddPlanDetailsForSubscription(planList, newSubscription.Quantity > 0);
                            var currentPlan = this.planRepository.GetById(newSubscription.PlanId);
                            var subscriptionData = this.apiClient.GetSubscriptionByIdAsync(newSubscription.SubscriptionId).ConfigureAwait(false).GetAwaiter().GetResult();
                            var subscribeId = this.subscriptionService.AddOrUpdatePartnerSubscriptions(subscriptionData);
                            if (subscribeId > 0 && subscriptionData.SaasSubscriptionStatus == SubscriptionStatusEnum.PendingFulfillmentStart)
                            {
                                SubscriptionAuditLogs auditLog = new SubscriptionAuditLogs()
                                {
                                    Attribute = Convert.ToString(SubscriptionLogAttributes.Status),
                                    SubscriptionId = subscribeId,
                                    NewValue = SubscriptionStatusEnum.PendingFulfillmentStart.ToString(),
                                    OldValue = "None",
                                    CreateBy = currentUserId,
                                    CreateDate = DateTime.Now,
                                };
                                this.subscriptionLogRepository.Save(auditLog);
                            }

                            subscriptionExtension = this.subscriptionService.GetSubscriptionsBySubscriptionId(newSubscription.SubscriptionId, true);
                            subscriptionExtension.ShowWelcomeScreen = false;
                            subscriptionExtension.CustomerEmailAddress = this.CurrentUserEmailAddress;
                            subscriptionExtension.CustomerName = this.CurrentUserName;
                            subscriptionExtension.SubscriptionParameters = this.subscriptionService.GetSubscriptionsParametersById(newSubscription.SubscriptionId, currentPlan.PlanGuid);
                        }
                    }
                    else
                    {
                        this.TempData["ShowWelcomeScreen"] = "True";
                        subscriptionExtension.ShowWelcomeScreen = true;
                        return this.View(subscriptionExtension);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        return this.Challenge(
                            new AuthenticationProperties
                            {
                                RedirectUri = "/?token=" + token,
                            }, OpenIdConnectDefaults.AuthenticationScheme);
                    }
                    else
                    {
                        this.TempData["ShowWelcomeScreen"] = "True";
                        subscriptionExtension.ShowWelcomeScreen = true;
                        return this.View(subscriptionExtension);
                    }
                }

                return this.View(subscriptionExtension);
            }
            catch (Exception ex)
            {
                this.logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
                return this.View("Error", ex);
            }
        }

        /// <summary>
        /// Subscription this instance.
        /// </summary>
        /// <returns> Subscription instance.</returns>
        public IActionResult Subscriptions()
        {
            this.logger.LogInformation("Home Controller / Subscriptions ");
            try
            {
                if (this.User.Identity.IsAuthenticated)
                {
                    this.TempData["ShowWelcomeScreen"] = "True";
                    SubscriptionViewModel subscriptionDetail = new SubscriptionViewModel();
                    subscriptionDetail.Subscriptions = this.subscriptionService.GetPartnerSubscription(this.CurrentUserEmailAddress, default, true).ToList();
                    foreach (var subscription in subscriptionDetail.Subscriptions)
                    {
                        Plans planDetail = this.planRepository.GetById(subscription.PlanId);
                        subscriptionDetail.IsAutomaticProvisioningSupported = Convert.ToBoolean(this.applicationConfigRepository.GetValueByName("IsAutomaticProvisioningSupported"));
                        subscription.IsPerUserPlan = planDetail.IsPerUser.HasValue ? planDetail.IsPerUser.Value : false;
                    }

                    subscriptionDetail.SaaSAppUrl = this.apiClient.GetSaaSAppURL();

                    if (this.TempData["ErrorMsg"] != null)
                    {
                        subscriptionDetail.IsSuccess = false;
                        subscriptionDetail.ErrorMessage = Convert.ToString(this.TempData["ErrorMsg"]);
                    }

                    return this.View(subscriptionDetail);
                }
                else
                {
                    return this.RedirectToAction(nameof(this.Index));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
                return this.View("Error", ex);
            }
        }

        /// <summary>
        /// Get All Subscription List for Current Logged in User.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <returns>
        /// The <see cref="IActionResult" />.
        /// </returns>
        public IActionResult SubscriptionDetail(Guid subscriptionId)
        {
            this.logger.LogInformation("Home Controller / SubscriptionDetail subscriptionId:{0}", JsonSerializer.Serialize(subscriptionId));
            try
            {
                if (this.User.Identity.IsAuthenticated)
                {
                    var subscriptionDetail = this.subscriptionService.GetPartnerSubscription(this.CurrentUserEmailAddress, subscriptionId).FirstOrDefault();
                    subscriptionDetail.PlanList = this.subscriptionService.GetAllSubscriptionPlans();

                    return this.View(subscriptionDetail);
                }
                else
                {
                    return this.RedirectToAction(nameof(this.Index));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
                return this.View("Error", ex);
            }
        }

        /// <summary>
        /// Get Subscription Details for selected Subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <returns>
        /// The <see cref="IActionResult" />.
        /// </returns>
        public IActionResult SubscriptionQuantityDetail(Guid subscriptionId)
        {
            this.logger.LogInformation("Home Controller / SubscriptionQuantityDetail subscriptionId:{0}", JsonSerializer.Serialize(subscriptionId));
            try
            {
                if (this.User.Identity.IsAuthenticated)
                {
                    var subscriptionDetail = this.subscriptionService.GetPartnerSubscription(this.CurrentUserEmailAddress, subscriptionId).FirstOrDefault();
                    return this.View(subscriptionDetail);
                }
                else
                {
                    return this.RedirectToAction(nameof(this.Index));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
                return this.View("Error", ex);
            }
        }

        /// <summary>
        /// Subscriptions the log detail.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <returns> Subscription log detail.</returns>
        public IActionResult SubscriptionLogDetail(Guid subscriptionId)
        {
            this.logger.LogInformation("Home Controller / SubscriptionQuantityDetail subscriptionId:{0}", JsonSerializer.Serialize(subscriptionId));
            try
            {
                if (this.User.Identity.IsAuthenticated)
                {
                    List<SubscriptionAuditLogs> subscriptionAudit = new List<SubscriptionAuditLogs>();
                    subscriptionAudit = this.subscriptionLogRepository.GetSubscriptionBySubscriptionId(subscriptionId).ToList();
                    return this.View(subscriptionAudit);
                }
                else
                {
                    return this.RedirectToAction(nameof(this.Index));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
                return this.View("Error", ex);
            }
        }

        /// <summary>
        /// The Error.
        /// </summary>
        /// <returns>
        /// The <see cref="IActionResult" />.
        /// </returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionDetail = this.HttpContext.Features.Get<IExceptionHandlerFeature>();
            return this.View(exceptionDetail?.Error);
        }

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="status">The status.</param>
        /// <returns>
        /// Return View.
        /// </returns>
        public IActionResult ProcessMessage(string action, string status)
        {
            try
            {
                if (status.Equals("Activate"))
                {
                    return this.PartialView();
                }
                else
                {
                    return this.View();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogInformation("Home Controller / ActivatedMessage Exception: {0}", ex);
                return this.View("Error", ex);
            }
        }

        /// <summary>
        /// Subscriptions the details.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="planId">The plan identifier.</param>
        /// <param name="operation">The operation.</param>
        /// <returns> Subscription Detials.</returns>
        public IActionResult SubscriptionDetails(Guid subscriptionId, string planId, string operation)
        {
            this.logger.LogInformation("Home Controller / ActivateSubscription subscriptionId:{0} :: planId : {1} :: operation:{2}", JsonSerializer.Serialize(subscriptionId), JsonSerializer.Serialize(planId), JsonSerializer.Serialize(operation));
            try
            {
                SubscriptionResultExtension subscriptionDetail = new SubscriptionResultExtension();
                if (this.User.Identity.IsAuthenticated)
                {
                    var userId = this.userService.AddUser(this.GetCurrentUserDetail());
                    var currentUserId = this.userService.GetUserIdFromEmailAddress(this.CurrentUserEmailAddress);
                    this.subscriptionService = new SubscriptionService(this.subscriptionRepository, this.planRepository, userId);
                    this.TempData["ShowWelcomeScreen"] = false;

                    subscriptionDetail = this.subscriptionService.GetSubscriptionsBySubscriptionId(subscriptionId);
                    var planDetails = this.planRepository.GetById(subscriptionDetail.PlanId);
                    var subscriptionParmaeters = this.subscriptionService.GetSubscriptionsParametersById(subscriptionId, planDetails.PlanGuid);
                    var inputParanetrs = subscriptionParmaeters.Where(s => s.Type.ToLower() == "input");
                    if (inputParanetrs != null && inputParanetrs.ToList().Count() > 0)
                    {
                        subscriptionDetail.SubscriptionParameters = inputParanetrs.ToList();
                    }

                    subscriptionDetail.CustomerEmailAddress = this.CurrentUserEmailAddress;
                    subscriptionDetail.CustomerName = this.CurrentUserName;
                }

                return this.View("Index", subscriptionDetail);
            }
            catch (Exception ex)
            {
                this.logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
                return this.View("Error", ex);
            }
        }

        /// <summary>
        /// Subscriptions the operation.
        /// </summary>
        /// <param name="subscriptionResultExtension">The subscription result extension.</param>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="planId">The plan identifier.</param>
        /// <param name="operation">The operation.</param>
        /// <returns>
        /// Subscriptions operation.
        /// </returns>
        [HttpPost]
        public IActionResult SubscriptionOperation(SubscriptionResultExtension subscriptionResultExtension, Guid subscriptionId, string planId, string operation)
        {
            this.logger.LogInformation("Home Controller / SubscriptionOperation subscriptionId:{0} :: planId : {1} :: operation:{2}", JsonSerializer.Serialize(subscriptionId), JsonSerializer.Serialize(planId), JsonSerializer.Serialize(operation));
            if (this.User.Identity.IsAuthenticated)
            {
                try
                {
                    var userDetails = this.userRepository.GetPartnerDetailFromEmail(this.CurrentUserEmailAddress);
                    SubscriptionProcessQueueModel queueObject = new SubscriptionProcessQueueModel();

                    if (subscriptionId != default)
                    {
                        SubscriptionResultExtension subscriptionDetail = new SubscriptionResultExtension();
                        this.logger.LogInformation("GetPartnerSubscription");
                        var oldValue = this.subscriptionService.GetPartnerSubscription(this.CurrentUserEmailAddress, subscriptionId, true).FirstOrDefault();
                        Plans planDetail = this.planRepository.GetById(oldValue.PlanId);
                        this.logger.LogInformation("GetUserIdFromEmailAddress");
                        var currentUserId = this.userService.GetUserIdFromEmailAddress(this.CurrentUserEmailAddress);
                        if (operation == "Activate")
                        {
                            try
                            {
                                this.logger.LogInformation("Save Subscription Parameters:  {0}", JsonSerializer.Serialize(subscriptionResultExtension.SubscriptionParameters));
                                if (subscriptionResultExtension.SubscriptionParameters != null && subscriptionResultExtension.SubscriptionParameters.Count() > 0)
                                {
                                    var inputParms = subscriptionResultExtension.SubscriptionParameters.ToList().Where(s => s.Type.ToLower() == "input");
                                    if (inputParms != null)
                                    {
                                        var inputParmsList = inputParms.ToList();
                                        this.subscriptionService.AddSubscriptionParameters(inputParmsList, currentUserId);
                                    }
                                }

                                if (Convert.ToBoolean(this.applicationConfigRepository.GetValueByName("IsAutomaticProvisioningSupported")))
                                {
                                    this.logger.LogInformation("UpdateStateOfSubscription PendingActivation: SubscriptionId: {0} ", subscriptionId);
                                    if (oldValue.SubscriptionStatus.ToString() != SubscriptionStatusEnumExtension.PendingActivation.ToString())
                                    {
                                        this.subscriptionService.UpdateStateOfSubscription(subscriptionId, SubscriptionStatusEnumExtension.PendingActivation.ToString(), true);
                                        if (oldValue != null)
                                        {
                                            SubscriptionAuditLogs auditLog = new SubscriptionAuditLogs()
                                            {
                                                Attribute = Convert.ToString(SubscriptionLogAttributes.Status),
                                                SubscriptionId = oldValue.SubscribeId,
                                                NewValue = SubscriptionStatusEnumExtension.PendingActivation.ToString(),
                                                OldValue = oldValue.SubscriptionStatus.ToString(),
                                                CreateBy = currentUserId,
                                                CreateDate = DateTime.Now,
                                            };
                                            this.subscriptionLogRepository.Save(auditLog);
                                        }
                                    }

                                    this.pendingActivationStatusHandlers.Process(subscriptionId);
                                }
                                else
                                {
                                    this.pendingFulfillmentStatusHandlers.Process(subscriptionId);
                                }
                            }
                            catch (FulfillmentException fex)
                            {
                                this.logger.LogInformation(fex.Message);
                            }
                        }

                        if (operation == "Deactivate")
                        {
                            this.subscriptionService.UpdateStateOfSubscription(subscriptionId, SubscriptionStatusEnumExtension.PendingUnsubscribe.ToString(), true);
                            if (oldValue != null)
                            {
                                SubscriptionAuditLogs auditLog = new SubscriptionAuditLogs()
                                {
                                    Attribute = Convert.ToString(SubscriptionLogAttributes.Status),
                                    SubscriptionId = oldValue.SubscribeId,
                                    NewValue = SubscriptionStatusEnumExtension.PendingUnsubscribe.ToString(),
                                    OldValue = oldValue.SubscriptionStatus.ToString(),
                                    CreateBy = currentUserId,
                                    CreateDate = DateTime.Now,
                                };
                                this.subscriptionLogRepository.Save(auditLog);
                            }

                            this.unsubscribeStatusHandlers.Process(subscriptionId);
                        }
                    }

                    this.notificationStatusHandlers.Process(subscriptionId);

                    return this.RedirectToAction(nameof(this.ProcessMessage), new { action = operation, status = operation });
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
                    return this.View("Error", ex);
                }
            }
            else
            {
                return this.RedirectToAction(nameof(this.Index));
            }
        }

        /// <summary>
        /// Changes the subscription plan.
        /// </summary>
        /// <param name="subscriptionDetail">The subscription detail.</param>
        /// <returns>Changes subscription plan.</returns>
        [HttpPost]
        public async Task<IActionResult> ChangeSubscriptionPlan(SubscriptionResult subscriptionDetail)
        {
            this.logger.LogInformation("Home Controller / ChangeSubscriptionPlan  subscriptionDetail:{0}", JsonSerializer.Serialize(subscriptionDetail));
            if (this.User.Identity.IsAuthenticated)
            {
                try
                {
                    var subscriptionId = Guid.Empty;
                    var planId = string.Empty;

                    if (subscriptionDetail != null)
                    {
                        subscriptionId = subscriptionDetail.Id;
                        planId = subscriptionDetail.PlanId;
                    }

                    if (subscriptionId != default && !string.IsNullOrEmpty(planId))
                    {
                        try
                        {
                            var currentUserId = this.userService.GetUserIdFromEmailAddress(this.CurrentUserEmailAddress);

                            var jsonResult = await this.apiClient.ChangePlanForSubscriptionAsync(subscriptionId, planId).ConfigureAwait(false);

                            var changePlanOperationStatus = OperationStatusEnum.InProgress;
                            if (jsonResult != null && jsonResult.OperationId != default)
                            {
                                while (OperationStatusEnum.InProgress.Equals(changePlanOperationStatus) || OperationStatusEnum.NotStarted.Equals(changePlanOperationStatus))
                                {
                                    var changePlanOperationResult = await this.apiClient.GetOperationStatusResultAsync(subscriptionId, jsonResult.OperationId).ConfigureAwait(false);
                                    changePlanOperationStatus = changePlanOperationResult.Status;

                                    this.logger.LogInformation("Operation Status :  " + changePlanOperationStatus + " For SubscriptionId " + subscriptionId + "Model SubscriptionID): {0} :: planID:{1}", JsonSerializer.Serialize(subscriptionId), JsonSerializer.Serialize(planId));
                                    this.applicationLogService.AddApplicationLog("Operation Status :  " + changePlanOperationStatus + " For SubscriptionId " + subscriptionId);
                                }

                                var oldValue = this.subscriptionService.GetSubscriptionsBySubscriptionId(subscriptionId, true);

                                this.subscriptionService.UpdateSubscriptionPlan(subscriptionId, planId);
                                this.logger.LogInformation("Plan Successfully Changed.");
                                this.applicationLogService.AddApplicationLog("Plan Successfully Changed.");

                                if (oldValue != null)
                                {
                                    SubscriptionAuditLogs auditLog = new SubscriptionAuditLogs()
                                    {
                                        Attribute = Convert.ToString(SubscriptionLogAttributes.Plan),
                                        SubscriptionId = oldValue.SubscribeId,
                                        NewValue = planId,
                                        OldValue = oldValue.PlanId,
                                        CreateBy = currentUserId,
                                        CreateDate = DateTime.Now,
                                    };
                                    this.subscriptionLogRepository.Save(auditLog);
                                }
                            }
                        }
                        catch (FulfillmentException fex)
                        {
                            this.TempData["ErrorMsg"] = fex.Message;
                        }
                    }

                    return this.RedirectToAction(nameof(this.Subscriptions));
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
                    return this.View("Error", ex);
                }
            }

            return this.RedirectToAction(nameof(this.Index));
        }

        /// <summary>
        /// Changes the quantity plan.
        /// </summary>
        /// <param name="subscriptionDetail">The subscription detail.</param>
        /// <returns>Changes subscription quantity.</returns>
        [HttpPost]
        public async Task<IActionResult> ChangeSubscriptionQuantity(SubscriptionResult subscriptionDetail)
        {
            this.logger.LogInformation("Home Controller / ChangeSubscriptionPlan  subscriptionDetail:{0}", JsonSerializer.Serialize(subscriptionDetail));
            if (this.User.Identity.IsAuthenticated)
            {
                try
                {
                    if (subscriptionDetail != null && subscriptionDetail.Id != default && subscriptionDetail.Quantity > 0)
                    {
                        try
                        {
                            var subscriptionId = subscriptionDetail.Id;
                            var quantity = subscriptionDetail.Quantity;

                            var currentUserId = this.userService.GetUserIdFromEmailAddress(this.CurrentUserEmailAddress);

                            var jsonResult = await this.apiClient.ChangeQuantityForSubscriptionAsync(subscriptionId, quantity).ConfigureAwait(false);

                            var changeQuantityOperationStatus = OperationStatusEnum.InProgress;
                            if (jsonResult != null && jsonResult.OperationId != default)
                            {
                                while (OperationStatusEnum.InProgress.Equals(changeQuantityOperationStatus) || OperationStatusEnum.NotStarted.Equals(changeQuantityOperationStatus))
                                {
                                    var changeQuantityOperationResult = await this.apiClient.GetOperationStatusResultAsync(subscriptionId, jsonResult.OperationId).ConfigureAwait(false);
                                    changeQuantityOperationStatus = changeQuantityOperationResult.Status;

                                    this.logger.LogInformation("changeQuantity Operation Status :  " + changeQuantityOperationStatus + " For SubscriptionId " + subscriptionId + "Model SubscriptionID): {0} :: quantity:{1}", JsonSerializer.Serialize(subscriptionId), JsonSerializer.Serialize(quantity));
                                    this.applicationLogService.AddApplicationLog("Operation Status :  " + changeQuantityOperationStatus + " For SubscriptionId " + subscriptionId);
                                }

                                var oldValue = this.subscriptionService.GetSubscriptionsBySubscriptionId(subscriptionId, true);

                                this.subscriptionService.UpdateSubscriptionQuantity(subscriptionId, quantity);
                                this.logger.LogInformation("Quantity Successfully Changed.");
                                this.applicationLogService.AddApplicationLog("Quantity Successfully Changed.");

                                if (oldValue != null)
                                {
                                    SubscriptionAuditLogs auditLog = new SubscriptionAuditLogs()
                                    {
                                        Attribute = Convert.ToString(SubscriptionLogAttributes.Quantity),
                                        SubscriptionId = oldValue.SubscribeId,
                                        NewValue = quantity.ToString(),
                                        OldValue = oldValue.Quantity.ToString(),
                                        CreateBy = currentUserId,
                                        CreateDate = DateTime.Now,
                                    };
                                    this.subscriptionLogRepository.Save(auditLog);
                                }
                            }
                        }
                        catch (FulfillmentException fex)
                        {
                            this.TempData["ErrorMsg"] = fex.Message;
                            this.logger.LogError("Message:{0} :: {1}   ", fex.Message, fex.InnerException);
                        }
                    }

                    return this.RedirectToAction(nameof(this.Subscriptions));
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
                    return this.View("Error", ex);
                }
            }
            else
            {
                return this.RedirectToAction(nameof(this.Index));
            }
        }

        /// <summary>
        /// Views the subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="planId">The plan identifier.</param>
        /// <param name="operation">The operation.</param>
        /// <returns> Subscriptions View. </returns>
        public IActionResult ViewSubscription(Guid subscriptionId, string planId, string operation)
        {
            try
            {
                SubscriptionResultExtension subscriptionDetail = new SubscriptionResultExtension();

                if (this.User.Identity.IsAuthenticated)
                {
                    var userId = this.userService.AddUser(this.GetCurrentUserDetail());
                    var currentUserId = this.userService.GetUserIdFromEmailAddress(this.CurrentUserEmailAddress);
                    this.subscriptionService = new SubscriptionService(this.subscriptionRepository, this.planRepository, userId);
                    var planDetails = this.planRepository.GetById(planId);
                    this.TempData["ShowWelcomeScreen"] = false;
                    subscriptionDetail = this.subscriptionService.GetPartnerSubscription(this.CurrentUserEmailAddress, subscriptionId).FirstOrDefault();
                    subscriptionDetail.ShowWelcomeScreen = false;
                    subscriptionDetail.CustomerEmailAddress = this.CurrentUserEmailAddress;
                    subscriptionDetail.CustomerName = this.CurrentUserName;
                    subscriptionDetail.SubscriptionParameters = this.subscriptionService.GetSubscriptionsParametersById(subscriptionId, planDetails.PlanGuid);
                }

                return this.View("Index", subscriptionDetail);
            }
            catch (Exception ex)
            {
                this.logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
                return this.View("Error", ex);
            }
        }
    }
}
