# ASSINONDEQUISERES: Assinaturas Digitais para Documentos com Alternativas

## Introdução
O ato de assinatura presume que o conteúdo do documento não irá mudar após a assinatura. A assinatura digital, enquanto mecanismo criptográfico, garante a autenticidade e a integridade absoluta do conteúdo assinado. Esse caso de uso satisfaz a maioria dos cenários. 

Contudo, existem situações onde é necessário assinar um documento contendo campos em aberto (que podem mudar após a assinatura) ou com valores alternativos (onde o assinante concorda com um conjunto de opções possíveis, mas não com todas simultaneamente). Exemplos incluem:
- Um documento com uma data flexível (ex: hoje ou amanhã);
- Um contrato onde se aceita comprar um item em três cores diferentes, deixando a escolha para o departamento de compras.

## Objetivo
O objetivo deste projeto é desenvolver uma aplicação que permita assinar documentos com as características especiais descritas acima. A aplicação deve ser simples e automatizar ao máximo o processo para o usuário, incluindo a geração de chaves.

### Fluxo de Uso
1. O usuário inicia a aplicação e decide se quer gerar um par de chaves RSA ou usar um existente;
2. Se gerar novas chaves, a chave pública é salva em um arquivo e a chave privada é protegida por criptografia AES-256-CBC com uma senha fornecida pelo usuário;
3. O usuário escreve o documento a ser assinado, podendo incluir placeholders (ex: `[PLACEHOLDER1]`, `[PLACEHOLDER2]`);
4. Define como os placeholders serão tratados (conteúdo flexível ou opções múltiplas);
5. A aplicação gera uma ou mais assinaturas digitais e salva os arquivos resultantes;
6. A aplicação permite validar assinaturas, fornecendo a chave pública correspondente.

## Funcionalidades
### Funcionalidades Básicas
- Geração de par de chaves RSA e armazenamento seguro;
- Assinatura digital de documentos com placeholders flexíveis e alternativas;
- Geração de assinaturas para cada alternativa disponível no documento;
- Validação de assinaturas digitais.

### Funcionalidades Avançadas
- Implementação de um esquema eficiente para placeholders de escolha múltipla (assinatura do hash do documento com a opção selecionada);
- Suporte a HMAC-SHA256 como alternativa às assinaturas digitais;
- Permite escolher entre AES-256-CBC e AES-256-CTR para cifrar a chave privada;
- Utiliza códigos de autenticação de mensagens como algoritmo de derivação de chave (ex: HMAC-SHA256);
- Permite escolher a função de hash usada na assinatura e o tamanho das chaves RSA.

## Segurança
Durante a implementação, é importante considerar possíveis vulnerabilidades. Alguns pontos a serem analisados incluem:
- Possibilidade de ataques por substituição de placeholders não assinados;
- Segurança da chave privada armazenada;
- Integridade das alternativas escolhidas no documento final.

## Entrega do Trabalho
A entrega do trabalho é feita única e exclusivamente via Moodle até às 23:59 do dia 25/05/2025. Os nomes dos ficheiros devem seguir a especificação incluída na seção dedicada a essa entrega na plataforma. Para cada dia de atraso na entrega de qualquer elemento do trabalho (código de implementação, scripts de instalação ou outros artefatos), serão descontados 0,5 valores (aos 5).

## Sugestão de Alinhamento para a Apresentação
A defesa do trabalho deve ser acompanhada por um breve conjunto de diapositivos. A apresentação oral pelos elementos do grupo não pode demorar mais do que 10 minutos. O alinhamento sugerido é:

1. Título do trabalho e elementos do grupo;
2. Objetivos do trabalho;
3. Levantamento de requisitos de segurança;
4. Engenharia do Software;
5. Implementação;
6. Apresentação do sistema/aplicação desenvolvida (com demonstração ao vivo ou vídeo);
7. Testes efetuados;
8. Análise crítica;
9. Objetivos alcançados, conclusões e trabalho futuro;
10. Agradecimento e abertura para perguntas.


