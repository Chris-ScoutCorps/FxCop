using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class MissingHttpMethodRule : BaseRule
    {
        public MissingHttpMethodRule() : base("MissingHttpMethodRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.ExternallyVisible;
            }
        }

        public override ProblemCollection Check(Member member)
        {
            if (member.IsControllerAction() && !member.HasActionMethodSelectorAttribute())
            {
                Problems.Add(new Problem(this.GetResolution()));
            }
            return Problems;
        }
    }
}
