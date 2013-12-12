using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public class SQLInjectionRule : BaseRule
    {
        public SQLInjectionRule() : base("SQLInjectionRule") { }

        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.All;
            }
        }

        static Member _currentMember = null;
        static object _currentMemberLock = new object();

        bool _problemRound = false;
        DirtySafeCollection _dirty = null;

        public override ProblemCollection Check(Member member)
        {
            if (member.DeclaringType.DeclaringModule.ContainingAssembly.IsDataDll())
            {
                lock (_currentMemberLock)
                {
                    _currentMember = member;

                    _problemRound = false;
                    _dirty = new DirtySafeCollection() { SafetyPass = true };

                    var meth = member as Method;
                    if (meth != null && meth.Parameters != null)
                    {
                        VisitParameters(meth.Parameters);
                    }

                    do
                    {
                        _dirty.Updated = false;
                        VisitMethodCallStatements(member);
                    } while (_dirty.Updated); //keep going until it stops marking new things safe

                    _dirty.SafetyPass = false;
                    do
                    {
                        _dirty.Updated = false;
                        VisitMethodCallStatements(member);
                    } while (_dirty.Updated); //keep going until it stops marking new things safe

                    _problemRound = true;
                    VisitMethodCallStatements(member);
                }
            }

            return Problems;
        }

        public override void VisitParameters(ParameterCollection parameters)
        {
            foreach (var p in parameters.Where(w => !IsTypeSafe(w.Type)))
            {
                if (IsSqlExecutingFunction(_currentMember))
                    _dirty.MarkSafe(p);
                else
                    _dirty.MarkDirty(p, null, p.DeclaringMethod.Body.SourceContext, true);
            }

            base.VisitParameters(parameters);
        }

        public override void VisitAssignmentStatement(AssignmentStatement assignment)
        {
            if (!IsTypeSafe(assignment.Target.Type))
            {
                if (IsConst(assignment.Source))
                {
                    _dirty.MarkSafe(assignment.Target);
                }
                else
                {
                    var sm = assignment.Source as MethodCall;
                    if (sm != null)
                    {
                        if (IsSafeStringFunction(sm, assignment.Target))
                        {
                            _dirty.MarkSafe(assignment.Target);
                            _dirty.MarkSafe(assignment.Source);
                        }
                    }

                    var tern = assignment.Source as TernaryExpression;
                    if (tern != null)
                    {
                        if (IsConst(tern.Operand2) && IsConst(tern.Operand3))
                        {
                            _dirty.MarkSafe(assignment.Source);
                            _dirty.MarkSafe(assignment.Target);
                        }
                    }

                    var con = assignment.Source as Construct;
                    if (con != null)
                    {
                        if (con.Operands.All(a => IsConst(a)))
                        {
                            _dirty.MarkSafe(assignment.Source);
                            _dirty.MarkSafe(assignment.Target);
                        }
                    }

                    var conarr = assignment.Source as ConstructArray;
                    if (conarr != null)
                    {
                        if (conarr.Operands.All(a => IsConst(a)))
                        {
                            _dirty.MarkSafe(assignment.Source);
                            _dirty.MarkSafe(assignment.Target);
                        }
                    }

                    if (_dirty.IsSafe(assignment.Source))
                    {
                        _dirty.MarkSafe(assignment.Target);
                    }
                    else
                    {
                        _dirty.MarkDirty(assignment.Target, assignment.Source, assignment.SourceContext, false);
                    }
                }
            }
            else
            {
                _dirty.MarkSafe(assignment.Target);
            }
            
            base.VisitAssignmentStatement(assignment);
        }

        public override void VisitMethodCall(MethodCall call)
        {
            bool safe = true;
            
            var mb = call.Callee as MemberBinding;
            if (mb != null)
            {
                if (IsStringFunction(call))
                {
                    if (!IsSafeStringFunction(call, null))
                    {
                        safe = false;
                        _dirty.MarkDirty(mb.TargetObject, mb.BoundMember, call.SourceContext, false);
                    }
                }
                else if (mb.TargetObject != null && !_dirty.IsSafe(mb.TargetObject) && IsStringIsh(call.Type) && !call.Method().DeclaringType.IsDerivedFrom("CDS.Core.Utils.Inspection.SafeSqlBuilder"))
                {
                    safe = false;
                    _dirty.MarkDirty(call, mb.TargetObject, call.SourceContext, false);
                }
            }
            
            if (safe)
            {
                _dirty.MarkSafe(call);
            }

            if (!IsSqlExecutingFunction(call.Method()) && !IsSqlGeneratingFunction(call.Method()) && !IsSafeStringFunction(call, null) && !IsStringIsh(call.Method().DeclaringType))
            {
                // mark the reference-type operands as dirty unless it's
                //  ... executing SQL or building dynamic SQL (those get scanned on the inside)
                //  ... a string or StringBuilder function. We know that those don't alter their inputs.
                foreach (var op in call.Operands.Where(w => w.Type != null && !w.Type.IsValueType && !IsTypeSafe(w.Type)))
                {
                    _dirty.MarkDirty(op, call.Method(), call.SourceContext, false);
                }
            }

            //if the function runs SQL
            // an operand's been marked dangerous, PROBLEM!
            if (_problemRound && IsSqlExecutingFunction(call.Method()) && _dirty.AnyDirty)
            {
                var dirty = call.Operands.FirstOrDefault(f => IsStringIsh(f.Type) && !IsConst(f) && !_dirty.IsSafe(f));
                if (dirty != null)
                {
                    Problems.Add(new Problem(this.GetResolution(dirty.GetName(), _dirty.GetDirtyDetails(dirty, call, true)), call.SourceContext));
                }
            }

            base.VisitMethodCall(call);
        }

        public override void VisitReturn(ReturnNode returnInstruction)
        {
            if (_problemRound && IsSqlGeneratingFunction(_currentMember))
            {
                if (returnInstruction.Expression != null && IsStringIsh(returnInstruction.Expression.Type) && !IsConst(returnInstruction.Expression) && !_dirty.IsSafe(returnInstruction.Expression))
                {
                    Problems.Add(new Problem(this.GetResolution(returnInstruction.Expression.GetName(), _dirty.GetDirtyDetails(returnInstruction.Expression, returnInstruction, true)), returnInstruction.SourceContext));
                }
                
                var currentMethod = _currentMember as Method;
                foreach (var p in currentMethod.Parameters.Where(w => !IsTypeSafe(w.Type)))
                {
                    //if it's been marked dirty (aside from being a param), DANGER!
                    if (_dirty.MarkedDirtyInsideMethod(p))
                    {
                        Problems.Add(new Problem(this.GetResolution(p.GetName(), _dirty.GetDirtyDetails(p, returnInstruction, false)), returnInstruction.SourceContext));
                    }
                }
            }

            base.VisitReturn(returnInstruction);
        }
        
        private bool IsStringFunction(MethodCall call)
        {
            return IsStringIsh(call.Method().DeclaringType);
        }

        private bool IsSafeStringFunction(MethodCall call, Node target)
        {
            if (call.Method().DeclaringType.IsDerivedFrom("CDS.Core.Utils.Inspection.SafeSqlBuilder"))
            {
                return true;
            }

            if (!IsSqlGeneratingFunction(call.Method()))
            {
                if (!IsStringFunction(call))
                    return false;

                if (call.Method().Name.Name == "ToString")
                    return _dirty.IsSafe(call.Callee);
            }

            foreach (var op in call.Operands.Where(w => w != target && !IsTypeSafe(w)))
            {
                var nestedCall = op as MethodCall;
                if (nestedCall != null)
                {
                    if (!IsSafeStringFunction(nestedCall, target))
                        return false;
                }
                else if (!_dirty.IsSafe(op) && !IsConst(op))
                {
                    return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// It seems a little weird at first that we'd treat something that comes from a dynamic-SQL-generating function as safe
        /// But those will be scanned separately. So we'll give an error for the function that generates the SQL (if applicable), and not in every place that uses it!
        /// </summary>
        private bool IsSqlGeneratingFunction(Member member)
        {
            return member.Attributes != null && member.Attributes.Any(a => a.Type.FullName == "CDS.Core.Utils.Inspection.BuildsDynamicSqlAttribute");
        }
    }

    public class DirtySafeCollection
    {
        private HashSet<int> _everSafeVars = new HashSet<int>();
        private HashSet<int> _safeVars = new HashSet<int>();
        private Dictionary<int, HashSet<DirtyDetails>> _dirtyVars = new Dictionary<int, HashSet<DirtyDetails>>();
        private Dictionary<int, HashSet<int>> _dirtyToDirty = new Dictionary<int, HashSet<int>>();

        public bool Updated { get; set; }
        public bool SafetyPass { get; set; }

        public bool AnyDirty { get { return _dirtyVars.Any(); } }

        public void MarkDirty(Node n, Node from, SourceContext ctx, bool asParam)
        {
            if ((SafetyPass && !asParam) || n == null)
                return;

            if (_safeVars.Contains(n.UniqueKey))
                _safeVars.Remove(n.UniqueKey);

            if (!_dirtyVars.ContainsKey(n.UniqueKey))
            {
                _dirtyVars.Add(n.UniqueKey, new HashSet<DirtyDetails>());
                Updated = true;
            }
            _dirtyVars[n.UniqueKey].Add(new DirtyDetails(ctx, asParam));

            if (from != null)
            {
                if (!_dirtyToDirty.ContainsKey(n.UniqueKey))
                    _dirtyToDirty.Add(n.UniqueKey, new HashSet<int>());
                if (_dirtyToDirty.ContainsKey(from.UniqueKey))
                {
                    foreach (var dd in _dirtyToDirty[from.UniqueKey])
                        _dirtyToDirty[n.UniqueKey].Add(dd);
                }
                _dirtyToDirty[n.UniqueKey].Add(from.UniqueKey);
            }

            var indexer = n as Indexer;
            if (indexer != null)
            {
                MarkDirty(indexer.Object, indexer, ctx, asParam);
            }
        }

        public void MarkSafe(Node n)
        {
            if (_dirtyVars.ContainsKey(n.UniqueKey))
                return;

            if (!_everSafeVars.Contains(n.UniqueKey))
            {
                Updated = true;
            }

            _safeVars.Add(n.UniqueKey);
            _everSafeVars.Add(n.UniqueKey);
        }

        public bool IsSafe(Node n)
        {
            return _safeVars.Contains(n.UniqueKey);
        }

        public bool MarkedDirtyInsideMethod(Node n)
        {
            return _dirtyVars.ContainsKey(n.UniqueKey) && _dirtyVars[n.UniqueKey].Any(a => !a.AsParam);
        }

        private IEnumerable<DirtyDetails> GetDirtyDetails(Node n)
        {
            if (_dirtyVars.ContainsKey(n.UniqueKey))
            {
                foreach (var det in _dirtyVars[n.UniqueKey])
                    yield return det;
            }
            if (_dirtyToDirty.ContainsKey(n.UniqueKey))
            {
                foreach (var dd in _dirtyToDirty[n.UniqueKey].Where(w => _dirtyVars.ContainsKey(w)))
                {
                    foreach (var det in _dirtyVars[dd])
                        yield return det;
                }
            }
        }

        public string GetDirtyDetails(Node dirty, Node calledFrom, bool includeAsParam)
        {
            var details = new StringBuilder();
            string curFile = calledFrom.SourceContext.FileName;

            var dirtyDetails = GetDirtyDetails(dirty);
            if (!dirtyDetails.Any()) //this usually means it's marked dirty inline
            {
                var nodeDetails = new DirtyDetails(calledFrom.SourceContext, false);
                dirtyDetails = dirtyDetails.Union(new DirtyDetails[] { nodeDetails });
            }
            foreach (var dd in dirtyDetails.Where(w => includeAsParam || !w.AsParam))
            {
                if (curFile == dd.Filename)
                    details.AppendFormat("line {0}, ", dd.Line);
                else
                    details.AppendFormat("{0} line {1}, ", dd.Filename, dd.Line);
                curFile = dd.Filename;
            }

            return details.ToString();
        }
    }

    public class DirtyDetails //gimme the dirty details
    {
        public string Filename { get; private set; }
        public int Line { get; private set; }
        public bool AsParam { get; private set; }

        public DirtyDetails(SourceContext ctx, bool asParam)
        {
            Filename = ctx.FileName;
            Line = ctx.StartLine;
            AsParam = asParam;
        }

        public override bool Equals(object obj)
        {
            var o = obj as DirtyDetails;
            if (o == null) return false;
            return Line == o.Line && Filename == o.Filename && AsParam == o.AsParam;
        }

        public override int GetHashCode()
        {
            return (Filename ?? "").GetHashCode() + Line.GetHashCode();
        }
    }
}
