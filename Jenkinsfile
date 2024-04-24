pipeline {
    agent any
	environment {
        DEPLOYMENT_DIR = 'C:\\deploy\\Slacknotifier'
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
						ASPNETCOREENVIRONMENT = SELECTION
						echo "Environment value ${ASPNETCOREENVIRONMENT} fetched from selection item ${SELECTION}"
                    } else if (SELECTION == 'Production') {
						echo "Environment value ${ASPNETCOREENVIRONMENT} fetched from selection item ${SELECTION}"
                        ASPNETCOREENVIRONMENT = SELECTION
                    } else {
						echo "Environment value Development fetched from selection item ${SELECTION}"
                        ASPNETCOREENVIRONMENT = 'Development'
                    }
			   }
            }
        }

        stage('Restore') {
            steps {
				bat "set ASPNETCORE_ENVIRONMENT=${ASPNETCOREENVIRONMENT}"
				bat "setx ASPNETCORE_ENVIRONMENT ${ASPNETCOREENVIRONMENT} /M"
				echo "${ASPNETCORE_ENVIRONMENT} was set"
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
					if (!fileExists(DEPLOYMENT_DIR)) {
						bat "mkdir ${DEPLOYMENT_DIR}"
					} else {
						echo 'Directory already exists'
						bat "taskkill /F /FI \"IMAGENAME eq dotnet.exe\" /FI \"WINDOWTITLE eq SN.ConsoleApp\" /T >NUL 2>&1"
						bat "del /Q ${DEPLOYMENT_DIR}\\*"
						bat "for /D %%p in (${DEPLOYMENT_DIR}\\*) do rmdir /S /Q %%p"
					}
					
					bat "xcopy /s /y .\\SN.Console\\bin\\Release\\net6.0\\* ${DEPLOYMENT_DIR}"
					bat "xcopy /s /y c:\\deploy\\secrets\\SlackNotifier\\* ${DEPLOYMENT_DIR}"
					
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
