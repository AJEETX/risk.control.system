using System.Security.Cryptography;

namespace risk.control.system.Helpers
{
    public static class NameGenerator
    {
        private static readonly string[] firstNames = { "John", "Paul", "Ringo", "George", "Laura", "Stephaney" };
        private static readonly string[] lastNames = { "Lennon", "McCartney", "Starr", "Harrison", "Blanc", "Keir" };

        public static string GenerateName()
        {
            var random = new Random();
            int index = RandomNumberGenerator.GetInt32(0, firstNames.Length);
            string firstName = firstNames[index];
            string lastName = lastNames[index];

            return $"{firstName} {lastName}";
        }
    }
}