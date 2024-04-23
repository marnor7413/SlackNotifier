pipeline {
    agent any
	environment {
		SELECTED_ENV = ''
        DEPLOYMENT_DIR = 'C:\\deploy\\Slacknotifier'
		ASPNETCOREENVIRONMENT = ''
    }
	
    stages {
		stage('Parameters') {
            steps {
                script {
                    properties([
                        parameters([
                            multiselect(
                                decisionTree: [
                                    variableDescriptions: [
                                        [
                                            label       : 'Environment',
                                            variableName: 'SELECTED_ENV'
                                        ]
                                    ],
                                    itemList: [
                                        ['value': 'Default'],
                                        ['value': 'Development'],
                                        ['value': 'Production']
                                    ]
                                ],
                                description: 'Please select!',
                                name: 'Environment'
                            )
                        ])
                    ])
					 echo "Selected environment: ${env.SELECTED_ENV}"
                }
            }
        }
		
		stage('Set environment') {
            steps {
               script { 
					if (env.SELECTED_ENV == 'Development') { 
						env.ASPNETCOREENVIRONMENT = env.SELECTED_ENV
						echo "Environment value ${env.ASPNETCOREENVIRONMENT} fetched from selection item ${env.SELECTED_ENV}"
                    } else if (env.SELECTED_ENV == 'Production') {
						echo "Environment value ${env.ASPNETCOREENVIRONMENT} fetched from selection item ${env.SELECTED_ENV}"
                        env.ASPNETCOREENVIRONMENT = env.SELECTED_ENV
                    } else {
						echo "Environment value Development fetched from selection item ${env.SELECTED_ENV}"
                        env.ASPNETCOREENVIRONMENT = 'Development'
                    }
			   }
            }
        }

        stage('Restore') {
            steps {
				bat "set ASPNETCORE_ENVIRONMENT=${env.ASPNETCOREENVIRONMENT}"
				bat "setx ASPNETCORE_ENVIRONMENT ${env.ASPNETCOREENVIRONMENT} /M"
				echo "${env.ASPNETCORE_ENVIRONMENT} was set"
                bat 'dotnet restore Slacknotifier.sln'
            }
        }
        stage('Build') {
            steps {
                bat 'dotnet build --configuration Release Slacknotifier.sln'
            }
        }
        stage('Deploy') {
			steps {
				script {
					if (!fileExists(env.DEPLOYMENT_DIR)) {
						bat "mkdir ${env.DEPLOYMENT_DIR}"
					} else {
						echo 'Directory already exists'
						bat "taskkill /F /FI \"IMAGENAME eq dotnet.exe\" /FI \"WINDOWTITLE eq SN.ConsoleApp\" /T >NUL 2>&1"
						bat "del /Q ${env.DEPLOYMENT_DIR}\\*"
						bat "for /D %%p in (${env.DEPLOYMENT_DIR}\\*) do rmdir /S /Q %%p"
					}
					
					bat "xcopy /s /y .\\SN.Console\\bin\\Release\\net6.0\\* ${env.DEPLOYMENT_DIR}"
					bat "xcopy /s /y c:\\deploy\\secrets\\SlackNotifier\\* ${env.DEPLOYMENT_DIR}"
					
				}
			}
		}
    }
	post {
        always {
            cleanWs()
        }
    }
}
