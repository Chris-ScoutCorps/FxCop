using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class JSONAllowGetRule : BaseRule
    {
        public JSONAllowGetRule() : base("JSONAllowGetRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.ExternallyVisible;
            }
        }

        public override ProblemCollection Check(Member member)
        {
            VisitMethodCallStatements(member);
            return Problems;
        }

        public override void VisitMethodCall(MethodCall call)
        {
            if (call.Method() != null)
            {
                if (call.Method().Parameters.Any(a => a.Type.FullName == "System.Web.Mvc.JsonRequestBehavior"))
                {
                    this.Problems.Add(new Problem(this.GetResolution(), (Node)call));
                }
            }

            base.VisitMethodCall(call);
        }
    }
}
