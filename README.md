# qauthgoogle — OAuth 2.0 "Sign in with Google" (ASP.NET Core / .NET 10)

A minimal, real-world example of authenticating users with their Google account using the
OAuth 2.0 **Authorization Code** flow in ASP.NET Core.

---

## Table of Contents
1. [How It Works](#how-it-works)
2. [Project Structure](#project-structure)
3. [Google Cloud Configuration (step by step)](#google-cloud-configuration-step-by-step)
4. [Application Configuration (secrets)](#application-configuration-secrets)
5. [Run the App](#run-the-app)
6. [Endpoints](#endpoints)
7. [Postman / API Details](#postman--api-details)
8. [Troubleshooting](#troubleshooting)

---

## How It Works

This app uses two authentication schemes working together:

- **Cookie** scheme — stores the signed-in session after login.
- **Google** scheme — performs the OAuth 2.0 login against Google.

### The OAuth 2.0 Authorization Code flow

```
 Browser                 ASP.NET Core App                 Google
	|                          |                             |
 1  |  GET /account/login      |                             |
	|------------------------->|                             |
	|   302 Redirect to Google (Challenge)                   |
	|<-------------------------|                             |
 2  |  GET accounts.google.com/o/oauth2/... (consent screen) |
	|------------------------------------------------------->|
	|   User signs in & consents                             |
 3  |   302 Redirect to /signin-google?code=...              |
	|<-------------------------------------------------------|
 4  |  GET /signin-google?code=...                           |
	|------------------------->|                             |
	|                          |  5 Exchange code for tokens |
	|                          |---------------------------->|
	|                          |     id_token + access_token |
	|                          |<----------------------------|
	|                          |  6 Fetch userinfo (name,    |
	|                          |    email, picture)          |
	|   7 Issue auth cookie, 302 -> /account/login-callback  |
	|<-------------------------|                             |
 8  |  Redirect to /profile.html                             |
	|  GET /account/me (cookie) -> user JSON                 |
	|------------------------->|                             |
```

1. User clicks **Sign in with Google**; `GET /account/login` returns a `Challenge`.
2. The browser is redirected to Google's consent screen.
3. After consent, Google redirects back to the app's `CallbackPath` (`/signin-google`) with an authorization `code`.
4/5. The Google handler exchanges the `code` for an `id_token` and `access_token`.
6. The handler calls Google's userinfo endpoint to read `name`, `email`, `picture`, etc.
7. An authentication **cookie** is issued and the browser is redirected to `/account/login-callback`.
8. The user lands on `profile.html`, which calls the protected `/account/me` endpoint using the cookie.

Logging out (`/account/logout`) clears the cookie.

---

## Project Structure

```
qauthgoogle/
├── Program.cs                       # Auth configuration (Cookie + Google), pipeline
├── Controllers/
│   └── AccountController.cs         # login, callback, me, logout endpoints
├── wwwroot/
│   ├── index.html                   # Landing page with "Sign in with Google" button
│   └── profile.html                 # Shows the signed-in user's profile
├── appsettings.json                 # Google ClientId/ClientSecret placeholders
└── Properties/launchSettings.json   # Local URLs (https://localhost:7025, http://localhost:5062)
```

---

## Google Cloud Configuration (step by step)

1. Go to the **[Google Cloud Console](https://console.cloud.google.com/)** and sign in.
2. Create (or select) a **Project**: top bar → project dropdown → **New Project** → name it → **Create**.
3. Configure the **OAuth consent screen**:
   - Left menu → **APIs & Services → OAuth consent screen**.
   - **User type**: choose **External** → **Create**.
   - Fill **App name**, **User support email**, **Developer contact email** → **Save and Continue**.
   - **Scopes**: add `.../auth/userinfo.email`, `.../auth/userinfo.profile`, and `openid` → **Save and Continue**.
   - **Test users**: while the app is in "Testing", add the Google account(s) you'll sign in with → **Save and Continue**.
4. Create the **OAuth 2.0 Client ID**:
   - Left menu → **APIs & Services → Credentials**.
   - **+ Create Credentials → OAuth client ID**.
   - **Application type**: **Web application**.
   - **Name**: e.g. `qauthgoogle-local`.
   - **Authorized JavaScript origins** (optional for this flow):
	 - `https://localhost:7025`
	 - `http://localhost:5062`
   - **Authorized redirect URIs** (required — must match `CallbackPath`):
	 - `https://localhost:7025/signin-google`
	 - `http://localhost:5062/signin-google`
   - **Create**.
5. Copy the generated **Client ID** and **Client secret** — you'll put them in user secrets next.

> If you later host the app, add your production redirect URI too, e.g.
> `https://your-domain.com/signin-google`.

---

## Application Configuration (secrets)

**Never commit real client secrets.** Use .NET User Secrets for local development:

```powershell
cd qauthgoogle
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
```

Alternatively, environment variables:

```powershell
$env:Authentication__Google__ClientId = "YOUR_CLIENT_ID"
$env:Authentication__Google__ClientSecret = "YOUR_CLIENT_SECRET"
```

`appsettings.json` only holds empty placeholders and documents the shape:

```json
{
  "Authentication": {
	"Google": {
	  "ClientId": "",
	  "ClientSecret": ""
	}
  }
}
```

---

## Run the App

```powershell
cd qauthgoogle
dotnet run
```

Then open **https://localhost:7025/** and click **Sign in with Google**.

---

## Endpoints

| Method | Route                     | Auth        | Description |
|--------|---------------------------|-------------|-------------|
| GET    | `/account/login`          | Anonymous   | Starts the OAuth flow (redirects to Google). Optional `?returnUrl=`. |
| GET    | `/signin-google`          | (handler)   | Google's redirect target; handled internally by the Google middleware. |
| GET    | `/account/login-callback` | Cookie      | Post-login landing; redirects to `returnUrl`. |
| GET    | `/account/me`             | **Required**| Returns the signed-in user's profile as JSON. |
| GET    | `/account/logout`         | Anonymous   | Clears the auth cookie and redirects to `/`. |

**`/account/me` response example:**

```json
{
  "id": "1234567890",
  "name": "Ada Lovelace",
  "email": "ada@example.com",
  "givenName": "Ada",
  "surname": "Lovelace",
  "picture": "https://lh3.googleusercontent.com/a/..."
}
```

---

## Postman / API Details

> **Important:** OAuth "Sign in with Google" is an **interactive, browser-based** flow.
> The redirect to Google's consent screen and the cookie session require a real browser,
> so you cannot fully script the login inside Postman with a single request. There are two
> practical ways to test the API in Postman:

### Option A — Reuse the browser session cookie (quickest)
1. Sign in through the browser at `https://localhost:7025/`.
2. Open DevTools → **Application → Cookies** and copy the auth cookie
   (default name `.AspNetCore.Cookies`).
3. In Postman, call `GET https://localhost:7025/account/me` and add a header:
   - `Cookie: .AspNetCore.Cookies=PASTE_VALUE_HERE`
4. You'll receive the profile JSON.

### Option B — Let Postman drive Google OAuth (Authorization tab)
Use Postman's built-in **OAuth 2.0** helper to get a token directly from Google
(useful if you want to inspect Google's tokens; note this app itself authenticates via cookie):

- **Auth Type:** OAuth 2.0
- **Grant Type:** Authorization Code (with PKCE)
- **Callback URL:** `https://oauth.pstmn.io/v1/callback` *(add this exact URI to your Google redirect URIs too)*
- **Auth URL:** `https://accounts.google.com/o/oauth2/v2/auth`
- **Access Token URL:** `https://oauth2.googleapis.com/token`
- **Client ID:** your Google Client ID
- **Client Secret:** your Google Client Secret
- **Scope:** `openid email profile`
- **Client Authentication:** Send as Basic Auth header

Click **Get New Access Token**, sign in with Google, then Postman stores the token.

### Import the ready-made collection
A Postman collection is included at **[`postman/qauthgoogle.postman_collection.json`](postman/qauthgoogle.postman_collection.json)**.
Import it into Postman:
- Set the collection variable `baseUrl` (default `https://localhost:7025`).
- Set `cookie` to your `.AspNetCore.Cookies` value (for Option A).

---

## Troubleshooting

| Symptom | Fix |
|--------|-----|
| `redirect_uri_mismatch` | The redirect URI in Google must exactly match `{scheme}://{host}/signin-google`. Add both http and https local URIs. |
| `Access blocked: app not verified` | Add your test account under **OAuth consent screen → Test users**, or publish the app. |
| `Missing config: Authentication:Google:ClientId` | Set the user secrets / env vars as shown above. |
| `401` on `/account/me` in Postman | You're missing a valid auth cookie — use Option A above. |
| HTTPS cert warning locally | Run `dotnet dev-certs https --trust`. |

---
<img width="3768" height="2037" alt="image" src="https://github.com/user-attachments/assets/23af15a0-6470-4af8-92f0-a2e5a04131d8" />

## Security Notes
- Client secrets live in **user secrets / environment variables**, never in source control.
- The session is a signed, HTTP-only cookie issued by the app.
- Only `openid email profile` scopes are requested (least privilege).
