param appServicePlanName string
param location string
param webAppName string
param slotName string

resource appService 'Microsoft.Web/sites@2021-02-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: appServicePlanName
    siteConfig: {
      appCommandLine: 'npm start' // Startup command
      linuxFxVersion: 'NODE|20-lts' // Specify the Node.js runtime version
    }
  }
}

resource deploymentSlot 'Microsoft.Web/sites/slots@2021-02-01' = {
  parent: appService
  name: slotName
  location: location
  properties: {
    serverFarmId: appServicePlanName
    siteConfig: {
      appCommandLine: 'npm start' // Startup command for staging slot
      linuxFxVersion: 'NODE|20-lts' // Specify the Node.js runtime version for the slot
    }
  }
}

output appServiceId string = appService.id
