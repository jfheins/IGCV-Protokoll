using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace IGCV_Protokoll.util
{
    // http://stackoverflow.com/a/2139922

        /// <summary>
        /// Allows Authorization for users in roles as well as the given users.
        /// </summary>
    public class PermissiveAuthorizationAttribute : AuthorizeAttribute
    {
        // This method must be thread-safe since it is called by the thread-safe OnCacheAuthorization() method.
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            base.AuthorizeCore(httpContext);

            if (_usersSplit == null || _rolesSplit == null)
            {
                // wish base._usersSplit were protected instead of private...
                InitializeSplits();
            }

            IPrincipal user = httpContext.User;
            if (!user.Identity.IsAuthenticated)
            {
                return false;
            }

            var userRequired = _usersSplit.Length > 0;
            var userValid = userRequired
                && _usersSplit.Contains(user.Identity.Name, StringComparer.OrdinalIgnoreCase);

            var roleRequired = _rolesSplit.Length > 0;
            var roleValid = (roleRequired)
                && _rolesSplit.Any(user.IsInRole);

            var userOrRoleRequired = userRequired || roleRequired;

            return (!userOrRoleRequired) || userValid || roleValid;
        }

        private string[] _rolesSplit;
        private string[] _usersSplit;

        private void InitializeSplits()
        {
            lock (this)
            {
                if (_rolesSplit == null || _usersSplit == null)
                {
                    _rolesSplit = SplitString(Roles);
                    _usersSplit = SplitString(Users);
                }
            }
        }

        private static readonly char[] _splitParameter = new char[1] {','};

        private static string[] SplitString(string original)
        {
            if (string.IsNullOrEmpty(original))
                return new string[0];

            return ((IEnumerable<string>)original.Split(_splitParameter)).Select(piece => piece.Trim())
                .Where(x => !string.IsNullOrEmpty(x)).ToArray<string>();
        }
    }
}