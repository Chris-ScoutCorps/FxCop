using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    //there's also a good tutorial here, yo: http://www.binarycoder.net/fxcop/html/check_and_visit.html

    /// <summary>
    /// This is a sample rule to provide a demonstration of a 'hello world' FxCop rule. It flags any methods whose names start with 'c' as errors.
    /// </summary>
    public class HelloWorldRule : BaseRule
    {
        public HelloWorldRule() : base("HelloWorldRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.All;
            }
        }

        public override ProblemCollection Check(Member member)
        {
            Method m = member as Method;
            if (m != null && m.Name.Name.StartsWith("c", StringComparison.OrdinalIgnoreCase))
            {
                this.Problems.Add(new Problem(this.GetResolution(m.FullName)));
            }
            return this.Problems;
        }
    }
}
