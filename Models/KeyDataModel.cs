public class KeyDataModel
{
    //isto e uma estrutura de dados que vai representar o json que estou a receberm assim consigo 
    public string CipherMode { get; set; } // "CBC" ou "CTR"
    public string EncryptedPrivateKey { get; set; }
}