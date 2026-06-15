# UltraTask — Documento Funcional

**Versão:** 2.0.0  
**Plataforma:** Windows 10/11  
**Atualizado:** 2026-06-14

---

## 1. Visão Geral do Produto

UltraTask é um gerenciador de tarefas desktop para Windows com foco em **alta densidade visual** e **organização avançada**. Uma sessão de trabalho abre um único arquivo de lista (`.json`), que pode conter tarefas e seções organizadas livremente.

O app não requer conta, serviço externo ou conexão de rede. Todos os dados ficam em arquivos JSON controlados pelo usuário.

---

## 2. Conceitos Fundamentais

### Arquivo de lista
A unidade de trabalho do UltraTask é um arquivo `.json`. Cada arquivo contém uma lista de tarefas completa — com suas configurações de tags, papéis, links e ordem de exibição. Múltiplos arquivos podem ser abertos em instâncias separadas do app.

### Tarefas
Uma tarefa é um item de linha com título editável e campos opcionais: tags, designado, contato, data de vencimento, nota e marcação de importância. A posição na lista é manual (drag-and-drop).

### Seções
Uma seção é um item especial que serve como separador visual e agrupador semântico. Seções têm título colorido, podem ser editadas e movidas como qualquer item, mas não possuem data, nota ou tags.

### Tags
Tags são rótulos coloridos associados a tarefas. Cada arquivo tem seu próprio catálogo de tags (nome, cor, largura opcional, estilo e fonte). Uma tarefa pode ter múltiplas tags.

### Papéis (Contact, Assignee e Pendência)
Três campos de texto livre exibidos como chips visuais na linha da tarefa. A aparência do chip (cor, estilo, prefixo, fonte, tamanho) é configurável por papel para toda a lista. O campo **Pendência** serve para registrar rapidamente o que está aguardando retorno de outra pessoa naquela tarefa.

### Links automáticos
Padrões regex configurados pelo usuário que transformam partes do título em hiperlinks clicáveis. Útil para tickets de sistema (ex: `[OS-1234]` → link para o sistema de OS).

---

## 3. Interface Principal

### Layout geral
A janela principal é dividida em três áreas:

```
┌──────────────────────────────────────────────────────────┐
│  Cabeçalho: ícone + título da lista + timestamp de build │
├──────┬───────────────────────────────────────────────────┤
│      │  Barra de filtros / Barra de operações em lote    │
│  S   ├───────────────────────────────────────────────────┤
│  i   │                                                   │
│  d   │           Lista de tarefas                        │
│  e   │                                                   │
│  b   │                                                   │
│  a   ├───────────────────────────────────────────────────┤
│  r   │  Status: N tarefas · caminho do arquivo           │
└──────┴───────────────────────────────────────────────────┘
```

### Sidebar (barra lateral)
Coluna de 60px com botões de ação rápida, organizados em grupos separados por divisores:

| Ícone | Ação |
|---|---|
| + | Nova tarefa |
| ⊕ | Novo link (gerenciar links) |
| ≡ | Adicionar seção |
| ☰ | Modo operações em lote |
| ↺ | Recarregar arquivo (Ctrl+R) |
| 🏷 | Gerenciar tags |
| 👤 | Gerenciar papéis |
| 🔗 | Gerenciar links |
| 📄 | Propriedades do arquivo |
| ⚙ | Configurações do app |
| ℹ | Sobre o UltraTask |

### Barra de filtros
Aparece na área de conteúdo quando o modo lote está desativado. Filtros disponíveis:

- **Tag** — combo editável com tags em uso
- **Designado** — combo editável com designados em uso
- **Contato** — combo editável com contatos em uso
- **Importantes** — checkbox para mostrar apenas marcados como importantes
- **X Limpar** — aparece quando há filtro ativo; remove todos os filtros

Filtros são cumulativos. Seções sempre são exibidas independente dos filtros.

### Linha de tarefa
Cada tarefa é exibida em uma linha de altura configurável. Os campos exibidos e sua ordem são configuráveis por arquivo em **Propriedades do arquivo**. Campos disponíveis:

