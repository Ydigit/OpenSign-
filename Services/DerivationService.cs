using System;
using System.Security.Cryptography;
public class DerivationService{

    private static int num_iter = 100000;
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
    public static (byte[] Kderivada, byte[] salt) DeriveKey(string password){

        byte[] salt = genSalt(16);//16 bytes?
    
        Rfc2898DeriveBytes password_derived = new Rfc2898DeriveBytes(password, salt, num_iter, HashAlgorithmName.SHA256);
        //correcao do rafa, tinha 256, mas tem de ser o numero de bytes nao de bits
        //depois de gerar o derivado em bytes, retiramos os bytes da propria chave ja derivada
        return (password_derived.GetBytes(32), salt);
    
    }


    /*
     * método que deriva com o salt associado à password 
     *
    */
    public static byte[] DeriveKey(string password, byte[] salt){

        Rfc2898DeriveBytes password_derived = new Rfc2898DeriveBytes(password, salt, num_iter, HashAlgorithmName.SHA256);

        return (password_derived.GetBytes(32), salt);

    
    }   
}