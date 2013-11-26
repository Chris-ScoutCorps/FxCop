using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class SkipAuthorizeRule : BaseRule
    {
        public SkipAuthorizeRule() : base("SkipAuthorizeRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.ExternallyVisible;
            }
        }

        public override ProblemCollection Check(TypeNode type)
        {
            if (type.IsController() && type.Attributes.Any(a => a.Type.FullName == "CDS.Web.Authorization.SkipAuthorizeAttribute"))
            {
                Problems.Add(new Problem(this.GetResolution()));
            }
            return Problems;
        }

        public override ProblemCollection Check(Member member)
        {
            if (member.IsControllerAction() && member.Attributes.Any(a => a.Type.FullName == "CDS.Web.Authorization.SkipAuthorizeAttribute"))
            {
                Problems.Add(new Problem(this.GetResolution()));
            }
            return Problems;
        }
    }
}
