using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace CustomRules
{
    public static class Extensions
    {
        public static bool IsDerivedFrom(this TypeNode typeNodeToTest, string fullTypeName)
        {
            if (typeNodeToTest.GetFullUnmangledNameWithoutTypeParameters() == fullTypeName)
                return true;
            else if (typeNodeToTest.Interfaces.Any(i => i.GetFullUnmangledNameWithoutTypeParameters() == fullTypeName))
                return true;
            else if (typeNodeToTest.BaseType == null)
                return false;
            else
                return typeNodeToTest.BaseType.IsDerivedFrom(fullTypeName);
        }

        public static bool IsController(this TypeNode node)
        {
            return node.IsPublic && node.IsDerivedFrom("System.Web.Mvc.Controller");
        }

        public static bool IsControllerAction(this Member member)
        {
            Method m = member as Method;
            if (m == null) return false;
            return m.IsPublic && m.Name.Name != ".ctor" && m.DeclaringType.IsController() && m.ReturnType.IsDerivedFrom("System.Web.Mvc.ActionResult");
        }

        public static bool HasHttpPostAttribute(this Member member)
        {
            Method m = member as Method;
            if (m == null) return false;
            return m.Attributes.Any(a => a.Type.FullName == "System.Web.Mvc.HttpPostAttribute");
        }

        public static bool HasHttpGetAttribute(this Member member)
        {
            Method m = member as Method;
            if (m == null) return false;
            return m.Attributes.Any(a => a.Type.FullName == "System.Web.Mvc.HttpGetAttribute");
        }

        public static bool HasActionMethodSelectorAttribute(this Member member)
        {
            Method m = member as Method;
            if (m == null) return false;
            return m.Attributes.Any(a => a.Type.IsDerivedFrom("System.Web.Mvc.ActionMethodSelectorAttribute"));
        }

        public static Method Method(this MethodCall call)
        {
            var mb = call.Callee as MemberBinding;
            if (mb == null)
                return null;

            var method = mb.BoundMember as Method;
            return method;
        }

        public static bool IsSystemAssembly(this AssemblyNode assembly)
        {
            string assemblyFile = (assembly.Location ?? "").ToLower();
            return assemblyFile.Contains(":\\Program Files (x86)\\Microsoft ASP.NET\\ASP.NET MVC 3\\Assemblies".ToLower())
                || assemblyFile.Contains(":\\Program Files (x86)\\Microsoft ASP.NET\\ASP.NET MVC 4\\Assemblies".ToLower())
                || assemblyFile.Contains(":\\Program Files (x86)\\Microsoft ASP.NET\\ASP.NET Web Pages\\v1.0\\Assemblies".ToLower())
                || assemblyFile.Contains(":\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework".ToLower());
        }

        public static bool IsPropertyAccessor(this Method method)
        {
            if (method.IsSpecialName && (method.Name.Name.StartsWith("set_") || method.Name.Name.StartsWith("get_")))
            {
                string lookForProp = method.Name.Name.Substring(4);
                return method.DeclaringType.Members.Any(a => (a as PropertyNode) != null && a.Name.Name == lookForProp);
            }

            return false;
        }

        public static bool IsDataDll(this AssemblyNode dll)
        {
            //MOD: this is our data access library. Insert yours here.
            return dll.AssemblyReferences.Any(a => a.Name.ToUpper() == "CDS.ORM");
        }

        public static string GetName(this Node node)
        {
            var p = node as Parameter;
            if (p != null) return p.Name.Name;

            var v = node as Variable;
            if (v != null) return v.Name.Name;

            var l = node as Local;
            if (l != null) return l.Name.Name;

            var m = node as Member;
            if (m != null) return m.Name.Name;

            var mb = node as MemberBinding;
            if (mb != null) return mb.TargetObject.GetName();

            var call = node as MethodCall;
            if (call != null)
            {
                return call.Callee.GetName() + "." + call.Method().Name.Name;
            }

            return null;
        }
    }
}
