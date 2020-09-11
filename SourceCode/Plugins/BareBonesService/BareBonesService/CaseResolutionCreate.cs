using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BareBonesService
{
    /// <summary>
    /// Restricts c   
    /// </summary>
    public class CaseResolutionCreate : IPlugin
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


            if (context.PrimaryEntityName != "hdn_caseresolution")
            {
                tracingService.Trace("primary entity isn't case");
                return true;
            }

            tracingService.Trace(context.PrimaryEntityId.ToString());
            var targetEntity = service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, new ColumnSet("hdn_case", "hdn_resolutiontype"));

            tracingService.Trace("here.");
            // resolved cases can only be closed by creating a resolved case record. This sets the status and adds a link to the resolution record. 

            if (!targetEntity.Contains("hdn_resolutiontype"))
            {
                throw new InvalidPluginExecutionException(OperationStatus.Failed, 10, "Case cannot be closed");
            }
            if (!targetEntity.Contains("hdn_case"))
            {
                throw new InvalidPluginExecutionException(OperationStatus.Failed, 10, "Case not specified be closed");
            }

            var caseId = targetEntity.GetAttributeValue<EntityReference>("hdn_case");
            Entity closedCase = new Entity(caseId.LogicalName, caseId.Id);
             closedCase.Attributes.Add("statecode", new OptionSetValue(1));
            var statuscode = targetEntity.GetAttributeValue<OptionSetValue>("hdn_resolutiontype").Value;
            tracingService.Trace(statuscode.ToString());
            closedCase.Attributes.Add("statuscode", targetEntity.GetAttributeValue<OptionSetValue>("hdn_resolutiontype"));
            closedCase.Attributes.Add("hdn_caseresolution", new EntityReference(context.PrimaryEntityName, context.PrimaryEntityId));
            service.Update(closedCase);
            return true;
        }
    }
}
