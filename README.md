# SlackNotifier
Forward Gmail messages to a Slack channel

### Installation

#### Setup Google Console
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

#### Setup done
Double-check that the secrets files are in the SN.Console project.
Build and run!

___* NOTE: filenames for secrets files can be changed in appsettings___
