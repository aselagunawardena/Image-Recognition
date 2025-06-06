param location string = resourceGroup().location
param appServicePlanName string
param skuName string = 'P1v2'
param webAppName string
param slotName string = 'staging'

module appServicePlan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlanModule'
  params: {
    location: location
    appServicePlanName: appServicePlanName
    skuName: skuName
  }
}

module appService 'modules/appService.bicep' = {
  name: 'appServiceModule'
  params: {
    appServicePlanName: appServicePlan.outputs.appServicePlanName
    location: location
    webAppName: webAppName
    slotName: slotName
  }
}
