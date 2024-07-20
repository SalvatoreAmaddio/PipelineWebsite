using Microsoft.Extensions.Configuration;

namespace Pipeline
{
    /// <summary>
    /// The User class is used to provide login credentials for accessing webpages.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets the username of the user.
        /// </summary>
        public string UserName { get; }

        /// <summary>
        /// Gets the password of the user.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Initializes a new instance of the User class with the specified username and password.
        /// </summary>
        /// <param name="userName">The username of the user.</param>
        /// <param name="password">The password of the user.</param>
        public User(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public User(IConfigurationRoot configuration) : this(configuration["Credentials:Username"]!, configuration["Credentials:Password"]!)
        {
        }

        public override string? ToString() => UserName;
        public override bool Equals(object? obj) => obj is User user && UserName == user.UserName;
        public override int GetHashCode() => HashCode.Combine(UserName);
    }
}