using System;
using System.Security.Cryptography;
public class DerivationService{

    /*
    * function that generates the salt 
    * @param sizeSalt - lenght of the salt 
    */
    private static byte[] genSalt(int salt_size){
        byte[] salt = new byte[salt_size];

        using (RandomNumberGenerator rand_gen = RandomNumberGenerator.Create()){
            rand_gen.GetBytes(salt);
        }

        return salt;
    }

    /*
    * function that derivates the hash key from password 
    * @param password -> password introduced by the user 
    */
    public static byte[] DeriveKey(string password){

        byte[] salt = genSalt(16);//16 bytes?

        int num_iter = 100000;
    
        Rfc2898DeriveBytes password_derived = new Rfc2898DeriveBytes(password, salt, num_iter, HashAlgorithmName.SHA256);

        return password_derived.GetBytes(256);
    
    }
    
}