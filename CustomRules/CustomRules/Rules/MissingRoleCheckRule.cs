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
                    //MOD: we use a custom attribute here (mostly just to display a friendly error). You may also do something like that.
                    if (type.Attributes.Any(a => a.Type.FullName == "System.Web.Mvc.AuthorizeAttribute"))
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
