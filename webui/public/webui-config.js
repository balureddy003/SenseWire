// Copyright (c) Microsoft. All rights reserved.

// This file is rewritten during the deployment.
// Values can be changed for development purpose.
// The file is public, so don't expect secrets here.

var DeploymentConfig = {
  authEnabled: true,
  authType: 'aad',
  aad : {
    tenant: 'balachandrareddyoutlook.onmicrosoft.com',
    appId: '173f39ce-bc70-4e75-a0d7-de508ecd775a',
    instance: 'https://login.microsoftonline.com/'
  }
}
