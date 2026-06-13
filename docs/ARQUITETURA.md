# UltraTask — Documento de Arquitetura

**Versão:** 2.0.0  
**Plataforma:** Windows 10/11  
**Stack:** C# / .NET 10 / WPF  
**Atualizado:** 2026-06-13

---

## 1. Visão Geral

UltraTask é um gerenciador de tarefas desktop desenvolvido em WPF com foco em alta densidade visual e produtividade. A arquitetura segue o padrão **MVVM** (Model–View–ViewModel), usando o Community Toolkit.Mvvm para geração de código (propriedades observáveis e comandos).

O app opera sobre um único arquivo JSON por vez (uma lista = um arquivo) e persiste as preferências do usuário em um `settings.json` separado.

---

## 2. Estrutura de Pastas

```
UltraTask/
├── Models/           Entidades de dados — mapeadas diretamente para JSON
├── ViewModels/       Camada de apresentação — estado e lógica de UI
├── Views/            Janelas WPF (MainWindow + auxiliares)
├── Controls/         UserControls reutilizáveis
├── Services/         Serviços sem dependência de UI (persistence, theme, filter…)
├── Converters/       IValueConverter para bindings XAML
├── Themes/           Paleta de cores e estilos globais
└── Resources/        Ícone do app
```

---

## 3. Camada de Modelos (Models/)

Os modelos são POCOs serializados diretamente para JSON via `System.Text.Json`. Nenhum deles depende de WPF ou de outros modelos da aplicação.

### TaskItem
Unidade atômica da lista. Pode ser tarefa ou seção.

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | string (GUID) | Identificador único |
| `Title` | string | Título do item |
| `Completed` | bool | Item concluído (reservado — sem uso na UI atual) |
| `Important` | bool | Marcado como importante |
| `DueDate` | string | Data no formato `yyyy-MM-dd` |
| `Notes` | string | Nota de texto simples (legado) |
| `NotesRich` | NotesRich? | Nota com formatação HTML |
| `Contact` | string | Nome do contato responsável |
| `Assignee` | string | Nome do designado |
| `Tags` | List\<string\> | Tags aplicadas ao item |
| `ItemType` | string | `"task"` ou `"section"` |
| `SectionColor` | string | Cor hex da seção |

`IsSection` é uma propriedade calculada (não serializada).

### TaskFile
Documento completo — representa um arquivo aberto.

| Campo | Tipo | Descrição |
|---|---|---|
| `Title` | string | Nome da lista |
| `Tasks` | List\<TaskItem\> | Itens em ordem manual |
| `TaskRowOrder` | List\<string\> | Ordem dos tokens na linha |
| `RoleConfig` | RoleConfig | Configuração visual de Contact/Assignee |
| `TagCatalog` | List\<TagEntry\> | Catálogo de tags com cor e tamanho |
| `LinkCatalog` | List\<LinkRule\> | Regras de link automático por regex |

### AppSettings
Preferências locais do usuário. Persistidas em `settings.json` ao lado do executável — não viajam com o arquivo de tarefas.

| Campo | Tipo | Descrição |
|---|---|---|
| `TaskFilePath` | string | Caminho absoluto do arquivo ativo |
| `Theme` | string | `"dark"` ou `"light"` |
| `LayoutMode` | string | `"compact"` / `"normal"` / `"extended"` |
| `WindowWidth/Height/Left/Top` | double | Geometria da janela |
| `WindowState` | string | `"Normal"` / `"Maximized"` |
| `TitlebarFormat` | string | `"app"` / `"list"` / `"app-list"` / `"list-app"` |

### TagEntry
Entrada do catálogo de tags.

| Campo | Tipo | Descrição |
|---|---|---|
| `Name` | string | Identificador único da tag |
| `Color` | string | Cor hex |
| `Order` | int | Índice de ordenação |
| `Size` | string | Largura fixa em caracteres (vazio = auto) |

### RoleConfig / RoleEntry
Configuração dos dois papéis configuráveis (Contact e Assignee).

| Campo em RoleEntry | Tipo | Descrição |
|---|---|---|
| `Color` | string | Cor hex do chip |
| `Style` | string | `"tag"` (cantos retos) ou `"balloon"` (pill) |
| `Prefix` | string | Prefixo textual (ex: `@`, `→`) |
| `Font` | string | Nome da fonte |
| `Size` | string | Largura fixa em chars |

### LinkRule
Regra de link automático no título da tarefa.

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | string (GUID) | Identificador |
| `Name` | string | Nome descritivo da regra |
| `Pattern` | string | Regex (suporta grupos nomeados e numéricos) |
| `UrlTemplate` | string | Template com `{match}`, `{1}`, `{nome}` |
| `Order` | int | Prioridade de aplicação |

---

## 4. Camada de Serviços (Services/)

Serviços são classes estáticas sem dependência de UI. São testáveis independentemente.

### PersistenceService
Responsável por toda I/O de arquivos JSON.

