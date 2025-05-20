using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PlaceholderTextApp.Services
{
    public interface IDocumentHmacSigningService
    {
        object GerarJsonAssinaturasHmac(string texto);
    }

    public class DocumentHmacSigningService : IDocumentHmacSigningService
    {
        // Idealmente esta chave deve vir de uma configuração segura
        private readonly byte[] _hmacKey = Encoding.UTF8.GetBytes("chave-secreta-supersegura");

        public object GerarJsonAssinaturasHmac(string texto)
        {
            var regex = new Regex(@"\[([^\]]*)\]");
            var matches = regex.Matches(texto);

            var placeholders = new Dictionary<string, object>();
            var fixedOptionValues = new List<List<string>>();
            var fixedPlaceholders = new List<string>();

            foreach (Match match in matches)
            {
                string conteudo = match.Groups[1].Value;

                if (conteudo.Contains(":"))
                {
                    var partes = conteudo.Split(":", 2);
                    string nome = partes[0].Trim();
                    var opcoes = partes[1]
                        .Split(',')
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrWhiteSpace(o))
                        .ToList();

                    if (opcoes.Count > 0)
                    {
                        placeholders[nome] = opcoes;
                        fixedOptionValues.Add(opcoes);
                        fixedPlaceholders.Add(match.Value);
                    }
                    else
                    {
                        placeholders[nome] = "Free Text";
                    }
                }
                else
                {
                    string nome = conteudo.Trim();
                    placeholders[nome] = "Free Text";
                }
            }

            var combinacoes = GerarCombinacoes(fixedOptionValues);
            var combinacoesAssinadas = new List<Dictionary<string, string>>();

            using var hmac = new HMACSHA256(_hmacKey);

            foreach (var combinacao in combinacoes)
            {
                string textoFinal = texto;
                for (int i = 0; i < fixedPlaceholders.Count; i++)
                {
                    textoFinal = textoFinal.Replace(fixedPlaceholders[i], combinacao[i]);
                }

                byte[] dados = Encoding.UTF8.GetBytes(textoFinal);
                byte[] assinatura = hmac.ComputeHash(dados);
                string assinaturaBase64 = Convert.ToBase64String(assinatura);

                combinacoesAssinadas.Add(new Dictionary<string, string>
                {
                    { "text", textoFinal },
                    { "signature", assinaturaBase64 }
                });
            }

            return new
            {
                original = texto,
                placeholders = placeholders,
                signed_combinations = combinacoesAssinadas
            };
        }

        private List<List<string>> GerarCombinacoes(List<List<string>> listas)
        {
            var resultado = new List<List<string>> { new List<string>() };

            foreach (var lista in listas)
            {
                var temp = new List<List<string>>();
                foreach (var prefixo in resultado)
                {
                    foreach (var item in lista)
                    {
                        var nova = new List<string>(prefixo) { item };
                        temp.Add(nova);
                    }
                }
                resultado = temp;
            }

            return resultado;
        }
    }
}
