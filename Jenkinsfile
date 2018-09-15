def releaseInfo
def branch

pipeline {
  agent {
    docker {
      image 'microsoft/dotnet:2.1-sdk'
      args '-v $HOME/.dotnet:/.dotnet'
    }
  }

  options {
    buildDiscarder(logRotator(numToKeepStr:'5'))
  }

  stages {
    stage('Set branch') {
      when { expression { env.BRANCH_NAME != 'master' } }

      steps {
        script {
          branch = env.BRANCH_NAME
        }
      }
    }

    stage('Build Release Info') {
      steps {
        script {
          releaseInfo = generateGithubReleaseInfo(
            'PaulTrampert',
            'PTrampert.ApiProxy',
            'v',
            'Github User/Pass',
            'https://api.github.com',
            branch,
            env.BUILD_NUMBER
          )

          echo "Next version is ${releaseInfo.nextVersion().toString()}."
          echo "Changelog:\n${releaseInfo.changelogToMarkdown()}"
        }
      }
    }

    stage('Build') {
      steps {
        sh "dotnet build -c Release /p:Version=${releaseInfo.nextVersion().toString()}"
      }
    }

    stage('Test') {
      steps {
        sh "dotnet test PTrampert.ApiProxy.Test/PTrampert.ApiProxy.Test.csproj -l trx -c Release --no-build /p:Version=${releaseInfo.nextVersion().toString()}"
      }
    }

    stage('Package') {
      steps {
        sh "dotnet pack PTrampert.ApiProxy/PTrampert.ApiProxy.csproj -c Release --no-build /p:Version=${releaseInfo.nextVersion().toString()}"
      }
    }

    stage ('Tag') {
      when { expression { env.BRANCH_NAME == 'master' } }

      steps {
        script {
          publishGithubRelease(
            'PaulTrampert',
            'PTrampert.ApiProxy',
            releaseInfo,
            'v',
            'Github User/Pass',
            'https://api.github.com'
          )
        }
      }
    }

    stage('Publish Pre-Release') {
      when { expression{env.BRANCH_NAME != 'master'} }
      environment {
        API_KEY = credentials('nexus-nuget-apikey')
      }
      steps {
        sh "dotnet nuget push **/*.nupkg -s 'https://packages.ptrampert.com/repository/nuget-prereleases/' -k ${env.API_KEY}"
      }
    }

    stage('Publish Release') {
      when { expression {env.BRANCH_NAME == 'master'} }
      environment {
        API_KEY = credentials('nuget-api-key')
      }
      steps {
        sh "dotnet nuget push **/*.nupkg -s https://api.nuget.org/v3/index.json -k ${env.API_KEY}"
      }
    }
  }

  post {
    failure {
      mail(
        to: 'paul.trampert@ptrampert.com',
        subject: "Build status of ${env.JOB_NAME} changed to ${currentBuild.result}", body: "Build log may be found at ${env.BUILD_URL}"
      )
    }
    always {
      archiveArtifacts '**/*.nupkg'

      xunit(
        testTimeMargin: '3000',
        thresholdMode: 1,
        thresholds: [
          failed(unstableThreshold: '0')
        ],
        tools: [
          MSTest(
            deleteOutputFiles: true,
            failIfNotNew: true,
            pattern: '**/*.trx',
            skipNoTestFiles: false,
            stopProcessingIfError: true
          )
        ]
      )

      cobertura(
        autoUpdateHealth: false,
        autoUpdateStability: false,
        coberturaReportFile: '**/*.cobertura.xml',
        conditionalCoverageTargets: '70, 0, 0',
        failUnhealthy: false,
        failUnstable: false,
        lineCoverageTargets: '80, 0, 0',
        maxNumberOfBuilds: 0,
        methodCoverageTargets: '80, 0, 0',
        onlyStable: false,
        sourceEncoding: 'ASCII',
        zoomCoverageChart: false
      )
    }
  }
}