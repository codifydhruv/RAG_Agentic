# Enterprise RAG Assistant — Build Log

**Stack:** C# / .NET, Azure AI Foundry, Azure AI Search, Azure Blob Storage, Microsoft Entra ID
**Status:** Step 1 (Foundation) and Step 2 (Ingestion Pipeline) complete

---

## Mental Model

The system is four layers, each a trust boundary:

1. **Identity layer** — who is asking (Entra ID token)
2. **Knowledge layer** — what the assistant is allowed to know (Blob Storage + AI Search)
3. **Reasoning layer** — LLM that retrieves, grounds, and decides on tool calls
4. **Action layer** — tool calls gated by approval when risky

Security and audit are not a bolt-on layer — they run through all four.

---

## Step 1 — Foundation Resources

### Why this step came first
Identity and infrastructure have to exist before any API can enforce least privilege. Building the RAG pipeline first and retrofitting auth later leads to authorization checks scattered everywhere instead of designed in from the start.

### Resources provisioned (via Azure Portal)

| Resource | Name | Purpose |
|---|---|---|
| Resource Group | `rg-enterprise-rag-dev` | Blast-radius boundary; scopes IAM, cost tracking, and lets the whole dev environment be torn down in one action |
| Storage Account | `stentragdev001` | Source-of-truth store for raw documents (container: `raw-documents`). Always rebuildable index from here. |
| Azure AI Search | `srch-entrag-dev` (Basic tier) | Retrieval engine — vector + hybrid search. Holds processed/indexed chunks, not originals. |
| Azure AI Foundry project | `enterprise-rag-dev` | Hosts model deployments: `gpt-4o` (reasoning) and `text-embedding-3-large` (embedding) |
| Key Vault | `kv-entrag-dev` | Holds secrets that can't be solved via Managed Identity (e.g. future third-party tool credentials) |
| Entra ID App Registration | `EnterpriseRagAssistant-API` | Identity contract between users and the API |

### App Registration — configuration and intent

- **Account type: Single tenant** — only identities inside our own org directory can get a token. Multitenant or personal-account options were explicitly rejected; widening this later should be a deliberate, reviewed change, not a default.
- **Redirect URI: left blank** — only needed for interactive sign-in flows (browser redirect after login). Our API validates tokens it receives; it doesn't initiate sign-in itself. Relevant later if a frontend (SPA/desktop) does its own login.
- **Expose an API → Scope `access_as_user`** — answers *"can this application get a token to call the API on a user's behalf?"* — a delegated permission. App-to-API trust, not user-to-action trust.
- **App Roles: `RagAssistant.User`, `RagAssistant.Approver`** — answers *"what is this specific person allowed to do?"* This is the least-privilege hook: regular users can ask questions; only Approvers can approve risky tool calls (used starting Step 6 — human-in-the-loop).
- Users still need to be assigned to roles under **Enterprise Applications → this app → Users and groups** — the App Registration alone doesn't do this assignment.

**Key distinction carried forward:**

| Concept | Question it answers | Checked where |
|---|---|---|
| Scope (`access_as_user`) | Can this *app* get a token at all? | Token issuance (Entra ID) |
| App Role | What can this *person* do? | Inside API code, per endpoint/action |

### Anti-patterns avoided
- No shared client secret used for service-to-service calls — Managed Identity is the target pattern for production (API → Search, API → Foundry). Console-app dev tooling uses keys as an explicit, temporary, scoped trade-off.
- No multitenant App Registration "just in case" — scope tightly now, widen deliberately later if ever needed.

---

## Step 2 — Ingestion Pipeline

Built as a **standalone .NET console app** (`IngestionPipeline`), deliberately separate from the user-facing API. Ingestion is a batch process with its own lifecycle (run on-demand/scheduled); coupling it to the API's deploy cycle is an anti-pattern.

### Packages used
- `Azure.Search.Documents` (12.0.0)
- `Azure.Identity` (1.21.0)
- `Azure.Storage.Blobs`
- `Azure.AI.OpenAI` (2.1.0) — wraps the base `OpenAI` SDK; pattern is `AzureOpenAIClient.GetEmbeddingClient(deploymentName)`
- `Microsoft.Extensions.Configuration.Json`

### Pipeline stages and why each is separate