| Token | Exibição |
|---|---|
| `tags` | Chips coloridos das tags |
| `assignee` | Chip do designado |
| `contact` | Chip do contato |
| `title` | Título editável inline |
| `pendencia` | Chip de pendência (texto livre) |
| `notes` | Ícone circular (indicador de nota existente) |
| `date` | Data de vencimento |
| `spacer` | Espaço flexível — tudo após o espaço fica alinhado à direita |

**Orelha de importância:** borda colorida de 4px na extremidade esquerda da linha, visível quando a tarefa está marcada como importante. Clicável para alternar.

**Botão excluir:** aparece na extremidade direita ao passar o mouse.

**Grip de drag-and-drop:** ícone ⠿ à esquerda da linha, visível no hover. Arrastar para reordenar.

### Linha de seção
Altura maior, fundo diferenciado, título colorido em fonte maior e sem borda inferior. Seções aparecem como agrupadores visuais na lista. Botão excluir fica sempre visível na seção.

### Barra de status
Rodapé da janela com:
- Contagem de tarefas (ex: "19 tarefas")
- Caminho completo do arquivo aberto

---

## 4. Edição de Tarefas

### Editar título
Duplo-clique no título abre o modo de edição inline. Enter confirma, Escape cancela.

### Editar data de vencimento
Clique na data (ou no espaço de data se vazia) abre o seletor de data modal. Inclui botão "Limpar" para remover a data. Data vencida é exibida em vermelho e negrito.

### Editar tags
Clique direito na área de tags (ou menu de contexto > Alterar tags) abre o popup de seleção de tags do catálogo. Múltipla seleção.

### Editar designado / contato
Clique no chip de designado ou contato (ou menu de contexto) abre edição inline com lista de valores já usados na lista.

### Marcar como importante
Clique na orelha colorida na lateral esquerda alterna o estado de importância. Também disponível no menu de contexto.

### Editar nota
Menu de contexto > Notas abre a janela de notas.

---

## 5. Menu de Contexto de Tarefas

Acessado com clique direito em qualquer tarefa:

| Item | Ação |
|---|---|
| ✏ Editar título | Ativa edição inline do título |
| ⭐ Importante / Não importante | Alterna marcação |
| 🏷 Alterar tags | Popup de seleção de tags |
| 👤 Definir designado | Edição inline de designado |
| 📞 Definir contato | Edição inline de contato |
| ⚠ Definir pendência | Edição inline do texto de pendência |
| 📅 Definir data | Abre seletor de data |
| 📝 Notas | Abre janela de notas |
| ⧉ Duplicar | Cria cópia com novo ID |
| 🗑 Excluir | Remove com confirmação |

### Menu de contexto de seções

| Item | Ação |
|---|---|
| ✏ Editar | Ativa edição inline do título |
| 🎨 Alterar cor | Seletor de cor |
| ⧉ Duplicar | Cria cópia |
| 🗑 Excluir | Remove com confirmação |

---

## 6. Janela de Notas

Editor de texto com formatação. Abre como janela modal com as seguintes funcionalidades:

### Toolbar
| Botão | Atalho | Ação |
|---|---|---|
| **B** | Ctrl+B | Negrito |
| *I* | Ctrl+I | Itálico |
| <u>U</u> | Ctrl+U | Sublinhado |
| ~~S~~ | — | Tachado |
| A (cor) | — | Cor do texto (abre seletor) |
| ▨ (fundo) | — | Cor de fundo do texto |
| Eraser | — | Remover toda a formatação da seleção |
| ☐ | — | Inserir item de checklist |

### Checklist interativo
- Clique em ☐ ou ☒ no editor alterna o estado diretamente
- O cursor muda para mãozinha ao passar sobre um checkbox
- Checkboxes são inseridos em tamanho maior (FontSize 16)

### Ações do rodapé
- **Salvar** — persiste o HTML e fecha
- **Cancelar** / Escape — fecha sem salvar
- **Limpar** — apaga todo o conteúdo (com confirmação)

---

## 7. Operações em Lote

Ativadas pelo botão de modo lote na sidebar (ícone ☰). Quando ativo:

- A barra de filtros é substituída pela **barra de operações em lote**
- Cada linha de tarefa exibe uma checkbox à esquerda
- Seções não podem ser selecionadas

### Barra de operações em lote

