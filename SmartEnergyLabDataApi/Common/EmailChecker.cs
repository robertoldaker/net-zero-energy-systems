using System.Text.RegularExpressions;

namespace SmartEnergyLabDataApi.Common
{
    public class EmailChecker
    {
        private Regex _regex;
        public EmailChecker()
        {
            _regex = new Regex(@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$",RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }
        public bool Check(string email)
        {
            if (email == null) return false;
            var match = _regex.Match(email);
            return match.Success;
        }
    }
}