| Step | What it does | Why it's isolated |
|---|---|---|
| **2a** | Create index schema (`enterprise-docs-index`) in AI Search, as code | Schema is a contract every later step writes into. Defined as code (not portal clicks) for reproducibility across environments, audit trail via source control, and to avoid schema/code drift. Made idempotent (check-then-create) so re-running the app doesn't fail or risk re-creating a populated index. |
| **2b** | Upload source documents to Blob Storage (`raw-documents` container) | Manual upload chosen for speed at this stage. Files named with department prefix convention (see below). |
| **2c** | Extract text from each blob | Trivial for this corpus — all source files are plain Markdown (`.md`), so extraction is just reading UTF-8 text. (Would need OpenXML/PdfPig for DOCX/PDF if those file types were used.) |
| **2d** | Chunk each document's text | Paragraph-aware splitting (~500 words/chunk, ~60-word overlap) rather than naive fixed-character slicing — avoids cutting mid-sentence/mid-list, which would otherwise silently degrade retrieval quality. |
| **2e** | Generate embeddings per chunk | Calls `text-embedding-3-large` via Foundry. Batched (16 chunks/call) for efficiency at scale. Retry logic added with exponential backoff — but scoped to **transient errors only** (429/5xx); 400s fail fast since retrying a malformed request never succeeds. |
| **2f** | Upload chunks into the index | Deterministic, idempotent document IDs (`sourceFile_chunk_index`) so re-running ingestion updates rather than duplicates. Per-document upload results checked individually — a batch can partially fail without throwing, so success/failure is logged per item, not assumed from a non-error batch response. |
| **2g** | Manual retrieval test (no LLM) | Proves retrieval quality in isolation before adding an LLM on top — isolates whether a bad answer later is a retrieval problem or a prompting problem. |

### Department tagging — approach and rationale

- **Decision:** resolved from filename convention, not a maintained dictionary, not Blob metadata.
- **Convention:** `<DEPARTMENT>_description.md` — department is everything before the first underscore, case-insensitive. Known departments: `hr`, `it`, `finance`, `engineering`. Anything unmatched defaults to `general` with a logged warning (fail-safe, not fail-silent).
- **Why filename over a dictionary:** zero code changes needed to add new files going forward, as long as the naming convention is followed.
- **Why filename over Blob metadata:** filename parsing already solves the problem with code already written and verified; Blob metadata would mean manually tagging every blob individually — a new manual step with the same "easy to forget" risk class, without removing the original one. Reconsider only if filenames become unreliable (e.g. ingesting from a third-party system with arbitrary names) or multiple independent metadata dimensions are needed beyond department.
- **Tagging happens in 2f**, not 2c — it's set when constructing each chunk's index document, since `department` is a property of the source file and every chunk from that file inherits it. There's no separate "tag afterward" pass.

### Bugs hit and fixed

| Issue | Symptom | Root cause | Fix |
|---|---|---|---|
| Embedding call failing with HTTP 400 on every retry | `Embedding call failed (attempt 3): HTTP 400` | Foundry endpoint URL in config had an incorrect/extra path segment | Corrected endpoint to the bare resource URL (no path suffix) |
| Retrieval test showing `Score` but empty `SourceFile`/`Department`/`Content` | Only score populated, all document fields blank | C# `IndexedChunk` class used PascalCase properties (`SourceFile`); index schema uses camelCase (`sourceFile`). Serializer couldn't map between them on the read path. | Added explicit `[JsonPropertyName("...")]` attributes mapping each property to its camelCase index field name. Confirmed this only affects client-side (de)serialization — no impact on already-indexed documents, since the stored data and schema were correct all along. |

### Verification performed
- Indexing run reported 0 failures; chunk count from pipeline matched document count shown in the AI Search portal index view — confirmed actual stored state, not just console log output.
- Manual retrieval test run against three real, in-domain questions (one per department): each returned the correct source document as the top hit, with a clear score lead over other results, and relevant content snippets.
- Also tested one deliberately out-of-corpus question ("decimal representation of pi") — surfaced an open issue (see below), not yet fixed.

### Open issue carried into Step 3
Out-of-corpus queries do not produce a clearly low similarity score — there's a gap relative to true in-domain matches (~0.52 vs ~0.65–0.70) but no hard cliff. Without a minimum-score threshold, the LLM layer could be handed irrelevant chunks for off-topic questions and produce a confidently wrong answer instead of declining. **Plan:** introduce a similarity score threshold in the Step 3 retrieval+answer API — below threshold, return an explicit "no relevant information found" response rather than forcing an answer.

### Minor note (non-blocking)
Some chunk content snippets begin mid-sentence or mid-list-item, suggesting paragraph-based chunk boundaries occasionally split through bulleted/numbered lists. Did not affect correctness of results observed so far; worth revisiting `Chunker.cs` if wrong-document retrieval is ever seen later.

---

## What's Next — Step 3
Build the Retrieval + Grounded Answer API (.NET Web API): Entra ID token validation, retrieval with the score-threshold guardrail above, and a grounded, cited answer generated via `gpt-4o` — proving RAG works end-to-end before introducing agentic tool-calling.
