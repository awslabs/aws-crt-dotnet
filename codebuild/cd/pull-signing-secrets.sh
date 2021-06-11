#!/usr/bin/env bash

set -e

echo "ECS Uri environment:"
echo $AWS_CONTAINER_CREDENTIALS_FULL_URI
echo $AWS_CONTAINER_CREDENTIALS_RELATIVE_URI

aws --region us-west-2 sts assume-role --role-arn arn:aws:iam::582595803497:role/aws-common-runtime-secrets-role --role-session-name DotnetCrtCD --query Credentials > $TEMP/creds.txt

# these will unset when the script ends
export AWS_ACCESS_KEY_ID=$(cat ${TEMP}/creds.txt | sed -n 's/.*"AccessKeyId": "\(.*\)".*/\1/p')
export AWS_SECRET_ACCESS_KEY=$(cat ${TEMP}/creds.txt | sed -n 's/.*"SecretAccessKey": "\(.*\)".*/\1/p')
export AWS_SESSION_TOKEN=$(cat ${TEMP}/creds.txt | sed -n 's/.*"SessionToken": "\(.*\)".*/\1/p')
rm $TEMP/creds.txt

# code signing not needed yet
#aws --region us-west-2 s3 cp s3://aws-dotnet-devops/aws-dotnet-sdk-team_cert.pfx $TEMP/cert.pfx
#aws --region us-west-2 secretsmanager get-secret-value --secret-id arn:aws:secretsmanager:us-west-2:582595803497:secret:prod/build-infrastructure/teamcert-kIcScQ --query SecretString | sed -n 's/{\\"Key\\":\\"\(.*\)\\"}/\1/p' | sed -n 's/"\(.*\)"/\1/p' > $TEMP/cert_pass

# strong name key
aws --region us-west-2 secretsmanager get-secret-value --secret-id arn:aws:secretsmanager:us-west-2:582595803497:secret:prod/build-infrastructure/snk-kz1uH0 --query SecretBinary | sed -n 's/^"\(.*\)"/\1/p' | base64 -d > $TEMP/snk 


