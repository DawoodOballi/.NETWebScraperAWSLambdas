# .NETWebScraperAWSLambdas
A repository containing two event driven .NET AWS Lambdas

Whats included in the repo?
1. A WebSraper Lambda for scraping HTML and sending the html content via email using the AWS SES service
2. A WebScraperFileDownload for scraping a file from a website through the use of a dynamic XPATH
3. Fully automated versioning pipelines for automated tagging and CHANGELOG generation.
4. A Dockerfile for building and Azure DevOps agent for running the pipelines.
5. A local .vscode setup for running and debugging the lambdas locally
   1. To locally debug the lambdas install [AWS .NET Mock Lambda Test Tools](https://github.com/aws/aws-lambda-dotnet/blob/master/Tools/LambdaTestTool/README.md)
6. 
7. A script for automatically starting the ADO agent locally. You will need to get a PAT and replace it in the file for <ADO_PAT>
8. A serverless.yml file for deploying the lambdas via the [Serverless Framework](https://www.serverless.com/examples/serverlessDotNetSample).
9. Code coverage configured to automatically display code coverage when running unit tests. Packages used can be found the **.csproj files in the **.Tests directories.
   1.  Report Generator tool for generating HTML for the coverage.
   2.  Coverlet for collecting code coverage

NOTES:
* One thing to note is that to successfully deploy the .net lambdas and avoid getting a '.NET binaries' error in lambda console when testing. You have to first go into each lambda project dir and run 'dotnet lambda package'. Then you have to sepcify the zipped packages in serverless framework then run 'serverless deploy'
* For automation, also ensure to run 'npm init' to create the package.json file and then run the command 'serverless plugin install --name <name of plugin>' (relevant packages for this project are can be found the package.json file) to install the desired plugin into package.json. This is so that once you run this in the pipeline you can simply run 'npm ci' to automate the installation of the packages instead of having to specify each package installation in the CI/CD itself.


Installation:
* Install dotnet 6.0, npm, node.
* dotnet tool install -g Amazon.Lambda.Tools 
* dotnet tool install --global Amazon.Lambda.TestTool-6.0 
* dotnet tool list -g 
  * Follow https://github.com/aws/aws-lambda-dotnet/tree/master/Tools/LambdaTestTool to setup test tool on vscode
* https://www.nuget.org/packages/dotnet-reportgenerator-globaltool#readme-body-tab / https://reportgenerator.io/getstarted / https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/MSBuildIntegration.md#:~:text=Supported Formats%3A,teamcity
* https://github.com/coverlet-coverage/coverlet / https://www.nuget.org/packages/coverlet.collector / https://www.nuget.org/packages/coverlet.msbuild
* https://github.com/ryanluker/vscode-coverage-gutters/tree/1b7414f85ec133b87fe225d6d1ed6c815dd57382/example/dotnet
* To run test and generate html code coverage by just running the launch setting from debugger https://stackoverflow.com/questions/62899076/how-to-run-tasks-in-sequence
