using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class StaticDisposableRule : BaseRule
    {
        public StaticDisposableRule() : base("StaticDisposableRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.All;
            }
        }

        public override ProblemCollection Check(Member member)
        {
            var field = member as Field;
            if (field != null && field.IsStatic && field.Type.IsDerivedFrom("System.IDisposable") && field.Type.FullName != "System.Runtime.Caching.MemoryCache" && !field.Type.FullName.StartsWith("System.Threading.ThreadLocal`"))
            {
                Problems.Add(new Problem(this.GetResolution()));
            }

            return Problems;
        }

        public override ProblemCollection Check(TypeNode type)
        {
            if (type.Attributes.Any(a => a.Type.FullName == "CDS.ProxyFactory.SingletonAttribute") && type.IsDerivedFrom("System.IDisposable"))
            {
                Problems.Add(new Problem(this.GetResolution()));
            }

            return Problems;
        }
    }
}
