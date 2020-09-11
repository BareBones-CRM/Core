using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections.Generic;

namespace BareBonesSales
{
    public static class AutoSetOwner
    {
        public static bool SetCustomerOwner(IOrganizationService service, ITracingService tracingService, EntityReference thisRecord, EntityReference customer)
        {
            var owner = GetCustomerOwner(service,tracingService, customer);
            if (owner != null)
            {     
                var thisEntity = new Entity(thisRecord.LogicalName, thisRecord.Id);
                thisEntity.Attributes.Add("ownerid", owner);
                service.Update(thisEntity);
                return true;
            }
            tracingService.Trace("No customer found");
            return false;
        }

        public static EntityReference GetCustomerOwner(IOrganizationService service,  ITracingService tracingService, EntityReference customer)
        {                                                                                         
            var customerEntity = service.Retrieve(customer.LogicalName, customer.Id, new ColumnSet("hdn_automateassigntoowner", "ownerid"));
            if (!customerEntity.Contains("ownerid"))
            {
                return null;
            }
            if (!customerEntity.Contains("hdn_automateassigntoowner"))
            {   
                return null;
            }
            if (!(bool)customerEntity.Attributes["hdn_automateassigntoowner"])
            {
                return null;
            }
            return customerEntity.GetAttributeValue<EntityReference>("ownerid");
        }

    }
}
