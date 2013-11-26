using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class ContextInDataDLLRule : BaseRule
    {
        public ContextInDataDLLRule() : base("ContextInDataDLLRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.All;
            }
        }

        public override ProblemCollection Check(Member member)
        {
            if (member.DeclaringType.DeclaringModule.ContainingAssembly.IsDataDll())
            {
                VisitMethodCallStatements(member);
            }
            return Problems;
        }

        public override void VisitMethodCall(MethodCall call)
        {
            if (call.Method() != null)
            {
                if (call.Method().IsStatic && call.Method().Name.Name.StartsWith("get_Current")
                    && call.Method().DeclaringType.FullName != "System.Globalization.CultureInfo")
                {
                    this.Problems.Add(new Problem(this.GetResolution(call.Method().FullName), (Node)call));
                }
            }

            base.VisitMethodCall(call);
        }
    }
}
