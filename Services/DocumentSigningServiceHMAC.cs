using System.Text.RegularExpressions;

namespace OpenSign.Services
{
    /// @brief Service responsible for generating HMAC-signed document combinations.
    public class DocumentSigningServiceHmac
    {
        private readonly HmacService _hmacService;

        /// @brief Initializes the service with the HMAC calculator.
        public DocumentSigningServiceHmac()
        {
            _hmacService = new HmacService();
        }

        /// @brief Generates HMAC signatures for all combinations of placeholders in the input text.
        /// @param texto The input template text containing placeholders.
        /// @param chaveHmac The secret key used for HMAC signing.
        /// @return A JSON-like object containing the original text, resolved combinations, and signatures.
        public object GenerateHmacSignedJson(string texto, string chaveHmac)
        {
            /// @brief Use regex to extract all placeholders.
            var regex = new Regex(@"\[([^\]]*)\]");
            var matches = regex.Matches(texto);

            int maxPlaceholders = 7;
            if (matches.Count > maxPlaceholders)
                throw new Exception($"The text exceeds the limit of {maxPlaceholders} placeholders.");

            var placeholders = new Dictionary<string, object>();
            var fixedOptionValues = new List<List<string>>();
            var fixedPlaceholders = new List<string>();

            /// @brief Classify each placeholder: fixed options or free text.
            foreach (Match match in matches)
            {
                string conteudo = match.Groups[1].Value;

                if (conteudo.Contains(":"))
                {
                    /// @brief Split placeholder into name and options (e.g., [Role:Admin,User]).
                    var partes = conteudo.Split(":", 2);
                    string nome = partes[0].Trim();
                    var opcoes = partes[1]
                        .Split(',')
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrWhiteSpace(o))
                        .ToList();

                    /// @brief Enforce option limit.
                    if (opcoes.Count > 3)
                        throw new Exception($"The placeholder '{nome}' exceeds the limit of 3 options.");

                    placeholders[nome] = opcoes;

                    /// @brief Store for combination logic.
                    if (opcoes.Count > 0)
                    {
                        fixedOptionValues.Add(opcoes);
                        fixedPlaceholders.Add(match.Value);
                    }
                }
                else
                {
                    /// @brief Placeholder is free text input.
                    string nome = conteudo.Trim();
                    placeholders[nome] = "Free Text";
                }
            }

            ///@brief Generate a random 16-byte salt to derive the HMAC key
            byte[] salt = DerivationService.genSalt(16);

            ///@brief Generate all possible combinations of fixed placeholder values
            var combinacoes = GenerateCombinations(fixedOptionValues);
            var combinacoesAssinadas = new Dictionary<string, object>();

            foreach (var combinacao in combinacoes)
            {
                string textoFinal = texto;

                ///@brief Replace each placeholder in the text with the corresponding fixed value
                for (int i = 0; i < fixedPlaceholders.Count; i++)
                    textoFinal = textoFinal.Replace(fixedPlaceholders[i], combinacao[i]);

                ///@brief Remove free-text placeholders (those without fixed options)
                textoFinal = Regex.Replace(textoFinal, @"\[[^\]:\]]+\]", "");

                ///@brief Calculate HMAC for the resulting text
                var hmacHex = _hmacService.CalcularHmac(textoFinal, chaveHmac, salt);

                ///@brief Store the result in the dictionary using the HMAC as key
                combinacoesAssinadas[hmacHex] = new
                {
                    text = textoFinal,
                    hmac = hmacHex
                };
            }

            /// @brief Return full signature result including salt and algorithm info.
            return new
            {
                original = texto,
                placeholders = placeholders,
                signed_combinations = combinacoesAssinadas,
                salt = Convert.ToBase64String(salt),
                signature_algorithm = "HMAC-SHA256 (hex)"
            };
        }

        /// @brief Generates all possible combinations from a list of option lists.
        /// @param listas A list of lists, where each inner list contains options for one placeholder.
        /// @return A list of all possible option combinations (cartesian product).
        private List<List<string>> GenerateCombinations(List<List<string>> listas)
        {
            /// @brief Start with one empty combination.
            var resultado = new List<List<string>> { new List<string>() };

            /// @brief Expand combinations with each set of options.
            foreach (var lista in listas)
            {
                var temp = new List<List<string>>();

                foreach (var prefixo in resultado)
                {
                    foreach (var item in lista)
                    {
                        /// @brief Add each option to each existing prefix.
                        var nova = new List<string>(prefixo) { item };
                        temp.Add(nova);
                    }
                }

                /// @brief Replace previous result with expanded one.
                resultado = temp;
            }

            /// @brief Return the full set of combinations.
            return resultado;
        }
    }
}