**À esquerda:**
- Contador de tarefas selecionadas (ex: "5 selecionada(s)")
- Botão **Todas** — seleciona todas as tarefas visíveis
- Botão **Nenhuma** — desmarca todas

**À direita:**
| Botão | Ação |
|---|---|
| + Tag | Menu com catálogo de tags → adiciona a selecionadas |
| − Tag | Menu com tags das selecionadas → remove das selecionadas |
| Designado | Menu com designados em uso + "Digitar..." + "Limpar" |
| Contato | Menu com contatos em uso + "Digitar..." + "Limpar" |
| Importante | Marca todas as selecionadas como importantes |
| Não import. | Desmarca importância de todas as selecionadas |
| Excluir | Exclui todas as selecionadas (com confirmação) |

Ao desativar o modo lote, todas as seleções são limpas.

### Feedback visual
Linhas selecionadas ficam com fundo diferenciado (`BgRowSelected`). O hover não sobrescreve o fundo de uma linha já selecionada.

---

## 8. Filtros

Os filtros são cumulativos. Todos os campos são combos editáveis — é possível digitar um valor não listado.

- **Tag:** mostra tarefas que contenham a tag especificada
- **Designado:** mostra tarefas com o designado especificado
- **Contato:** mostra tarefas com o contato especificado
- **Importantes:** quando marcado, mostra apenas itens com `Important = true`

Seções sempre aparecem na lista independentemente dos filtros ativos.

O botão **X Limpar** remove todos os filtros de uma vez. Ele só aparece quando há pelo menos um filtro ativo.

---

## 9. Drag-and-Drop

Tarefas e seções podem ser reordenadas arrastando pelo grip (ícone ⠿) que aparece ao passar o mouse sobre a linha.

- Uma **linha azul** indica a posição de destino durante o arrasto
- A posição é calculada pela metade vertical do item de destino
- Seções e tarefas podem ser misturadas livremente (sem restrição de posição)
- A ordem é imediatamente persistida ao soltar

---

## 10. Links Automáticos

Padrões de texto no título podem ser convertidos em hiperlinks automaticamente. Cada regra tem:
- **Nome** — identificação da regra
- **Pattern** — expressão regular (com suporte a grupos nomeados e numéricos)
- **URL Template** — template com placeholders `{match}`, `{1}`, `{nome}`
- **Prioridade** — ordem de aplicação (regras de maior prioridade têm precedência)

Clique no link abre no navegador padrão. Hover exibe a URL em tooltip.

---

## 11. Temas

O app suporta dois temas com troca ao vivo (sem reiniciar):

- **Escuro** (padrão) — fundo azul petróleo escuro, textos claros
- **Claro** — fundo acinzentado claro, textos escuros

Formas de alternar:
- `F11` → tema claro
- `F12` → tema escuro
- Menu Configurações do app → campo Tema

Os atalhos F11/F12 funcionam com qualquer janela do app em foco.

---

## 12. Layouts

Três presets de densidade visual. Alteram altura das linhas, tamanho de fonte e espaçamento dos tokens:

| Layout | Densidade | Uso sugerido |
|---|---|---|
| **Compact** | Alta | Muitas tarefas na tela, menos detalhe visual |
| **Normal** | Média | Equilíbrio entre densidade e legibilidade |
| **Extended** | Baixa | Mais espaço por linha, fonte maior |

Formas de alternar:
- `F5` → compact / `F6` → normal / `F7` → extended
- `Ctrl+Scroll` para cima → mais compacto / para baixo → mais expandido
- Menu Configurações do app → campo Layout

---

## 13. Configurações do App

Janela de preferências com os seguintes campos:

| Campo | Opções |
|---|---|
| Arquivo de tarefas | Selecionador de arquivo |
| Tema | Escuro / Claro |
| Layout | Compact / Normal / Extended |
| Formato da barra de título | Nome do app / Nome da lista / Nome do app — Nome da lista / Nome da lista — Nome do app |

---

## 14. Propriedades do Arquivo

Configura a **ordem e visibilidade dos campos** na linha de tarefa, por arquivo. As configurações ficam salvas no próprio arquivo `.json` e viajam junto com a lista.

