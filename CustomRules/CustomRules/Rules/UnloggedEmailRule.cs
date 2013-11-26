using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class UnloggedEmailRule : BaseRule
    {
        public UnloggedEmailRule() : base("UnloggedEmailRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.All;
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
                if ((call.Method().Name.Name == "SendMail" || call.Method().Name.Name == "SendBulkMail") && call.Method().DeclaringType.FullName == "CDS.Core.Utils.EMail")
                {
                    Problems.Add(new Problem(this.GetResolution()));
                }
            }

            base.VisitMethodCall(call);
        }
    }
}
