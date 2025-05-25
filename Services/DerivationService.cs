using System;
using System.Security.Cryptography;
public class DerivationService{

    private static int num_iter = 100000;
    /*
    * function that generates the salt 
    * @param sizeSalt - lenght of the salt 
    */
    public static byte[] genSalt(int salt_size){
        byte[] salt = new byte[salt_size];

        using (RandomNumberGenerator rand_gen = RandomNumberGenerator.Create()){
            rand_gen.GetBytes(salt);
        }

        return salt;
    }

    // this function returns the deriveKey and the respective salt
    public static (byte[] Kderivada, byte[] salt) DeriveKey(string password){
        //generate random salt with 16 bytes
        byte[] salt = genSalt(16);//16 bytes
        //PBKDF2  HMAC = f(salt, password, num_iter, SHA256)-> number of iter to reforce hashing
        Rfc2898DeriveBytes password_derived = new Rfc2898DeriveBytes(password, salt, num_iter, HashAlgorithmName.SHA256);
        //extract the first 32bytes of the PBKDF2 obj, enough for AES256 enc input requirements
        return (password_derived.GetBytes(32), salt);//salt is important to store in the json for later decryption
    
    }


    /*
     * método que deriva com o salt associado à password 
     *
    */
    //same thing but does not generate a random salt
    //derive pass to decript the sk with a given salt and criptogram(sk)
    public static byte[] DeriveKey(string password, byte[] salt){

        Rfc2898DeriveBytes password_derived = new Rfc2898DeriveBytes(password, salt, num_iter, HashAlgorithmName.SHA256);

        return (password_derived.GetBytes(32));

    
    }   
}