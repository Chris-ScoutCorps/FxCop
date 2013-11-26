using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class UnloggedThreadingRule : BaseRule
    {
        public UnloggedThreadingRule() : base("UnloggedThreadingRule") { }

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
                if (
                    (call.Method().Name.Name == "Start" && call.Method().DeclaringType.FullName == "System.Threading.Thread")
                    || (call.Method().Name.Name == "StartNew" && call.Method().DeclaringType.FullName == "System.Threading.Tasks.Task.Factory")
                    || (call.Method().Name.Name == "Start" && call.Method().DeclaringType.FullName == "System.Threading.Tasks.Task")
                    || (call.Method().Name.Name == "StartNew" && call.Method().DeclaringType.FullName == "System.Threading.Tasks.TaskFactory")
                    )
                {
                    Problems.Add(new Problem(this.GetResolution()));
                }
            }

            base.VisitMethodCall(call);
        }
    }
}
