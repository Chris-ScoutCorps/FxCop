using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class XSSRule : BaseRule
    {
        public XSSRule() : base("XSSRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.ExternallyVisible;
            }
        }

        public override ProblemCollection Check(Member member)
        {
            var method = member as Method;
            if (method != null)
            {
                var type = method.ReturnType;
                if (method.ReturnType.IsDerivedFrom("System.Web.IHtmlString"))
                {
                    Problems.Add(new Problem(this.GetResolution()));
                }
            }
            return Problems;
        }
    }
}
