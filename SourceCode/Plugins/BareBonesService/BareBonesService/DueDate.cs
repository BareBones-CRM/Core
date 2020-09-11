using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections.Generic;

namespace BareBonesService
{
    public static class DueDate
    {
        public static DateTime? SetNewDeadline(IOrganizationService service, ITracingService tracingService, int optionSetValue, DateTime created, DateTime? deadline, bool isNewRecord)
        {
            
            tracingService.Trace("getting deadline");

            QueryExpression query = new QueryExpression("hdn_prioritysetting");
            query.ColumnSet = new ColumnSet("hdn_priority", "hdn_expectedresponsetime");
            query.Criteria.AddFilter(LogicalOperator.And);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var settings = service.RetrieveMultiple(query);
            Int32 minutes = 0;
            foreach (var e in settings.Entities)
            {
                if (e.GetAttributeValue<OptionSetValue>("hdn_priority").Value == optionSetValue)
                {
                    if (e.Contains("hdn_expectedresponsetime"))
                    {
                        minutes = e.GetAttributeValue<Int32>("hdn_expectedresponsetime");
                    }
                }
            }
            if (minutes > 0)
            {


                var newDateTime = created.AddMinutes(minutes);
                return newDateTime;
            }
            return null;            
        }
    }
}
