using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;

namespace BareBonesSales
{
    public class QualifyLead : CodeActivity
    {
        [Input("Lead")]
        [RequiredArgument]
        [ReferenceTarget("hdn_lead")]
        public InArgument<EntityReference> hdnLead
        {
            get;
            set;
        }

        [Output("NewOpportunity")]
        [ReferenceTarget("hdn_opportunity")]
        public OutArgument<EntityReference> NewOpportunity
        {
            get;
            set;
        }

        protected override void Execute(CodeActivityContext executionContext)
        {
            try
            {
                EntityReference template = hdnLead.Get(executionContext);
                if (template.LogicalName != "hdn_lead")
                {

                    throw new InvalidPluginExecutionException(OperationStatus.Canceled, 20, "Workflow triggered against wrong entity.");
                }

                var result = BusinessLogic(executionContext, template);


                NewOpportunity.Set(executionContext, result);
            }
            catch (InvalidPluginExecutionException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Unable to Qualify Lead.", ex);
            }
        }

        private static EntityReference BusinessLogic(CodeActivityContext executionContext, EntityReference lead)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            var returnedReference = new EntityReference();

            try
            {
                //Create the context
                IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
                IOrganizationServiceFactory serviceFactory =
                    executionContext.GetExtension<IOrganizationServiceFactory>();
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                #region License Validation 
                OrganizationRequest Req = new OrganizationRequest("hdn_ValidateLicense");
                Req["ProductName"] = "BareBonesSalesCore"; //pass in the Solution Name
                OrganizationResponse Response = service.Execute(Req);
                if (Response.Results.ContainsKey("IsValid") && Response.Results["IsValid"].ToString() != "True")
                {
                    var errorText = "The license for Bare Bones Sales Core is invalid. ";
                    if (Response.Results.Contains("ErrorMessage")){
                        errorText = errorText + Response.Results["ErrorMessage"];
                    }
                    throw new InvalidPluginExecutionException(errorText);
                }
                #endregion


                // pull the input parameters
                var leadDetails = service.Retrieve(lead.LogicalName, lead.Id, new ColumnSet("hdn_email", "hdn_lastname", "hdn_website", "hdn_company", "hdn_topic", "hdn_description"));

                if (leadDetails != null)
                {
                    var contactId = new EntityReference("contact");
                    if (leadDetails.Contains("hdn_email"))
                    {
                        var query2 = new QueryExpression("contact");
                        query2.ColumnSet = new ColumnSet("emailaddress1");
                        query2.Criteria.AddFilter(LogicalOperator.And);
                        query2.Criteria.AddCondition("emailaddress1", ConditionOperator.Equal, leadDetails.GetAttributeValue<string>("hdn_email"));
                        var contacts = service.RetrieveMultiple(query2);

                        if (contacts.Entities.Count > 0)
                        {
                            contactId.Id = contacts.Entities[0].Id;
                        }

                    }
                    if (contactId.Id == new Guid())
                    {
                        tracingService.Trace("creating lead");
                        if (leadDetails.Contains("hdn_lastname"))
                        {
                            var initialize = new InitializeFromRequest();
                            initialize.TargetEntityName = "contact";
                            initialize.EntityMoniker = lead;
                            // Execute the request
                            var initialized =
                                 (InitializeFromResponse)service.Execute(initialize);

                            if (initialized.Entity != null &&
                                initialized.Entity.LogicalName == "contact") ;
                            var contact = initialized.Entity;
                            var contactGuid = service.Create(contact);
                            contactId.Id = contactGuid;
                        }

                    }
                    var accountId = new EntityReference("account");
                    if (leadDetails.Contains("hdn_website"))
                    {
                        var query3 = new QueryExpression("account");
                        query3.ColumnSet = new ColumnSet("websiteurl");
                        query3.Criteria.AddFilter(LogicalOperator.And);
                        query3.Criteria.AddCondition("websiteurl", ConditionOperator.Equal, leadDetails.GetAttributeValue<string>("hdn_website"));
                        var accounts = service.RetrieveMultiple(query3);
                        if (accounts.Entities.Count > 0)
                        {
                            accountId.Id = accounts.Entities[0].Id;
                        }
                    }
                    if (accountId.Id == Guid.Empty)
                    {
                        if (leadDetails.Contains("hdn_company"))
                        {
                            var initializeAccount = new InitializeFromRequest();
                            initializeAccount.TargetEntityName = "account";
                            initializeAccount.EntityMoniker = lead;

                            // Execute the request
                            var initializedAccount =
                                 (InitializeFromResponse)service.Execute(initializeAccount);

                            if (initializedAccount.Entity != null &&
                                initializedAccount.Entity.LogicalName == "account") ;
                            var account = initializedAccount.Entity;
                            if (contactId.Id != Guid.Empty)
                            {
                                account["primarycontactid"] = contactId;
                            }
                            var accountGuid = service.Create(account);
                            accountId.Id = accountGuid;
                        }
                    }
                    var initializeOpp = new InitializeFromRequest();
                    initializeOpp.TargetEntityName = "hdn_opportunity";
                    initializeOpp.EntityMoniker = lead;

                    // Execute the request
                    var initializedOpp =
                         (InitializeFromResponse)service.Execute(initializeOpp);

                    if (initializedOpp.Entity != null &&
                        initializedOpp.Entity.LogicalName == "hdn_opportunity")
                    {
                        var newEntity = initializedOpp.Entity;
                        if (accountId.Id != Guid.Empty)
                        {
                            newEntity.Attributes["hdn_customer"] = accountId;

                        }
                        else
                        {
                            if (contactId.Id != Guid.Empty)
                            {
                                newEntity.Attributes["hdn_customer"] = contactId;
                            }
                        }
                        if (contactId.Id != Guid.Empty)
                        {
                            newEntity.Attributes["hdn_contact"] = contactId;
                        }
                        //newEntity.Attributes["hdn_topic"] = leadDetails.GetAttributeValue<string>("hdn_topic");

                        var guid = service.Create(newEntity);

                        //finally with the opportunity created lets qualify the lead
                        var updateLead = new Entity(lead.LogicalName, lead.Id);

                        updateLead.Attributes.Add("statuscode", new OptionSetValue(752240000));
                        updateLead.Attributes.Add("statecode", new OptionSetValue(1));
                        service.Update(updateLead);
                        return new EntityReference("hdn_opportunity", guid);
                    }
                }
            }
            catch (InvalidPluginExecutionException ex)
            {
                throw;
            }
            catch (Exception ex)
            {

                tracingService.Trace(ex.Message);
                tracingService.Trace(ex.StackTrace);
                throw;
            }

            return returnedReference;
        }
    }
}