using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class MissingRoleCheckRule : BaseRule
    {
        public MissingRoleCheckRule() : base("MissingRoleCheckRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.ExternallyVisible;
            }
        }

        public override ProblemCollection Check(TypeNode type)
        {
            if (type.IsController()) //only applies to controllers
            {
                while (type != null)
                {
                    if (type.Attributes.Any(a => a.Type.FullName == "CDS.Web.Authorization.AccessDeniedAuthorizeAttribute"))
                    {
                        return Problems; //it's got a capability check, no worries bra
                    }
                    type = type.BaseType;
                }

                Problems.Add(new Problem(this.GetResolution()));
            }
            return Problems;
        }
    }
}
