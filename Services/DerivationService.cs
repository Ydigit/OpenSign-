using System;
using System.Security.Cryptography;


/**
 * @brief Provides key derivation functions using PBKDF2 with HMAC-SHA256.
 */
public class DerivationService{

    /**
     * @brief Number of iterations for the PBKDF2 algorithm.
     *
     * Higher the number of iterations, higher resistance against brute-force attacks.
     */
    private static int num_iter = 100000;
  
    /**
     * @brief Generates a random salt.
     *
     * Creates a random byte array (salt) of the specified length.
     *
     * @param salt_size The length of the salt in bytes.
     * @return An array of bytes containing the generated salt.
     */
    public static byte[] genSalt(int salt_size){
        byte[] salt = new byte[salt_size];

        using (RandomNumberGenerator rand_gen = RandomNumberGenerator.Create()){
            rand_gen.GetBytes(salt);
        }

        return salt;
    }

   /**
     * @brief Derives a cryptographic key from a plain-text password using a randomly generated salt from the previous function.
     *
     * This function uses PBKDF2 with HMAC-SHA256 to derive a 32-byte key from the given plain-text password.
     * It generates a random salt (16 bytes) internally, then applies the key derivation function.
     *
     * @param password The palin-text password used for key derivation.
     * @return A tuple containing:
     *         - The derived key (32 bytes).
     *         - The salt (16 bytes) generated in the process.
     */
    public static (byte[] Kderivada, byte[] salt) DeriveKey(string password){
        //generate random salt with 16 bytes
        byte[] salt = genSalt(16);//16 bytes
        //PBKDF2  HMAC = f(salt, password, num_iter, SHA256)-> number of iter to reforce hashing
        Rfc2898DeriveBytes password_derived = new Rfc2898DeriveBytes(password, salt, num_iter, HashAlgorithmName.SHA256);
        //extract the first 32bytes of the PBKDF2 obj, enough for AES256 enc input requirements
        return (password_derived.GetBytes(32), salt);//salt is important to store in the json for later decryption
    
    }

    /**
     * @brief Derives a cryptographic key from a plain-text password using a provided salt.
     *
     * This function uses PBKDF2 with HMAC-SHA256 to derive a 32-byte key from the given plain-text password and salt.
     * It is particularly useful for scenarios where the same salt must be used, such as verifying data or decrypting content.
     *
     * @param password The plain-text password used for key derivation.
     * @param salt The salt previously generated and used in a prior derivation.
     * @return A 32-byte derived key.
     */
    public static byte[] DeriveKey(string password, byte[] salt){

        Rfc2898DeriveBytes password_derived = new Rfc2898DeriveBytes(password, salt, num_iter, HashAlgorithmName.SHA256);

        return (password_derived.GetBytes(32));

    
    }   
}