Campos disponíveis para ativar/desativar e reordenar:
`tags`, `assignee`, `contact`, `title`, `pendencia`, `notes`, `spacer`, `date`

---

## 15. Gerenciar Tags

Catálogo de tags do arquivo corrente:

- **Criar** nova tag com nome, estilo, fonte, cor (seletor nativo do Windows) e tamanho fixo opcional (em caracteres)
- **Editar** cor, estilo, fonte e tamanho de uma tag existente
- **Reordenar** as tags (afeta a ordem de exibição nos chips)
- **Excluir** tag (remove do catálogo; tarefas que usavam a tag mantêm a referência textual)

### Estilos de chip de tag

| Estilo | Aparência |
|---|---|
| `rótulo` | Cantos levemente arredondados (padrão) |
| `balão` | Pill totalmente arredondado |
| `faixa` | Sem padding vertical, ocupa toda a altura da linha |

---

## 16. Gerenciar Papéis

Configura a aparência dos chips de **Designado**, **Contato** e **Pendência**:

| Campo | Descrição |
|---|---|
| Cor | Cor de fundo do chip (abre seletor nativo do Windows) |
| Estilo | `rótulo`, `balão` ou `faixa` |
| Prefixo | Texto antes do valor (ex: `@`, `⚠`) |
| Fonte | Nome da fonte do chip |
| Tamanho | Largura fixa em caracteres (vazio = automático) |

Preview ao vivo enquanto edita. Clicar em um chip do preview navega diretamente para a aba correspondente.

---

## 17. Gerenciar Links

Catálogo de regras de link automático do arquivo corrente:

- **Criar** nova regra com nome, regex e template de URL
- **Editar** padrão e template existentes
- **Reordenar** regras (prioridade de aplicação)
- **Excluir** regra

**Exemplo de regra:**
- Nome: `OS do sistema`
- Pattern: `\[OS-(\d+)\]`
- URL Template: `https://sistema.interno/os/{1}`

---

## 18. Janela Sobre

Exibe:
- Logo Ultrasoft
- Nome e versão do app (v2.0.0)
- Stack tecnológica (C# · .NET 10 · WPF)
- Autores: Marcus Siqueira, ChatGPT "Raj" Codex, Claude "Claudinho" Code
- Timestamp de build
- Link para o repositório no GitHub
- Copyright © 2025 Ultrasoft

---

## 19. Abrir e Criar Arquivos

Na primeira execução (sem arquivo configurado), o app exibe o diálogo de abertura de arquivo. Cancelar cria automaticamente um novo arquivo em `Documentos/UltraTask.json`.

**Trocar de arquivo:** Configurações do app → campo Arquivo de tarefas.

**Recarregar do disco:** botão ↺ na sidebar ou `Ctrl+R`. Útil quando o arquivo foi editado externamente.

---

## 20. Persistência e Formato dos Dados

### Formato do arquivo de tarefas
JSON com indentação, encoding UTF-8. Compatível com edição manual em qualquer editor de texto.

### Chaves de serialização (snake_case)
```
title, tasks[], task_row_order[], role_config, tag_catalog[], link_catalog[]
```

Cada tarefa: `id, title, completed, important, due_date, notes, notes_rich.html, contact, assignee, tags[], item_type, section_color`

### Salvamento automático
Edições são salvas automaticamente com debounce de 500 ms após a última alteração. O salvamento pode ser forçado com `Ctrl+S`.

### Compatibilidade
O formato é compatível com a versão Python do UltraTask. Campos adicionados pelo app WPF são ignorados pelo app Python (e vice-versa).

---

## 21. Publicação e Distribuição

O app é distribuído como um único executável (`UltraTask.exe`) self-contained — não requer instalação do .NET no sistema do usuário.

O script `publish.ps1` na raiz do projeto gera o executável publicado em `build_out/`.

---

## 22. Limitações Conhecidas

- Uma lista por janela (múltiplas listas = múltiplas janelas)
- Não há sincronização em nuvem ou colaboração em tempo real
- Links automáticos são somente leitura (não editáveis inline)
- Notas ricas suportam um subconjunto limitado de HTML (sem tabelas, listas, imagens)
- Sem histórico de desfazer (Ctrl+Z) nas operações em lote
