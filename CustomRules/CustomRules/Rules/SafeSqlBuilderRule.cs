using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class SafeSqlBuilderRule : BaseRule
    {
        public SafeSqlBuilderRule() : base("SafeSqlBuilderRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.All;
            }
        }

        //test cases:
        // method, prop, constructor
        // call a dangerous function in another class

        static TypeNode _currentType = null;
        static object _currentTypeLock = new object();

        public override ProblemCollection Check(TypeNode cls)
        {
            lock (_currentTypeLock)
            {
                if (!cls.IsDerivedFrom("CDS.Core.Utils.Inspection.SafeSqlBuilder"))
                    return Problems;

                _currentType = cls;

                foreach (var m in cls.Members)
                {
                    var meth = m as Method;
                    if (meth != null && !m.IsPrivate)
                        VisitParameters(meth.Parameters);
                    
                    VisitMethodCallStatements(m);
                }
            }

            return Problems;
        }

        public override void VisitParameters(ParameterCollection parameters)
        {
            foreach (var p in parameters)
            {
                if (!IsTypeSafe(p.Type))
                {
                    Problems.Add(new Problem(this.GetResolution(), p.DeclaringMethod.Body.SourceContext));
                }
            }

            base.VisitParameters(parameters);
        }

        public override void VisitMethodCall(MethodCall call)
        {
            if (IsSqlExecutingFunction(call.Method()))
            {
                Problems.Add(new Problem(this.GetResolution(), call.SourceContext));
            }

            if (!call.Method().DeclaringType.FullName.StartsWith("System.") && !call.Method().DeclaringType.FullName.StartsWith("Microsoft.") && call.Method().DeclaringType != _currentType && call.Method().FullName != "CDS.Core.Utils.Inspection.SafeSqlBuilder.#ctor")
            {
                Problems.Add(new Problem(this.GetResolution(), call.SourceContext));
            }

            base.VisitMethodCall(call);
        }
    }
}