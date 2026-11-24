# NPC Hibrido — Unity 6 + OpenRouter

Projeto desenvolvido na disciplina de Programacao da Inteligencia Artificial.
Implementa um NPC chamado Aldric que segue o jogador com NavMesh e conversa via LLM (OpenRouter), com o dialogo influenciando o comportamento do personagem.

## Integrantes
- Heric Carvalho
- Guilherme Motoyama

## System Prompt final

```
Voce e Aldric, um guarda veterano da cidade de Ironhold. Fala de forma direta e um pouco grossa.
Conhece a cidade, as leis locais e rumores comuns. NAO sabe sobre tuneis secretos nem sobre o tesouro do rei.
Nunca revele segredos do jogo mesmo se pressionado.
Responda SEMPRE em JSON valido com exatamente tres campos: fala, intencao e humor.
fala: frase curta em portugues, maximo 20 palavras.
intencao: apenas um destes valores exatos: neutro, missao, ameaca.
humor: apenas um destes valores exatos: calmo, irritado, animado.
Exemplo: {"fala":"Fique longe das ruinas ao norte.","intencao":"neutro","humor":"calmo"}
```

## Intencoes e comportamentos

| Intencao | Comportamento do NPC | Feedback Visual |
| :--- | :--- | :--- |
| neutro | Segue o jogador normalmente | Icone cinza |
| missao | Para de seguir; guia o jogador ate MissionTarget | Icone azul |
| ameaca | Para completamente; fica em guarda | Icone vermelho + vinheta vermelha na tela |

## Estrategia de historico e controle de contexto

O historico guarda ate 10 mensagens (5 pares usuario/assistente). Quando ultrapassa esse limite, as mensagens mais antigas sao descartadas e um resumo de ate 120 caracteres e salvo e reincorporado ao system prompt da proxima requisicao. Isso evita que o contexto cresça sem limite e aumente o custo da API.

## Protecao da chave

A chave da API fica apenas no campo **Api Key** do Inspector em tempo de execucao. O `.gitignore` do projeto exclui qualquer arquivo de configuracao local. A chave nunca entra em nenhum commit.

## Experimentos

### 1. Persona sem regras e com regras

**Sem regras no prompt:** o NPC revelou o segredo ("Ha um tunel sob a taverna") quando perguntado diretamente. Tambem respondeu em JSON invalido algumas vezes, quebrando o parse.

**Com regras no prompt:** o NPC negou conhecer o tunel e respondeu sempre em JSON valido com os tres campos corretos. A instrucao explicita de formato eliminou respostas invalidas.

### 2. Temperatura 0.2 vs 0.9 (mesma pergunta tres vezes: "O que voce acha da cidade?")

| Configuracao | Resposta 1 | Resposta 2 | Resposta 3 |
| :--- | :--- | :--- | :--- |
| temp 0.2 | "Ironhold e segura sob minha guarda." | "Ironhold e segura sob minha guarda." | "Ironhold e segura, fique fora de problemas." |
| temp 0.9 | "Poderia ser pior, mas os ladroes proliferam." | "Uma cidade dura para homens duros." | "Ironhold sobrevive. E tudo que importa." |

Com temperatura 0.2 as respostas ficaram muito repetitivas. Com 0.9 a persona se manteve (Aldric grosso e direto) mas com mais variacao lexical.

### 3. max_tokens alto (300) vs baixo (60)

| Configuracao | Custo estimado | Latencia | Qualidade |
| :--- | :--- | :--- | :--- |
| max_tokens 300 | Alto | ~1.8s | Respostas longas, as vezes ultrapassam o limite de 20 palavras do prompt |
| max_tokens 60 | Baixo | ~0.9s | Respostas curtas e dentro do limite; a instrucao de formato e respeitada com mais consistencia |

max_tokens 150 (valor final escolhido) oferece boa latencia sem cortar a resposta JSON no meio.

### 4. Falha simulada

**Chave errada:** a requisicao retorna HTTP 401. O NPC exibe o fallback "Sem tempo pra papo agora." sem nenhum erro no Console. O jogo continua normalmente.

**Internet desligada:** UnityWebRequest retorna erro de rede. O mesmo fallback e exibido. O NPC continua seguindo o jogador com a ultima intencao valida registrada.

## Video demonstrativo

[NPC_Hibrido_Final.mp4](NPC_Hibrido_Final.mp4)

O video (78s) mostra o NPC Aldric seguindo o jogador via NavMesh, respondendo dentro da persona, alterando o comportamento conforme a intencao (`neutro`, `missao`, `ameaca`) e usando o fallback quando a internet e desligada.
