# SlackNotifier
Forward gmail messages to a slack channel

### Installation

##### Setup Google Console
Prerequisites: Gmail account + access to Google console
1. Login to Google Console at https://console.cloud.google.com/
2. Create new project
3. Enable the Gmail Api for your new project under _Enabled APIs & Services_ menu.
4. Click on _Credentials_ menu, then on _Create Credentials_. Choose _OAuth Client ID_, then choose application type _desktop client_. Pick a name and Save.
5. The created credential will be listed under Credentials menu. Click on it, and choose to download the credentials json.
6. Rename the file to **GoogleSecretsDevelopment.json** and move it to _SN.Console_ folder **\***. Make a copy of it named **GoogleSecretsProduction.json** for configuration of production settings.
7. Click _OAuth consent_ to setup authorizations for the app, during setup add the following scopes for the Gmail-api in the Scopes setup step:
   a. Non-sensitive scope: .../auth/gmail.addons.current.message.action
   b. Restricted scope: https://mail.google.com/
   c. Restricted scope: .../auth/gmail.modify
8. On Test users setup step, add your email account to the list.

##### Setup Slack
Prerequisites: Slack account
1. Login to Slack and go to https://api.slack.com/apps. Create an App in slack.
2. Click on Basic information > Add features
   - Click to setup Bots. 
   - Click to setup Permissions. Copy token from _OAuth Tokens for Your Workspace_ section. Under _Scopes_ section, set the bot scopes: chat:write + files:write + remote_files:write
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
4. Save the **SlackSecretsDevelopment.json** file to _SN.Console_ folder. Make a copy if it named **SlackSecretsProduction.json** for configuration of production settings.
5. Choose to _Install to Workspace_ to enable the app. A message will popup in the channel that a new integration have been created.
6. Important! Invite your bot to your channel.

##### Setup done
Doublecheck that the secrets files are in the SN.Console project.
Build and run!

___* NOTE: filenames for secrets files can be changed in appsettings___
