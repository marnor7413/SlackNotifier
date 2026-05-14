# SlackNotifier
Forward Gmail messages with attachments to a Slack channel without any payment plans for automations.

## What is SlackNotifier?

SlackNotifier is a .NET application that forwards incoming emails and attachments to a Slack channel. It was built to solve a real problem: my local housing association needed a way for external parties to reach all residents without going through a manual email chain via the board.

Instead of emailing the board who then forwards to everyone individually, anyone can send an email that automatically appears in the Slack workspace where all residents are members.

The application started as a Windows-hosted service with a Jenkins pipeline for deployment. It is currently being rebuilt as a headless service for deployment in Kubernetes, using a local GitLab-to-Harbor-to-K3s pipeline.

## Architecture

The solution follows Clean Architecture with the following layers:

- **SN.Core** — domain models and interfaces
- **SN.Application** — application logic and services
- **SN.Infrastructure** — Gmail and Slack integrations
- **SN.Console** — entry point, hosting and configuration
- **SN.UnitTests** — unit tests

The application runs as a .NET BackgroundService. It connects to Gmail via IMAP using MailKit with App Password authentication, avoiding the need for browser-based OAuth2 in a headless environment. Secrets are managed outside source control and will be handled as Kubernetes Secrets in the target deployment.

---

### Installation
#### Setup Google application password (preferred method)
1. Log in to your Google account.
2. Go to https://myaccount.google.com/apppasswords, if this doesn't work then enable two-step verification.
3. Create an application password. Save the password.
4. Create a json file called GoogleImapSecretsDevelopment.json
5. Add the following structure:
```JSON
{
    "email": "<your email>",
    "password": "<your google application password>"
}
```
6. Copy the file to GoogleImapSecretsProduction.json, change email and password if needed.

#### Optional: Setup Google Console (fallback if application password shuts down)
Prerequisites: Gmail account + access to Google console
1. Log in to Google Console at https://console.cloud.google.com/
2. Create a new project
3. Enable the Gmail API for your new project under the _Enabled APIs & Services_ menu.
4. Click on the _Credentials_ menu, then on _Create Credentials_. Choose _OAuth Client ID_, then choose application type _desktop client_. Pick a name and save.
5. The created credential will be listed under the Credentials menu. Click on it, and choose to download the credentials JSON.
6. Rename the file to **GoogleSecretsDevelopment.json** and move it to the _SN.Console_ folder **\***. Make a copy of it named **GoogleSecretsProduction.json** for configuration of production settings.
7. Click _OAuth consent_ to set up authorizations for the app, during setup add the following scopes for the Gmail API in the Scopes setup step:
   - Non-sensitive scope: .../auth/gmail.addons.current.message.action
   - Restricted scope: https://mail.google.com/
   - Restricted scope: .../auth/gmail.modify
8. On Test users setup step, add your email account to the list.

#### Setup Slack
Prerequisites: Slack account
1. Log in to Slack and go to https://api.slack.com/apps. Create an App in Slack.
2. Click on Basic information > Add features
   - Click to set up Bots.
   - Click to set up Permissions. Copy token from the _OAuth Tokens for Your Workspace_ section. Under the _Scopes_ section, set the bot scopes: chat:write + files:read + files:write
3. Create a file named **SlackSecretsDevelopment.json**. Contents of file as per below, fill in your details **\***.

```JSON
[
  {
    "Subject": "SlackService",
    "Token": "yourWorkspaceOAuthTokenToken",
    "Destination": "channelId"
  }
]
```
4. Save the **SlackSecretsDevelopment.json** file to the _SN.Console_ folder. Make a copy named **SlackSecretsProduction.json** for configuration of production settings.
5. Choose to _Install to Workspace_ to enable the app. A message will pop up in the channel that a new integration has been created.
6. Important! Invite your bot to your channel.

#### Choose method of fetching emails
1. Open appsettings.json
2. Set the following value to run the application headless: `"GmailStrategy" : "Headless"`
3. Set the following value to run the application with OAuth2, authenticating via the browser. You will need to be logged in to your Gmail account: `"GmailStrategy" : "BrowserAuthentication"`. Can be used as fallback if Google removes application password authentication.

#### Setup done
Double-check that the secrets files are in the SN.Console project.
Build and run!

___\* NOTE: filenames for secrets files can be changed in appsettings___