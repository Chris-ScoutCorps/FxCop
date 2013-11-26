FxCop
=====
A set of code inspection rules using FxCop, mostly dealing with detecting vulnerabilities to various attacks and managing an application's attack surface.


Projects
========
- FxCopUpdater: A simple utility to add/remove DLLs in a given directory to an FxCop project, keeping it up-to-date
- CustomRules: the custom FxCop rules


List of Rules
=============
There's also some information on these in the CustomRules.xml file and the PowerPoint deck.

Potentially Useful As-Is:
- SuspiciousGetRequestsRule:
  This flags any methods that allow HttpGet (as opposed to HttpPost) and have keywords that indicate that they may alter data on behalf of a user.
  This is useful in conjunction with a site-wide anti-CSRF solution to ensure that sloppy coding doesn't open up a breach.
  
- MissingHttpMethodRule:
  Falls under the category of "managing your attack surface". Any new Action without an HttpGet or HttpPost specified is flagged to be code reviewed.
  Used in conjunction with SuspiciousGetRequestsRule.
  
- JSONAllowGetRule:
  Protects against JSON Hijacking by flagging any action that returns JSON and overrides the default MVC behavior (DenyGet).
  
- XSSRule:
  Falls under the category of "managing your attack surface". Flags any user function that returns an IHtmlString as needing code review for possible XSS vulnerabilities.

- StaticDisposableRule:
  Not a security-related rule; any static variable of a type that is IDipoosable is probably bad mojo.

  
Useful With Minor Modifications:
(see the "MOD:" comments in the source code)
- MissingRoleCheckRule:
  Actions or Controllers that don't have attributes for Role checking (missing authorization control)

- ContextInDataDLLRule:
  Not a security-related rule; flags any use of .Context properties on the data layer of your application, as they may not be instantiated or may break caching of their results.


Specific to Our Organization/Application:
(but maybe you can get some ideas based on them)
- SkipAuthorizeAttribute:
  We've got a default platform-wide behavior to ensure that any request is authorized (user is logged-in) ... unless you add this attribute.
  This flags uses of this attribute for a code review.

- UnloggedEmailRule:
  We want to make sure people send email through our company's email library, not by hitting system functions directly.

- UnloggedThreadingRule:
  We want to make sure people spawn threads through our company's threading library, not by hitting system functions directly.

  
Totally Experimental:
(maybe you can complete what we've started)
- SafeSqlBuilderRule + SQLInjectionRule:
  These, in conjunction, look for potential SQL injection by ensuring that potentially "dirty" (affected by user input or unknown input) strings aren't passed as SQL to the database.
  It's nowhere near production/release ready, but it has caught legit issues in our code base along with the false alarms...
