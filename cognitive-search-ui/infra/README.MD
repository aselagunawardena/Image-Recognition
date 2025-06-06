# myExpressApp deployment

This is a simple Express application deployment that Creates App service Plan with deployment slots.

## Project Structure

```
myExpressApp
├── .github
│   └── workflows
│       └── ci.yml
|       └── cd.yml
├── routes
│   └── index.js
├── views
│   └── index.ejs
├── app.js
├── package.json
└── README.md
```

## deployment

To deplye the infra, run:
az deployment group create --resource-group <your-resource-group> --template-file main.bicep --parameters @parameters.json

az deployment group create --resource-group rg-acemy-appsevice-prem --template-file main.bicep --parameters @parameters.json
