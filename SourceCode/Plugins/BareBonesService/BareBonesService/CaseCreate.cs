using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BareBonesService
{
    /// <summary>
    /// Restricts c   
    /// </summary>
    public class CaseCreate : IPlugin
    {

        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                var result = BusinessLogic(serviceProvider);
                if (!result)
                {

                    throw new InvalidPluginExecutionException(OperationStatus.Canceled, 20,
                        "Error creating case record.");
                }
            }
            catch (InvalidPluginExecutionException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error within Case Create Plugin", ex);
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

            tracingService.Trace(context.OrganizationName);

            // Call LicensePower Validation code
            //#region License Validation 
            //OrganizationRequest Req = new OrganizationRequest("hdn_ValidateLicense");
            //Req["ProductName"]="BareBonesServiceCore"; //pass in the Solution Name
            //OrganizationResponse Response = service.Execute(Req);
            //if (Response.Results.ContainsKey("IsValid") && Response.Results["IsValid"].ToString() != "True")
            //{
            //    throw new InvalidPluginExecutionException("The license for BareBones Service Core is invalid");
            //}
            //#endregion


            if (context.PrimaryEntityName != "hdn_case")
            {
                tracingService.Trace("primary entity isn't case");
                return true;
            }
            // this runs after case creation as we cannot reassign the case before creating it.
            // so we need to pull information back.
            var targetEntity = service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, new ColumnSet("hdn_parentcasetype", "hdn_childcasetype", "hdn_customer"));
            

            EntityReference caseTypeId = null;
            if (targetEntity.Contains("hdn_customer"))
            {

            }
            if (targetEntity.Contains("hdn_parentcasetype"))
            {
                caseTypeId= targetEntity.GetAttributeValue<EntityReference>("hdnm_parentcasetype");
               
            }
            if (targetEntity.Contains("hdn_childcasetype"))
            {
                if (targetEntity.GetAttributeValue<EntityReference>("hdn_childcasetype") != null)
                {
                    caseTypeId = targetEntity.GetAttributeValue<EntityReference>("hdn_childcasetype");
                }
            }

            

            bool needToUpdate = false;
            var updateEntity = new Entity(context.PrimaryEntityName, context.PrimaryEntityId);
            int priorityValue = -1;


            #region Update Owner / prioity flag based on case type.
            if (caseTypeId != null)
            {
                needToUpdate = true;
                updateEntity.Attributes["hdn_casetype"] = caseTypeId;
                var caseType = service.Retrieve(caseTypeId.LogicalName, caseTypeId.Id, new ColumnSet("hdn_priority", "hdn_responsetime", "hdn_reassign", "ownerid"));       
                if (caseType.Contains("hdn_reassign") && caseType.GetAttributeValue<bool>("hdn_reassign"))
                {
                    EntityReference caseOwner = caseType.GetAttributeValue<EntityReference>("ownerid");
                    updateEntity.Attributes["ownerid"] = caseOwner;
                }
                else
                {
                    // 
                    if (targetEntity.Contains("hdn_customer"))
                    {
                        var customerOwner = AutoSetOwner.GetCustomerOwner(service, tracingService, targetEntity.GetAttributeValue<EntityReference>("hdn_customer"));
                        if (customerOwner != null)
                        {
                            updateEntity.Attributes["ownerid"] = customerOwner;
                        }
                    }
                }
                if (caseType.Contains("hdn_priority"))
                {
                    priorityValue = caseType.GetAttributeValue<OptionSetValue>("hdn_priority").Value;
                    updateEntity.Attributes["hdn_priority"]= caseType.GetAttributeValue<OptionSetValue>("hdn_priority");
                }
            }
            #endregion
            if (priorityValue>0)
            {
                var date = DueDate.SetNewDeadline(service, tracingService, priorityValue, DateTime.Now, null,false);
                if (date != null)
                {
                    updateEntity.Attributes.Add("hdn_duedate", date);
                    needToUpdate = true;
                }
            }
            if (needToUpdate)
            {
                service.Update(updateEntity);
            }
            return true;
        }
    }
}
