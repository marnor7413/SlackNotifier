pipeline {
    agent any
	environment {
        DEPLOYMENT_DIR = 'C:\\deploy\\Slacknotifier'
		ASPNETCOREENVIRONMENT = ''
		SELECTION = ''
		BUILDCONFIGURATION = ''
		USERFOLDER = 'noren_2c3vh71'
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
						BUILDCONFIGURATION = 'Debug'
                    } else if (SELECTION == 'Production') {
						echo "Environment value ${SELECTION} fetched"
						BUILDCONFIGURATION = 'Release'
                    } else {
						echo "No selection was made, setting Default as environment"
                        SELECTION = 'Default'
						BUILDCONFIGURATION = 'Debug'
                    }
					echo "Configuration set to ${BUILDCONFIGURATION}"
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
                bat "dotnet build --configuration ${env.BUILDCONFIGURATION} .\\SN.Console\\SN.ConsoleApp.csproj"
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
					bat "dotnet publish --configuration ${env.BUILDCONFIGURATION} -o ${env.DEPLOYMENT_DIR} .\\SN.Console\\SN.ConsoleApp.csproj"
					
					echo 'Adding secrets to application'				
					bat "xcopy /s /y c:\\deploy\\secrets\\SlackNotifier\\GoogleSecrets${env.BUILDCONFIGURATION}.json ${env.DEPLOYMENT_DIR}"
					bat "xcopy /s /y c:\\deploy\\secrets\\SlackNotifier\\SlackSecrets${env.BUILDCONFIGURATION}.json ${env.DEPLOYMENT_DIR}"
					
					echo "Copying runner batch file to desktop for user ${env.USERFOLDER}"
					bat "xcopy /s /y c:\\deploy\\runner\\SlackNotifier\\Slacknotifier${env.BUILDCONFIGURATION}.bat C:\\Users\\${env.USERFOLDER}\\Desktop"
					
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
