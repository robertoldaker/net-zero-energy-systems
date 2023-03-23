using System.Net.Mail;
using SmartEnergyLabDataApi.Common;

namespace SmartEnergyLabDataApi.Data
{
    public static class UserMethods
    {
        public static bool VerifyPassword(this User u, string password )
        {
            return Crypto.VerifyPassword(password, u.Salt, u.Password);
        }

        public static bool ChangePassword(this User u, string oldPassword, string newPassword)
        {
            // if old password ok then change password
            if (u.VerifyPassword( oldPassword ) )
            {
                u.SetPassword(newPassword);
                //
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void SetPassword(this User u, string newPassword)
        {
            // update salt as well as password
            byte[] salt = Crypto.CreateSalt();
            u.Salt = salt;
            u.Password = Crypto.GetPasswordHash(newPassword, salt);
        }

        public static MailAddress GetMailAddress(this User usr)
        {
            var ec = new EmailChecker();
            if (ec.Check(usr.Email)) {
                return new MailAddress(usr.Email, usr.Name);
            } else {
                return null;
            }
        }

    }
}