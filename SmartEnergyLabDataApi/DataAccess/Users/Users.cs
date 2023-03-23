using HaloSoft.DataAccess;
using SmartEnergyLabDataApi.Elsi;
using NHibernate.Criterion;

namespace SmartEnergyLabDataApi.Data
{
    public class Users : DataSet
    {
        public Users(DataAccess da) : base(da)
        {

        }

        public DataAccess DataAccess
        {
            get
            {
                return (DataAccess) _dataAccess;
            }
        }

        #region users

        public void Add(User user) {
            Session.Save(user);
        }

        public void Delete(User user) {
            Session.Delete(user);
        }

        public User GetUser(int userId) {
            return Session.Get<User>(userId);
        }

        public bool ContainsUser(string email, int id) {
            var count = Session.QueryOver<User>().Where( m=>m.Email.IsLike(email) && m.Id!=id).RowCount();
            return count!=0;
        }

        public User GetUser(string email) {
            var user = Session.QueryOver<User>().Where( m=>m.Email.IsLike(email) ).Take(1).SingleOrDefault();
            return user;
        }

        #endregion

    }
}

