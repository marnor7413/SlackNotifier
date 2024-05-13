pipeline {
    agent any
	environment {
        DEPLOYMENT_DIR = 'C:\\deploy\\Slacknotifier'
		USER_DESKTOP_DESTINATION = 'C:\\Users\\noren_2c3vh71\\Desktop\\Slacknotifier.bat'
		ASPNETCOREENVIRONMENT = ''
		SELECTION = ''
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
						
					echo "${SELECTED_ENV} was selected from the list"
					SELECTION = SELECTED_ENV
					echo "${SELECTION} is passed to other stages"
				}
			}
		}
		
		stage('Set environment') {
            steps {
               script { 
					
					if (SELECTION == 'Development') { 
						echo "Environment value ${SELECTION} fetched"
                    } else if (SELECTION == 'Production') {
						echo "Environment value ${SELECTION} fetched"
                    } else {
						echo "No selection was made, setting Default as environment"
                        SELECTION = 'Default'
                    }
					echo "Selection set to ${SELECTION}"
			   }
            }
        }
        stage('Restore') {
            steps {
				echo 'Restoring dependencies'
                bat 'dotnet restore Slacknotifier.sln'
            }
        }
        stage('Build') {
            steps {
				echo 'Building application'
                bat "dotnet build --configuration Release .\\SN.Console\\SN.ConsoleApp.csproj"
            }
        }
        stage('Deploy') {
			when {
					expression { SELECTION != 'Default' }
			}
			steps {
				script {
					if (!fileExists(env.DEPLOYMENT_DIR)) {
						echo 'Application directory missing, creating...'
						bat "mkdir ${env.DEPLOYMENT_DIR}"
					} else {
						echo 'Directory already exists'
						bat "taskkill /F /FI \"IMAGENAME eq dotnet.exe\" /FI \"WINDOWTITLE eq SN.ConsoleApp\" /T >NUL 2>&1"
						bat "del /Q ${env.DEPLOYMENT_DIR}\\*"
						bat "for /D %%p in (${env.DEPLOYMENT_DIR}\\*) do rmdir /S /Q %%p"
					}
					
					echo 'Publishing to application'
					bat "dotnet publish --configuration Release -o ${env.DEPLOYMENT_DIR} .\\SN.Console\\SN.ConsoleApp.csproj"
					
					echo "Adding secrets to application for ${SELECTION}"
					echo "Copying runner batch file to desktop into ${env.USER_DESKTOP_DESTINATION}"
					if (SELECTION == 'Production') {
						bat "xcopy /s /y c:\\deploy\\secrets\\SlackNotifier\\GoogleSecretsProduction.json ${env.DEPLOYMENT_DIR}"
						bat "xcopy /s /y c:\\deploy\\secrets\\SlackNotifier\\SlackSecretsProduction.json ${env.DEPLOYMENT_DIR}"
						bat "xcopy /s /y c:\\deploy\\runner\\SlackNotifier\\SlacknotifierProduction.bat ${env.USER_DESKTOP_DESTINATION}"
					} else {
						bat "xcopy /s /y c:\\deploy\\secrets\\SlackNotifier\\GoogleSecretsDevelopment.json ${env.DEPLOYMENT_DIR}"
						bat "xcopy /s /y c:\\deploy\\secrets\\SlackNotifier\\SlackSecretsDevelopment.json ${env.DEPLOYMENT_DIR}"
						bat "xcopy /s /y c:\\deploy\\runner\\SlackNotifier\\SlacknotifierDevelopment.bat ${env.USER_DESKTOP_DESTINATION}"
					}					

					echo 'Done!'
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
