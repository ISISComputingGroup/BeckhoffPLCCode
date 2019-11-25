#!groovy

pipeline {

  // agent defines where the pipeline will run.
  agent {  
    label {
      label "twinCAT"
    }
  }
  
  triggers {
    pollSCM('H/2 * * * *')
  }
  
  stages {  
    stage("Checkout") {
      steps {
        deleteDir()
        echo "Branch: ${env.BRANCH_NAME}"
        checkout scm
      }
    }
    
	stage("Dependencies") {
        steps {
          echo "Installing local genie python"
          timeout(time: 1, unit: 'HOURS') {
            bat """
                update_genie_python.bat ${env.WORKSPACE}\\Python
            """
          }
        }
    }

    stage("Build") {
      steps {
        bat """
            build.bat
            """
       }
    }

    stage("Test") {
        steps {
        bat """
			set "PYTHONDIR=%WORKSPACE%\\Python"
            call C:\\Instrument\\Apps\\EPICS\\config_env.bat
            python %EPICS_KIT_ROOT%\\support\\IocTestFramework\\master\\run_tests.py -tp ".\\PLC Development\\tests"
            """
    }
    }
  }
  
  post {
    always {
      junit "test-reports/**/*.xml"
    }
    failure {
      step([$class: 'Mailer', notifyEveryUnstableBuild: true, recipients: 'icp-buildserver@lists.isis.rl.ac.uk', sendToIndividuals: true])
    }
    changed {
      step([$class: 'Mailer', notifyEveryUnstableBuild: true, recipients: 'icp-buildserver@lists.isis.rl.ac.uk', sendToIndividuals: true])
    }
  }
  
  // The options directive is for configuration that applies to the whole job.
  options {
    buildDiscarder(logRotator(numToKeepStr:'5', daysToKeepStr: '7'))
    timeout(time: 60, unit: 'MINUTES')
    disableConcurrentBuilds()
  }
}
