using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace LoginTests
{
    public class Server
    {
        private const string tableName = "LoginTable";
        private const string usernameColumn = "Username";
        private const string passwordColumn = "Password";
        private const int SALT_LENGTH = 16;
        private const int HASH_LENGTH = 20;
        private const int ITERATIONS = 10000;


        private readonly string _DbConnection;

        public Server()
        {
            _DbConnection = File.ReadAllText("C:/Temp/LoginTests/connection.txt");
        }

        public bool TryLogin(string username, string password, out string message)
        {
            message = "";
            if (string.IsNullOrEmpty(username) || password.Length == 0)
            {
                message = "No login or password given";
                return false;
            }

            using (var connection = new SqlConnection(_DbConnection))
            {
                string query = $"SELECT {passwordColumn} FROM [LoginTable] WHERE {usernameColumn}='{username}'";
                var dataTable = ExecuteQuery(query, connection);

                if (dataTable.Rows.Count == 1)
                {
                    string passwordFromDb = dataTable.Rows[0][0].ToString();
                    if(CheckPassword(password, passwordFromDb))
                    {
                        message = "Login correct";
                        return true;
                    }
                    else
                    {
                        message = "Password incorrect";
                        return false;
                    }
                }
                else
                {
                    message = "Login incorrect";
                    return false;
                }

            }
        }

        public bool TryRegister(string username, string password, out string message)
        {
            message = "";
            using (var connection = new SqlConnection(_DbConnection))
            using (var command = new SqlCommand())
            {
                string query = $"SELECT * FROM [{tableName}] WHERE {usernameColumn}='{username}'";
                var dataTable = ExecuteQuery(query, connection);
                if (dataTable.Rows.Count > 0)
                {
                    message = "Login is already used";
                    return false;
                }

                query = $"INSERT INTO [{tableName}] ({usernameColumn}, {passwordColumn}) VALUES(@username, @password)";
                try
                {
                        command.CommandText = query;
                        command.Connection = connection;
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", HashNewPassword(password));
                        connection.Open();
                        var result = command.ExecuteNonQuery();

                        message = $"Registered successfully {username}";
                        return true;
                }
                catch (SqlException exception)
                {
                    message = $"Exception during registration.\n{exception}";
                    return false;
                }
            }
        }

        private DataTable ExecuteQuery(string query, SqlConnection connection)
        {
            var sqlDataAdapter = new SqlDataAdapter(query, connection);
            var dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);
            return dataTable;
        }

        private string HashNewPassword(string password)
        {
            byte[] salt = GenerateSalt(SALT_LENGTH);
            byte[] hash = GenerateHash(password, salt, ITERATIONS, HASH_LENGTH);
            byte[] hashedAndConcatenated = ConcatenateSaltAndHash(salt, hash);
            
            return Convert.ToBase64String(hashedAndConcatenated);
        }

        private bool CheckPassword(string password, string storedPassword)
        {
            byte[] saltAndHashedPassword = Convert.FromBase64String(storedPassword);
            byte[] salt = SplitSalt(saltAndHashedPassword, SALT_LENGTH);
            byte[] hashToCheck = GenerateHash(password, salt, ITERATIONS, HASH_LENGTH);

            for (var i = 0; i < HASH_LENGTH; i++)
            {
                if(hashToCheck[i] != saltAndHashedPassword[SALT_LENGTH+i])
                {
                    return false;
                }
            }

            return true;
        }

        private byte[] GenerateSalt(int length = 16)
        {
            var salt = new byte[length];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            return salt;
        }

        private byte[] GenerateHash(string password, byte[] salt, int iterations = 10000, int length = 20)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return deriveBytes.GetBytes(length);
            }
        }

        private byte[] ConcatenateSaltAndHash(byte[] salt, byte[] hash)
        {
            var concatenated = new byte[salt.Length + hash.Length];
            Array.Copy(salt, 0, concatenated, 0, salt.Length);
            Array.Copy(hash, 0, concatenated, salt.Length, hash.Length);

            return concatenated;
        }

        private byte[] SplitSalt(byte[] saltAndHash, int saltLength = 16)
        {
            var salt = new byte[saltLength];
            Array.Copy(saltAndHash, salt, saltLength);
            return salt;
        }
    }
}

// https://www.mking.net/blog/password-security-best-practices-with-examples-in-csharp
// https://medium.com/@mehanix/lets-talk-security-salted-password-hashing-in-c-5460be5c3aae
// https://simpleprogrammer.com/protect-your-passwords/
