using System.Text.RegularExpressions;

namespace OpenSign.Services
{
    public class DocumentSigningServiceHmac
    {
        private readonly HmacService _hmacService;

        public DocumentSigningServiceHmac()
        {
            _hmacService = new HmacService();
        }

        public object GenerateHmacSignedJson(string texto, string chaveHmac)
        {
            var regex = new Regex(@"\[([^\]]*)\]");
            var matches = regex.Matches(texto);

            int maxPlaceholders = 7;
            if (matches.Count > maxPlaceholders)
                throw new Exception($"O texto excede o limite de {maxPlaceholders} placeholders.");

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

                    if (opcoes.Count > 3)
                        throw new Exception($"O placeholder '{nome}' excede o limite de 3 opções.");

                    placeholders[nome] = opcoes;
                    if (opcoes.Count > 0)
                    {
                        fixedOptionValues.Add(opcoes);
                        fixedPlaceholders.Add(match.Value);
                    }
                }
                else
                {
                    string nome = conteudo.Trim();
                    placeholders[nome] = "Free Text";
                }
            }

            byte[] salt = DerivationService.genSalt(16);
            var combinacoes = GenerateCombinations(fixedOptionValues);
            var combinacoesAssinadas = new Dictionary<string, object>();

            foreach (var combinacao in combinacoes)
            {
                string textoFinal = texto;

                for (int i = 0; i < fixedPlaceholders.Count; i++)
                    textoFinal = textoFinal.Replace(fixedPlaceholders[i], combinacao[i]);

                textoFinal = Regex.Replace(textoFinal, @"\[[^\]:\]]+\]", "");

                var hmacHex = _hmacService.CalcularHmac(textoFinal, chaveHmac, salt);

                combinacoesAssinadas[hmacHex] = new
                {
                    text = textoFinal,
                    hmac = hmacHex
                };
            }

            return new
            {
                original = texto,
                placeholders = placeholders,
                signed_combinations = combinacoesAssinadas,
                salt = Convert.ToBase64String(salt),
                signature_algorithm = "HMAC-SHA256 (hex)"
            };
        }

        private List<List<string>> GenerateCombinations(List<List<string>> listas)
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