- `LoadSettings()` / `SaveSettings(AppSettings)` — preferências locais
- `LoadTaskFile(path)` / `SaveTaskFile(file, path)` — arquivo de tarefas
- `CreateNewTaskFile(path, title)` — novo arquivo vazio
- Migração automática de campos legados (ex: tags em formato antigo)
- Deserializador customizado `NotesRichConverter` para suporte a dois formatos de `notes_rich`

### ThemeService
Troca de tema em runtime sem reinicialização.

- `Apply("dark" | "light")` — substitui os brushes em `Application.Resources`
- Todos os brushes são Frozen (thread-safe) após aplicação
- A UI reage automaticamente via `DynamicResource`

### LayoutService
Aplica presets de tamanhos conforme modo de layout.

| Recurso | compact | normal | extended |
|---|---|---|---|
| `RowHeight` | 26 | 34 | 46 |
| `SectionHeight` | 36 | 44 | 54 |
| `FontSizeBase` | 12 | 13 | 15 |
| `FontSizeSmall` | 11 | 12 | 14 |
| `TokenSpacing` | 3 | 5 | 8 |

### FilterService
Lógica de filtro sem dependência de UI. Recebe um `FilterCriteria` e retorna bool.

- Comparação case-insensitive
- Seções sempre passam (excluídas da filtragem)
- Critérios combináveis: Tag, Contact, Assignee, ImportantOnly

### LinkResolverService
Resolve links automáticos nos títulos das tarefas.

- Recebe o título e o catálogo de regras
- Retorna lista de `Segment(Text, Url?)` — texto puro intercalado com links
- Aplica regras em ordem (prioridade), sem sobreposição de matches
- Suporta `{match}`, `{1}`, `{nome}` como placeholders na URL

### HtmlFlowConverter
Conversão bidirecional entre HTML simples e `FlowDocument` do WPF.

- Tags suportadas: `<b>`, `<i>`, `<u>`, `<s>`, `<span style="color:…">`, `<span style="background:…">`
- `ToFlowDocument(html)` — renderiza para edição no RichTextBox
- `ToHtml(doc)` — serializa de volta para armazenamento

---

## 5. Camada de ViewModels (ViewModels/)

### MainViewModel
Orquestrador principal. Herda `ObservableObject` do Community Toolkit.

**Responsabilidades:**
- Gerenciar `AllItems` (coleção completa) e `FilteredItems` (vista com filtro)
- Expor dados do arquivo ativo para binding (tags disponíveis, contacts, assignees, etc.)
- Comandos de criação, exclusão, duplicação e movimentação de itens
- Modo lote: ativar, selecionar, executar operações em lote
- Persistência com debounce de 500 ms

**Coleções:**
- `AllItems` — `ObservableCollection<TaskItemViewModel>` mantém a ordem manual
- `FilteredItems` — `ICollectionView` sobre `AllItems`, filtrado por `FilterService`

**Filtros observáveis:**
- `FilterTag`, `FilterContact`, `FilterAssignee`, `FilterImportantOnly`
- Qualquer mudança dispara `FilteredItems.Refresh()`

**Modo lote:**
- `BatchModeActive` — alterna visibilidade dos checkboxes nas linhas
- `SelectedItems` — `IEnumerable<TaskItemViewModel>` onde `IsSelected && !IsSection`
- `BatchSelectedCount` — atualizado via `PropertyChanged` dos itens
- Métodos: `BatchAddTag`, `BatchRemoveTag`, `BatchSetContact`, `BatchSetAssignee`, `BatchSetImportant`, `BatchDelete`

### TaskItemViewModel
Wrapper observável de `TaskItem`. Uma instância por item na lista.

- Propriedades observáveis espelham os campos do modelo via partial methods (`OnTitleChanged` → `Model.Title = value`)
- `IsSelected` — estado de seleção em lote (não persistido)
- `SyncFromModel()` — popula todas as observáveis a partir do modelo (usado após reload)

---

## 6. Camada de Controles (Controls/)

### TaskRowControl
UserControl que representa uma linha na lista. Reconstrói-se via `Rebuild()` quando qualquer DP relevante muda.

**DependencyProperties:** `Item`, `TaskRowOrder`, `RoleConfig`, `TagCatalog`, `LinkCatalog`, `BatchModeActive`, `RoleConfigVersion`, `LayoutVersion`

**Eventos:** `DeleteRequested`, `DuplicateRequested`, `ItemChanged`, `DragStarted`, `FilterByTag`, `FilterByContact`, `FilterByAssignee`

**Tokens suportados em TaskRowOrder:**
| Token | Descrição |
|---|---|
| `tags` | Chips de tag com cor |
| `assignee` | Chip de designado |
| `contact` | Chip de contato |
| `title` | Título editável inline (com links automáticos) |
| `notes` | Badge de notas (ícone circular) |
| `date` | Data de vencimento |
| `spacer` | Espaço flexível |

**Seleção em lote:**
- Checkbox controlado por eventos `Checked`/`Unchecked` (não por binding XAML)
- `OnItemPropertyChanged` sincroniza o checkbox quando `IsSelected` muda por fora
- Fundo de linha selecionada aplicado via `SetResourceReference("BgRowSelected")` em code-behind
- Hover respeita o estado de seleção (não sobrescreve)

