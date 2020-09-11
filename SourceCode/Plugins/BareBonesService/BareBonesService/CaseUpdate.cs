using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BareBonesService
{
    /// <summary>
    /// Restricts c   
    /// </summary>
    public class CaseUpdate : IPlugin
    {

        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                var result = BusinessLogic(serviceProvider);
                if (!result)
                {

                    throw new InvalidPluginExecutionException(OperationStatus.Canceled, 20,
                        "Error updating case record.");
                }
            }
            catch (InvalidPluginExecutionException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error within Business logic.", ex);
            }
        }

        private static bool BusinessLogic(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.InputParameters != null)
            {

            }
            else
            {
                tracingService.Trace("No input parameters exist");
                return true;
            }
            

            var targetEntity = (Entity)context.InputParameters["Target"];
            var preImageAccount = (Entity)context.PreEntityImages["PreImage"];
            #region quick checks to stop the plugin if it doesn't need to run.
            if (!preImageAccount.Contains("createdon"))
            {
                tracingService.Trace("This plugin has lost it's preImage. Cannot continue");
                return true;
            }
            if (context.PrimaryEntityName != "hdn_case")
            {
                tracingService.Trace("primary entity isn't case");
                return true;
            }
            if (targetEntity.Contains("hdn_casetype") && targetEntity.Contains("hdn_priority") && context.Depth > 1)
            {
                tracingService.Trace("This record has been updated by the create case plugin - we can stop now");
                return true;
            }
            #endregion

            //#region License Validation 
            //OrganizationRequest Req = new OrganizationRequest("hdn_ValidateLicense");
            //Req["ProductName"] = "BareBonesServiceCore"; //pass in the Solution Name
            //OrganizationResponse Response = service.Execute(Req);
            //if (Response.Results.ContainsKey("IsValid") && Response.Results["IsValid"].ToString() != "True")
            //{
            //    throw new InvalidPluginExecutionException("The license for BareBones Service Core is invalid");
            //}
            //#endregion

            // resolved cases can only be closed by creating a resolved case record. This sets the status and adds a link to the resolution record. 

            #region statuscode changes
            if (targetEntity.Contains("statuscode"))
            {
                var status = targetEntity.GetAttributeValue<OptionSetValue>("statuscode").Value;
                if (status == 752240000 || status == 752240001)
                {
                    if (!targetEntity.Contains("hdn_caseresolution"))
                    {
                        throw new InvalidPluginExecutionException(OperationStatus.Canceled, 20, "Case cannot be closed as resolved without Case Resolution being completed");
                    }
                    return true;
                }
               
            }
            #endregion

            #region final catch all before we start running actual logic
            // check if the record contains a change we need to use.
            if (!targetEntity.Contains("hdn_priority") && !targetEntity.Contains("hdn_parentcasetype") && !targetEntity.Contains("hdn_childcasetype"))
            {
                tracingService.Trace("No case type or priority change so no more to do anything.");
                return true;
            }
            #endregion

            var priorityValue = -1;
            if (targetEntity.Contains("hdn_priority"))
            {
                priorityValue = targetEntity.GetAttributeValue<OptionSetValue>("hdn_priority").Value;
            }

            #region Get Case Type
            EntityReference caseTypeId = null;
           
            if (targetEntity.Contains("hdn_parentcasetype"))
            {

                if (targetEntity.GetAttributeValue<EntityReference>("hdn_parentcasetype") != null)
                {

                    caseTypeId = targetEntity.GetAttributeValue<EntityReference>("hdn_parentcasetype");
                }
            }
            if (targetEntity.Contains("hdn_childcasetype"))
            {
                if (targetEntity.GetAttributeValue<EntityReference>("hdn_childcasetype") != null)
                {
                    caseTypeId = targetEntity.GetAttributeValue<EntityReference>("hdn_childcasetype");
                }
                else
                {
                    // child has been set to null. If the parent hasn't changed we need to use the preimage record to get the previous case type and use that.
                    if (!targetEntity.Contains("hdn_parentcasetype") && preImageAccount.Contains("hdn_parentcasetype"))
                    {
                        caseTypeId = preImageAccount.GetAttributeValue<EntityReference>("hdn_parentcasetype");
                    }
                }
            }
            if (targetEntity.Contains("hdn_parentcasetype") || targetEntity.Contains("hdn_childcasetype"))
            {        
                targetEntity.Attributes["hdn_casetype"] = caseTypeId;
            }
            #endregion

//Setting Customer information for ownership
            if (targetEntity.Contains("hdn_customer"))
            {
                var owner = AutoSetOwner.GetCustomerOwner(service, tracingService, targetEntity.GetAttributeValue<EntityReference>("hdn_customer"));
                if (owner != null)
                {
                    targetEntity.Attributes["ownerid"] = owner;

                }
            }


            if (caseTypeId != null)
            {
                var caseType = service.Retrieve(caseTypeId.LogicalName, caseTypeId.Id, new ColumnSet("hdn_priority", "hdn_responsetime", "hdn_reassign", "ownerid"));
                if (caseType.Contains("hdn_reassign") && caseType.GetAttributeValue<bool>("hdn_reassign"))
                {
                    EntityReference typeOwner = caseType.GetAttributeValue<EntityReference>("ownerid");
                    targetEntity.Attributes["ownerid"] = typeOwner;
                }
               
                if (caseType.Contains("hdn_priority"))
                {
                   
                    var priorityId = caseType.GetAttributeValue<OptionSetValue>("hdn_priority");
                    targetEntity.Attributes["hdn_priority"]= priorityId;
                    priorityValue = priorityId.Value;
                }
            }
            if (priorityValue>-1)
            {
                DateTime dt = preImageAccount.GetAttributeValue<DateTime>("createdon");
                var date = DueDate.SetNewDeadline(service, tracingService, priorityValue, dt, null,false);
                if (date != null)
                {
                    targetEntity.Attributes["hdn_duedate"]=date;
                }
            }
            context.InputParameters["Target"] = targetEntity;
            return true;

        }
    }
}
