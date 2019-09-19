namespace Diffology
{
    /// <summary>
    /// Encapsulates all user information required to produce commits and
    /// authenticate with the origin.
    /// </summary>
    public struct User
    {
        internal string Name;
        internal string Email;
        internal string Password;

        public User(string name, string email, string password)
        {
            Name = name;
            Email = email;
            Password = password;
        }
    }
}
