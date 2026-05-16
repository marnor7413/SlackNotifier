# SlackNotifier
Forward Gmail messages with attachments to a Slack channel without any payment plans for automations.

## What is SlackNotifier?

SlackNotifier is a .NET application that forwards incoming emails and attachments to a Slack channel. It was built to solve a real problem: my local housing association needed a way for external parties to reach all residents without going through a manual email chain via the board. Instead of emailing the board who then forwards to everyone individually, anyone can send an email that automatically appears in the Slack workspace where all residents are members. Attachments such as meeting notices, annual reports and quotes are forwarded alongside the message, ensuring nothing gets lost. Before SlackNotifier, the board would send an email, then manually post a message in the Slack channel and either upload attachments separately or ask an admin to do it. SlackNotifier eliminates this by handling the entire flow automatically. One email is all it takes. In addition, the board can use the same email address to send out information directly to all residents, without any manual steps in Slack.

While solutions like Zapier or Slack's built-in email integration can forward emails to Slack, they either come with recurring costs or lack reliable attachment support. SlackNotifier is self-hosted, free to run and handles attachments fully.

The application started as a Windows-hosted service with a Jenkins pipeline for deployment. It is currently being rebuilt as a headless service for deployment in Kubernetes, using a local GitLab-to-Harbor-to-K3s pipeline.

## Architecture

The solution follows Clean Architecture with the following layers:

- **SN.Core** — domain models and interfaces
- **SN.Application** — application logic and services
- **SN.Infrastructure** — Gmail and Slack integrations
- **SN.Console** — entry point, hosting and configuration
- **SN.UnitTests** — unit tests

The application runs as a .NET BackgroundService. It connects to Gmail via IMAP using MailKit with App Password authentication, avoiding the need for browser-based OAuth2 in a headless environment. Secrets are managed outside source control, via .NET user-secrets locally and Kubernetes Secrets in the target deployment.

---

## Installation

### 1. Setup Gmail — Application Password (preferred method)

This is the recommended method for running the application headless, including in Kubernetes.

1. Log in to your Google account.
2. Go to https://myaccount.google.com/apppasswords. If this does not work, enable two-step verification first.
3. Create an application password and save it.

### 2. Setup Slack

Prerequisites: Slack account

1. Log in to Slack and go to https://api.slack.com/apps. Create an App.
2. Click on **Basic Information > Add features**:
   - Set up Bots.
   - Set up Permissions. Copy the token from the _OAuth Tokens for Your Workspace_ section. Under _Scopes_, set the bot scopes: `chat:write`, `files:read`, `files:write`.
3. Choose **Install to Workspace** to enable the app.
4. Important: Invite your bot to the channel it should post to.

### 3. Choose method of fetching emails

1. Open `appsettings.json`.
2. Set `"GmailStrategy": "Headless"` to run the application headless using App Password authentication. This is required for Kubernetes.
3. Set `"GmailStrategy": "BrowserAuthentication"` to authenticate via the browser using OAuth2. Can be used as a fallback if Google removes App Password authentication. See the **Optional: Google Console setup** section below.

### 4. Configure secrets

Secrets are never stored in source control. Choose the method that matches your environment.

---

#### Running locally (Development)

Secrets are managed via .NET user-secrets, which stores values in your user profile outside the repository and are never checked in to Git.

Run the following commands from the `SN.Console` project folder:

```bash
dotnet user-secrets set "GmailImapSecrets:Email" "your-email@gmail.com"
dotnet user-secrets set "GmailImapSecrets:Password" "your-app-password"
dotnet user-secrets set "SlackSecrets:Subject" "SlackService"
dotnet user-secrets set "SlackSecrets:Token" "your-slack-oauth-token"
dotnet user-secrets set "SlackSecrets:Destination" "your-channel-id"
```

Make sure `ASPNETCORE_ENVIRONMENT` is set to `Development` in your launch profile (`launchSettings.json`).

---

#### Running in Kubernetes (Production)

Secrets are injected as environment variables via a Kubernetes Secret. Create a file `k8s/secret.yaml` with the following content. **Do not check this file into source control.**

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: slacknotifier-secrets
type: Opaque
stringData:
  GmailImapSecrets__Email: "your-email@gmail.com"
  GmailImapSecrets__Password: "your-app-password"
  SlackSecrets__Subject: "SlackService"
  SlackSecrets__Token: "your-slack-oauth-token"
  SlackSecrets__Destination: "your-channel-id"
```

Apply it to your cluster:

```bash
kubectl apply -f k8s/secret.yaml
```

The secret is referenced in the Deployment via `envFrom`. .NET automatically translates the `__` separator in environment variable names to `:`, matching the configuration keys the application expects.

---

### Optional: Setup Google Console (fallback if App Password is discontinued)

Prerequisites: Gmail account + access to Google Cloud Console

1. Log in to Google Console at https://console.cloud.google.com/
2. Create a new project.
3. Enable the Gmail API under **Enabled APIs & Services**.
4. Click **Credentials > Create Credentials > OAuth Client ID**. Choose application type **Desktop client**, pick a name and save.
5. Download the credentials JSON from the Credentials menu.
6. Rename the file to `GoogleSecretsDevelopment.json` and place it in the `SN.Console` folder. Make a copy named `GoogleSecretsProduction.json` for production.
7. Configure the OAuth consent screen and add the following Gmail API scopes:
   - Non-sensitive: `.../auth/gmail.addons.current.message.action`
   - Restricted: `https://mail.google.com/`
   - Restricted: `.../auth/gmail.modify`
8. Under **Test users**, add your Gmail account.

Set `"GmailStrategy": "BrowserAuthentication"` in `appsettings.json` to use this method. The filenames for the credentials files can be changed in `appsettings.Production.json`.

---

### 5. Build and run

```bash
dotnet build
dotnet run
```