:: Note: use lowercase names for the Docker images
SET DOCKER_IMAGE=icuboid/sensewire/server-nginx
:: "testing" is the latest dev build, usually matching the code in the "master" branch
SET DOCKER_TAG=%DOCKER_IMAGE%:testing

SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-16%
cd %APP_HOME%

:: Build the container image
    git log --pretty=format:%%H -n 1 > tmpfile.tmp
    SET /P COMMIT=<tmpfile.tmp
    DEL tmpfile.tmp
    SET DOCKER_LABEL2=Commit=%COMMIT%
	
docker build --compress --tag %DOCKER_TAG% --label "%DOCKER_LABEL2%" .