### InlineTextEditor
Alterna entre `TextBlock` (visualização) e `TextBox` (edição) com duplo-clique. Suporta inlines customizados (ex: hiperlinks de link automático).

### TagChipControl / RoleChipControl
Chips visuais para tags e papéis. Calculam contraste automático (texto preto/branco) baseado na luminância da cor de fundo.

---

## 7. Camada de Views (Views/)

Todas as janelas auxiliares têm:
- `ShowInTaskbar="False"` — não aparecem na barra de tarefas
- `WindowStartupLocation="CenterOwner"` — centradas na janela pai
- `ResizeMode="CanResize"` — redimensionáveis

| Janela | Parâmetros do construtor | Função |
|---|---|---|
| `MainWindow` | — | Janela principal |
| `NotesWindow` | `string? html, Action<string> onSave` | Editor de notas |
| `TagManagerWindow` | `List<TagEntry> tags, Action onChanged` | Catálogo de tags |
| `RoleManagerWindow` | `RoleConfig config, Action onChanged` | Config de papéis |
| `LinkManagerWindow` | `List<LinkRule> rules, Action onChanged` | Regras de link |
| `FilePropertiesWindow` | `TaskFile file, Action onSave` | Ordem de tokens |
| `AppSettingsWindow` | `AppSettings, Action<string>, Action` | Preferências do app |
| `DatePickerWindow` | `DateOnly? current` | Seletor de data |
| `ColorPickerDialog` | `string currentHex` | Seletor de cor |
| `AboutWindow` | `string buildStamp` | Sobre / versão |

---

## 8. Temas (Themes/)

### Colors.xaml
Define a paleta de recursos acessados via `DynamicResource`. Gerenciada em runtime pelo `ThemeService`.

**Grupos de recursos:**
- **Fundos:** `BgDeep`, `BgPanel`, `BgRow`, `BgRowHover`, `BgRowSelected`, `BgSection`
- **Textos:** `TextPrimary`, `TextSecondary`, `TextMuted`
- **Bordas:** `BorderSubtle`
- **Ações:** `Accent`, `Danger`, `ImportantEar`
- **Sidebar:** `BgSidebar`, `SidebarFg`, `SidebarHover`
- **Header:** `BgHeader`, `HeaderFg`
- **Layout (double/Thickness):** `RowHeight`, `SectionHeight`, `FontSizeBase`, `FontSizeSmall`, `TokenSpacing`, `RowSpacing`, `ChipRadius`, `BalloonRadius`

### Styles.xaml
Estilos globais WPF referenciados por nome (StaticResource).

**Estilos de botão:** `AccentButtonStyle`, `NeutralButtonStyle`, `DangerButtonStyle`, `SidebarButtonStyle`, `SidebarToggleStyle`

**Outros:** `FilterComboStyle`, `RowDeleteButtonStyle`, `CheckBox` (global com ControlTemplate customizado)

---

## 9. Fluxo de Dados

```
Disco (JSON)
    ↓ PersistenceService.LoadTaskFile()
TaskFile (Model)
    ↓ MainViewModel.ApplyFile()
ObservableCollection<TaskItemViewModel> (AllItems)
    ↓ ICollectionView (FilteredItems)
ItemsControl (XAML)
    ↓ DataTemplate
TaskRowControl (por item)
    ↓ Eventos (DeleteRequested, ItemChanged…)
MainWindow.xaml.cs
    ↓ MainViewModel.ScheduleSave()
PersistenceService.SaveTaskFile()
    ↓
Disco (JSON)
```

---

## 10. Persistência

### Debounce de Salvamento
Edições na UI disparam `ScheduleSave()` que agenda um `System.Timers.Timer` de 500 ms. Edições rápidas e consecutivas resetam o timer (coalescing). `SaveNow()` força salvamento imediato (usado ao fechar).

### Localização dos arquivos
- `settings.json` — mesmo diretório do executável
- Arquivo de tarefas — caminho arbitrário, definido pelo usuário

### Migração
O `PersistenceService` detecta e migra campos ausentes de versões antigas do formato JSON silenciosamente.

---

## 11. Atalhos de Teclado

| Atalho | Ação |
|---|---|
| `Ctrl+N` | Nova tarefa |
| `Ctrl+R` | Recarregar arquivo |
| `Ctrl+S` | Salvar agora |
| `F5` | Layout compact |
| `F6` | Layout normal |
| `F7` | Layout extended |
| `F11` | Tema claro |
| `F12` | Tema escuro |
| `Ctrl+Scroll` | Ciclar layout |

F11/F12 funcionam em qualquer janela aberta (registrados via `EventManager.RegisterClassHandler` em `App.xaml.cs`).

---

## 12. Publicação

O projeto inclui `publish.ps1` que gera um executável single-file self-contained para win-x64:

```
dotnet publish -r win-x64 -c Release
    --self-contained true
    -p:PublishSingleFile=true
    -p:EnableCompressionInSingleFile=true
    -p:PublishReadyToRun=true
```
