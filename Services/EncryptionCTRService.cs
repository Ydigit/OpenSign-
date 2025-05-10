//nao esquecer registar os servicos no Program.cs
//this are separate services to have the SPR responsabilidade unica
//RICARDO METER OS METODOS E FUNCOES NECESSARIAS PARA ENCRIPTAR A
//SK, devera retorna o o fichiero e o iv

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public class EncryptionCTRService
{
    public (byte[] EncryptedPrivateKey, byte[]nonce) Encrypt (string data,byte[] key)
    {
        using(var aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.ECB ; //CTR usa ECB

            byte[] nonce = new byte[8]; //gera valor random para juntar ao array debaixo
            RandomNumberGenerator.Fill(nonce);

            byte[]counterBlock = new byte[16]; //nonce + contador 
            Array.Copy(nonce,counterBlock,nonce.Length); //adiciona nonce ao array

            using(var encryptor = aes.CreateEncryptor())
            {   
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] cypherBytes = new byte[dataBytes.Length]; //este tamanho porque CTR não faz padding (XOR byte a byte)

                for(int i = 0;i < dataBytes.Length;i += 16) //bloco a bloco(cada bloco 16 bytes) 
                {
                    byte[] keyStream = encryptor.TransformFinalBlock(counterBlock,0,16); //encriptar cada bloco
                    int blockSize = Math.Min(16,dataBytes.Length - i); //para último bloco,caso seja menor que 16 

                    for(int j = 0;j < blockSize;j++)//byte a byte
                    {
                        cypherBytes[i+j] = (byte)(dataBytes[i+j] ^ keyStream[j]); 
                    }
                    IncrementCounter(counterBlock,8);//8 para comecar a incrementar após nonce
                }
                return(cypherBytes,nonce);
            }
        }
    }
    private static void IncrementCounter(byte[] counterBlock,int offset){
        for(int i = counterBlock.Length - 1;i >= offset ;i--){
            
            counterBlock[i]++;

            ////incrementa o byte na posicao ate chegar ao maximo(255) depois incrementa o em seguida
            /// mesmo sistema que (0,1,10,11,100,...)
            if (counterBlock[i] != 0)
                break;
        }
    } 

}
