using System.Text.Json.Serialization;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data
{
    public enum UserRole { Basic, Admin}

    [Class(0, Table = "smart_energy_users")]
    public class User
    {
        public User()
        {

        }

        /// <summary>
        /// Database identifier
        /// </summary>
        [Id(0, Name = "Id", Type = "int")]
        [Generator(1, Class = "identity")]
        public virtual int Id { get; set; }

        [Property()]
        public virtual string Name {get; set;}

        [Property()]
        public virtual string Email {get; set;}

        [Property()]
        [JsonIgnore]
        public virtual byte[] Password {get; set;}

        [Property()]
        [JsonIgnore]
        public virtual byte[] Salt {get; set;}

        [Property()]
        public virtual bool Enabled {get; set;}

        [Property()]
        public virtual UserRole Role {get; set;}

        public virtual string RoleStr {
            get {
                return this.Role.ToString();
            }
        }


    }
}