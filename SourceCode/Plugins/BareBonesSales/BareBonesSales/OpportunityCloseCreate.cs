using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BareBonesSales
{
    /// <summary>
    /// Restricts c   
    /// </summary>
    public class OpportunityCloseCreate : IPlugin
    {

        public void Execute(IServiceProvider serviceProvider)
        {

            var result = BusinessLogic(serviceProvider);
            if (!result)
            {

                throw new InvalidPluginExecutionException(OperationStatus.Canceled, 20,
                    "Error creating case record.");
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


            // ensure we have something to look at

            // var guid = (Guid)context.OutputParameters["id"];
            //#region License Validation 
            //OrganizationRequest Req = new OrganizationRequest("hdn_ValidateLicense");
            //Req["ProductName"] = "BareBonesSalesCore"; //pass in the Solution Name
            //OrganizationResponse Response = service.Execute(Req);
            //if (Response.Results.ContainsKey("IsValid") && Response.Results["IsValid"].ToString() != "True")
            //{
            //    throw new InvalidPluginExecutionException("The license for BareBones Sales Core is invalid");
            //}
            //#endregion

            if (context.PrimaryEntityName != "hdn_opportunityclose")
            {
                tracingService.Trace("primary entity isn't Opportunity Close");
                return true;
            }

            tracingService.Trace(context.PrimaryEntityId.ToString());
            var targetEntity = service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, new ColumnSet("hdn_opportunity", "hdn_closurereason", "hdn_closedate"));
            // resolved cases can only be closed by creating a resolved case record. This sets the status and adds a link to the resolution record. 

            if (!targetEntity.Contains("hdn_closurereason"))
            {
                throw new InvalidPluginExecutionException(OperationStatus.Failed, 10, "Opportunity cannot be closed");
            }
            if (!targetEntity.Contains("hdn_opportunity"))
            {
                throw new InvalidPluginExecutionException(OperationStatus.Failed, 10, "Opportunity not specified be closed");
            }

            var oppId = targetEntity.GetAttributeValue<EntityReference>("hdn_opportunity");
            Entity closedOpp = new Entity(oppId.LogicalName, oppId.Id);
            closedOpp.Attributes.Add("statecode", new OptionSetValue(1));
            var statuscode = targetEntity.GetAttributeValue<OptionSetValue>("hdn_closurereason").Value;
            tracingService.Trace(statuscode.ToString());
            closedOpp.Attributes.Add("statuscode", targetEntity.GetAttributeValue<OptionSetValue>("hdn_closurereason"));
            closedOpp.Attributes.Add("hdn_opportunityclose", new EntityReference(context.PrimaryEntityName, context.PrimaryEntityId));
            if (targetEntity.Contains("hdn_closedate"))
            {
                closedOpp.Attributes.Add("hdn_actualclosedate", targetEntity.GetAttributeValue<DateTime>("hdn_closedate"));
            }
            service.Update(closedOpp);
            return true;
        }
    }
}
