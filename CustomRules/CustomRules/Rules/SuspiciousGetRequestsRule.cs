using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class SuspiciousGetRequestsRule : BaseRule
    {
        public SuspiciousGetRequestsRule() : base("SuspiciousGetRequestsRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.ExternallyVisible;
            }
        }

        public override ProblemCollection Check(Member member)
        {
            if (member.IsControllerAction()
                && (!member.HasHttpPostAttribute() || member.HasHttpGetAttribute()) //it allows gets
                && !(member.DeclaringType.Members.Any(a => a.Name.Name == member.Name.Name && a.HasHttpPostAttribute() && !a.HasHttpGetAttribute())) //and there's no member with the same name that's post-only
            )
            {
                if (!KeywordViolations(member))
                {
                    VisitMethodCallStatements(member);
                }
            }
            return Problems;
        }

        public override void VisitMethodCall(MethodCall call)
        {
            if (call.Method() != null)
            {
                if (!call.Method().DeclaringType.DeclaringModule.ContainingAssembly.IsSystemAssembly() //only need to check our own stuff, we can't do data access through a MSFT function from our web projects
                    && call.Method().DeclaringType.DeclaringModule.ContainingAssembly.Name != "CDS.Core.Utils" //MOD: we've whitelisted some stuff, here
                    && call.Method().DeclaringType.DeclaringModule.ContainingAssembly.Name != "CDS.ProxyFactory"
                    && !call.Method().IsPropertyAccessor()) //call me overconfident, but I think we can assume property accessors aren't writing to the database
                {
                    KeywordViolations(call.Method());
                }
            }

            base.VisitMethodCall(call);
        }

        private static readonly string[] _alterDataKeywords = new string[] { "Set", "Save", "Alter", "Update", "Change", "Create", "Edit", "Rename", "Delete", "Add", "Remove", "Upload", "Share", "Transfer", "Move" };
        private bool KeywordViolations(Member toCheck)
        {
            string nameToMatch = toCheck.Name.Name;
            int genericStart = nameToMatch.IndexOf('<');
            if (genericStart > -1)
                nameToMatch = nameToMatch.Substring(0, genericStart);

            var matches = _alterDataKeywords
                .Where(w => nameToMatch.IndexOf(w, StringComparison.OrdinalIgnoreCase) > -1)
                .Where(w => ValidateMatch(nameToMatch, w));

            if (matches.Any())
            {
                string issues = (matches.Count() > 1 ? "s " : " ") + string.Join(", ", matches.Select(s => "'" + s + "'"));
                string name = toCheck.DeclaringType.FullName + "." + toCheck.Name.Name;
                Problems.Add(new Problem(this.GetResolution(name, issues), toCheck.SourceContext));
                return true;
            }

            return false;
        }

        //Yes, I could use a bunch of regexes, but it'd be nice if people could actually read it
        private bool ValidateMatch(string nameToMatch, string matchedKeyword)
        {
            if (matchedKeyword == "Update" && nameToMatch.StartsWith("Get") && nameToMatch.Contains("Updates")) //I think case should matter here to be safe
            {
                return false;
            }

            return true;
        }
    }
}
