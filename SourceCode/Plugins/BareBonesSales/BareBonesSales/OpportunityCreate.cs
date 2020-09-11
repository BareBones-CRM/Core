using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
namespace BareBonesSales
{
    public class OpportunityCreate : IPlugin
    {

        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                var result = BusinessLogic(serviceProvider);
                if (!result)
                {
                    throw new InvalidPluginExecutionException(OperationStatus.Canceled, 20,
                        "Error creating Opportunity record.");
                }
            }
            catch (InvalidPluginExecutionException ex){
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error creating Opportunity record.", ex);
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

            if (context.PrimaryEntityName != "hdn_opportunity")
            {
                tracingService.Trace("primary entity isn't opportunity");
                return true;
            }

            var targetEntity = service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, new ColumnSet("hdn_customer", "ownerid"));
            
            if (targetEntity.Contains("hdn_customer"))
            {
                tracingService.Trace("finding customer");
                // we need to call the appropriate logic
                AutoSetOwner.SetCustomerOwner (service, tracingService, new EntityReference(context.PrimaryEntityName,context.PrimaryEntityId), targetEntity.GetAttributeValue<EntityReference>("hdn_customer"));
                
            }

            return true;
        }

    }
    }
