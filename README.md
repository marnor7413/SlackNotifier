# SlackNotifier
Forward gmail messages to a slack channel

### Installation

##### Setup Google Console
Prerequisites: Gmail account
1. Login to Google Console at https://console.cloud.google.com/
2. Create new project
3. Enable the Gmail Api for your new project under _Enabled APIs & Services_ menu.
4. Click on _Credentials_ menu, then on _Create Credentials_. Choose _OAuth Client ID_. Pick a name and Save.
5. The created credential will be listed under Credentials menu. Click on it, and choose to download the credentials json.
6. Rename the file to _googleCredentialsOAuth.json_ and move it to _SN.Console_ folder. 

##### Setup Slack
Prerequisites: Slack account
1. Login to Slack and go to https://api.slack.com/apps. Create an App in slack.
2. Click on Basic information > Add features
   - Click to setup Bots. 
   - Click to setup Permissions. Copy token from _OAuth Tokens for Your Workspace_ section. Under _Scopes_ section, set the bot scopes: chat:write + files:write + remote_files:write
3. Create a file named **secrets.json**. Contents of file as per below, fill in your details. 
   
```JSON
[
  {
    "Subject": "yourServiceName",
    "Token": "yourWorkspaceOAuthTokenToken",
    "Destination": "channelId"
  }
]
```
4. Save the **secrets.json** file to _SN.Console_ folder.
5. Choose to _Install to Workspace_ to enable the app. A message will popup in the channel that a new integration have been created.
6. Important! Invite your bot to your channel.

##### Setup done
Doublecheck that the secrets.json and googleCredentials.json is in the SN.Console project.
Build and run!

Secrets filename can be changed in appsettings.