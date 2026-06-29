# Testing Guide — Enterprise RAG Assistant API

This document covers how to manually verify the API's auth and (later) retrieval/agent behavior during development. It is separate from `README.md` (the build log) — this file is operational ("how to test"), the other is architectural ("why we built it this way").

---

## Prerequisites

- API running locally (`dotnet run` inside `EnterpriseRagApi`), note the port shown (e.g. `https://localhost:7123`)
- Postman installed
- Your App Registration's **Application (client) ID** and **Directory (tenant) ID** (from Entra ID → App registrations → `EnterpriseRagAssistant-API` → Overview)

---

## One-time setup: register a redirect URI for Postman

Our API itself never needed a redirect URI (it only validates incoming tokens — see `README.md`, Step 1). Postman, however, acts as the *client* performing interactive sign-in, and Entra ID requires every client to have a registered redirect URI before it will issue tokens to it.

1. Azure Portal → **Microsoft Entra ID → App registrations** → `EnterpriseRagAssistant-API` → **Authentication** (left nav)
2. **+ Add a platform** → **Single-page application**
3. Redirect URI: `https://oauth.pstmn.io/v1/callback`
4. Save

This only needs to be done once per environment.

---

## One-time setup: assign yourself an App Role

Token validity and role assignment are two separate things — a token can be perfectly valid and still carry no `roles` claim if the signed-in user hasn't been assigned a role.

1. Azure Portal → **Microsoft Entra ID → Enterprise Applications** → `EnterpriseRagAssistant-API`
2. **Users and groups** (left nav) → **+ Add user/group**
3. Assign yourself to `RagAssistant.User` (and `RagAssistant.Approver` if testing approval flows later, from Step 6 onward)

---

## Postman OAuth 2.0 configuration

Create a request, then under the **Authorization** tab, set **Type: OAuth 2.0**, and configure a new token with:

| Field | Value |
|---|---|
| Grant Type | Authorization Code (With PKCE) |
| Callback URL | `https://oauth.pstmn.io/v1/callback` |
| Auth URL | `https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/authorize` |
| Access Token URL | `https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/token` |
| Client ID | App Registration's Application (client) ID |
| Client Secret | (leave blank — PKCE doesn't require one) |
| Scope | `api://{CLIENT_ID}/access_as_user` |
| Client Authentication | Send as Basic Auth Header (default) |

Replace `{TENANT_ID}` and `{CLIENT_ID}` with your actual values.

Click **Get New Access Token** → sign in via the browser popup → **Use Token**.

**Token reuse note:** Entra ID access tokens are short-lived (typically ~1 hour). If requests start failing with `401` after working previously, re-fetch the token rather than assuming the API broke.

---

## Test 1 — Auth wiring (`/api/diagnostics/whoami`)

**Without a token:**
- Request: `GET {base_url}/api/diagnostics/whoami`, Authorization tab set to **No Auth**
- Expected: `401 Unauthorized`
- This confirms the endpoint is actually protected (not silently open) — a passing "with token" test means nothing if this case isn't also verified.

**With a valid token:**
- Same request, Authorization tab using the OAuth 2.0 config above
- Expected: `200 OK`, JSON body containing your name and a `roles` array
- If `roles` is empty: token is valid, but the App Role assignment step above hasn't been done (or hasn't propagated yet — can take a minute or two)

---

## Test 2 — Retrieval service (once exposed via an endpoint, Step 3c onward)

To be filled in once the retrieval/answer endpoint exists. Will cover:
- A real in-domain question per department (reuse the three from Step 2g console testing, for consistency)
- An out-of-corpus question (e.g. the "decimal representation of pi" case) — expected to return the explicit "no relevant information found" response once the similarity threshold guardrail is live, not a hallucinated answer

---

## Known gotchas

| Symptom | Likely cause |
|---|---|
| `AADSTS` error during Postman login | Redirect URI not registered, or registered under the wrong platform type (must be SPA, not Web, for PKCE flow in some tenant configs) |
| `401` even with a token that "looks" valid in Postman | Token's `aud` (audience) claim doesn't match the API's expected audience — check `Scope` value matches exactly `api://{CLIENT_ID}/access_as_user` |
| `200 OK` but empty `roles` claim | User not assigned to an App Role under Enterprise Applications → Users and groups |
| Works, then suddenly `401` after a while | Token expired — re-run "Get New Access Token" in Postman |
