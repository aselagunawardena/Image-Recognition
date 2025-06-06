param location string
param appServicePlanName string
param skuName string

resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: skuName
    tier: 'PremiumV2'
  }
  properties: {
    reserved: true
  }
}

output appServicePlanName string = appServicePlan.name
