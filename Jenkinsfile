pipeline {
    agent any
	environment {
        DEPLOYMENT_DIR = 'C:\\deploy\\Slacknotifier'
    }
	
    stages {
		//stage ('Git Checkout') {
		//	steps {
		//	  git branch: 'develop', url: 'https://github.com/marnor7413/SlackNotifier'
		//	}
		//}
        stage('Restore') {
            steps {
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
