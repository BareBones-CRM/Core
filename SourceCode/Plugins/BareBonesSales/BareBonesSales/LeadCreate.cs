using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BareBonesSales
{
   public class LeadCreate : IPlugin
    {

        public void Execute(IServiceProvider serviceProvider)
        {

            var result = BusinessLogic(serviceProvider);
            if (!result)
            {
                throw new InvalidPluginExecutionException(OperationStatus.Canceled, 20,
                    "Error creating Lead record.");
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
            
            if (context.PrimaryEntityName != "hdn_lead")
            {
                tracingService.Trace("primary entity isn't Lead");
                return true;
            }
           

            //#region License Validation 
            //OrganizationRequest Req = new OrganizationRequest("hdn_ValidateLicense");
            //Req["ProductName"] = "BareBonesSalesCore"; //pass in the Solution Name
            //OrganizationResponse Response    = service.Execute(Req);
            //if (Response.Results.ContainsKey("IsValid") && Response.Results["IsValid"].ToString() != "True")
            //{
            //    throw new InvalidPluginExecutionException("The license for BareBones Service Core is invalid");
            //}
            //#endregion

         
            return true;
        }
   }
}
