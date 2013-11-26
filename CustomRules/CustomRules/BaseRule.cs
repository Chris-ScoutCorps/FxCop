using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public abstract class BaseRule : BaseIntrospectionRule
    {
        public BaseRule(string name) : base(name, "CustomRules.CustomRules", typeof(BaseRule).Assembly) { }

        /// <summary>
        /// Visits the statements in the body of a method, or both the getter and setter methods of a property
        /// </summary>
        protected void VisitMethodCallStatements(Member member)
        {
            var m = member as Method;
            if (m != null)
            {
                VisitStatements(m.Body.Statements);
            }

            var p = member as PropertyNode;
            if (p != null)
            {
                if (p.Getter != null) VisitStatements(p.Getter.Body.Statements);
                if (p.Setter != null) VisitStatements(p.Setter.Body.Statements);
            }
        }

        protected bool IsString(TypeNode type)
        {
            return type != null && type.FullName.Replace("@", "").Replace("&", "") == "System.String";
        }

        protected bool IsStringIsh(TypeNode type)
        {
            return type != null && (new string[] { "System.String", "System.Text.StringBuilder" }).Contains(type.FullName.Replace("@", "").Replace("&", ""));
        }


        static readonly string[] _safeTypes = { "System.DateTime", "System.Boolean" };
        static readonly string[] _maybeSafeGenericTypes = { "System.Nullable", "System.Collections.Generic.IEnumerable", "System.Func" };
        /// <summary>
        /// Numeric, bool, DateTime, etc. types are safe from SQL/XSS injection. User-defined types composed entirely of safe types are also safe.
        /// </summary>
        protected bool IsTypeSafe(TypeNode t, HashSet<string> checkedTypes = null)
        {
            if (t == null)
                return true;

            if (checkedTypes == null)
                checkedTypes = new HashSet<string>();
            else if (checkedTypes.Contains(t.FullName)) //don't recurse infinitely into self-referencing types
                return true;
            checkedTypes.Add(t.FullName);

            if (IsStringIsh(t) || (t.TemplateArguments != null && t.TemplateArguments.Any(a => IsStringIsh(a))))
                return false;

            if (t.IsPrimitiveNumeric || t is EnumNode)
                return true;

            if (_maybeSafeGenericTypes.Any(a => t.IsDerivedFrom(a)) && t.TemplateArguments != null && t.TemplateArguments.All(a => IsTypeSafe(a, checkedTypes)))
                return true;

            if (_safeTypes.Contains(t.FullName))
                return true;

            return t.Members.Where(w => w.Name.Name != "ToString").All(a => IsTypeSafe(a, checkedTypes));
        }

        protected bool IsTypeSafe(Member m, HashSet<string> checkedTypes = null)
        {
            var f = m as Field;
            if (f != null)
                return IsTypeSafe(f.Type, checkedTypes);

            var p = m as PropertyNode;
            if (p != null)
                return IsTypeSafe(p.Type, checkedTypes);

            var meth = m as Method;
            if (meth != null)
                return IsTypeSafe(meth.ReturnType, checkedTypes);

            return true;
        }

        protected bool IsTypeSafe(Expression ex, HashSet<string> checkedTypes = null)
        {
            var bx = ex as BinaryExpression;
            if (bx != null && bx.NodeType == NodeType.Box)
            {
                return IsConst(bx.Operand2);
            }
            else
            {
                return IsTypeSafe(ex.Type, checkedTypes);
            }
        }

        /// <summary>
        /// Is this a constant or a literal? (not settable)
        /// </summary>
        protected bool IsConst(Expression expression)
        {
            if (expression as Literal != null)
            {
                return true;
            }

            var mb = expression as MemberBinding;
            if (mb != null)
            {
                var f = mb.BoundMember as Field;
                if (f != null && f.IsInitOnly)
                    return true;
            }

            return false;
        }

        protected readonly string[] _typeCanRunSql = new string[] { "System.Data.SqlClient.SqlCommand", "CDS.ORM.Repository", "Dapper.SqlMapper" };
        protected readonly string[] _funcCanRunSql = new string[] { "execute", "query", "reader" };
        /// <summary>
        /// If something is executing SQL, we can trust the parameters, because anything dangerous is reported upon calling that method
        /// </summary>
        protected bool IsSqlExecutingFunction(Member member)
        {
            if (_typeCanRunSql.Any(a => member.DeclaringType.IsDerivedFrom(a)) && _funcCanRunSql.Any(a => member.Name.Name.ToLower().Contains(a)))
                return true;

            return member.Attributes != null && member.Attributes.Any(a => a.Type.FullName == "CDS.Core.Utils.Inspection.ExecutesSqlAttribute");
        } 
    }
}
