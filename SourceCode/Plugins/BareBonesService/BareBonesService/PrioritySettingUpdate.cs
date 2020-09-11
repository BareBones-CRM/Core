using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BareBonesService
{
    /// <summary>
    /// Restricts c   
    /// </summary>
    public class PriorityUpdate : IPlugin
    {

        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                var result = BusinessLogic(serviceProvider);
                if (!result)
                {

                    throw new InvalidPluginExecutionException(OperationStatus.Canceled, 20,
                        "A record for this priority already exists");
                }
            }
            catch (InvalidPluginExecutionException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error within Priority Setting Create Plugin", ex);
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


            if (context.PrimaryEntityName != "hdn_prioritysetting")
            {
                tracingService.Trace("primary entity isn't Priority Setting");
                return true;
            }
            var targetEntity = (Entity)context.InputParameters["Target"];
            if (targetEntity.Contains("hdn_priority"))
            {
                return false;
            }
            return true;
        }
    }
}
