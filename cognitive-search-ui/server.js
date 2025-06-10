const express = require("express");
const axios = require("axios");
const bodyParser = require("body-parser");
const { BlobServiceClient, StorageSharedKeyCredential, generateBlobSASQueryParameters, BlobSASPermissions } = require("@azure/storage-blob");
const multer = require("multer");
const path = require("path"); // Import the path module

const app = express();
const port = 3000;

// Middleware
app.use(bodyParser.json());
app.use(express.static("public")); // Serve static files (frontend)

// Configure multer for file uploads
const upload = multer({ dest: "uploads/" }); // Define the upload middleware

app.get("/", (req, res) => res.sendFile(path.join(__dirname, "public", "index.html")));
app.get("/about", (req, res) => res.sendFile(path.join(__dirname, "public", "about.html")));
app.get("/upload", (req, res) => res.sendFile(path.join(__dirname, "public", "upload.html")));

require("dotenv").config(); // Load environment variables from .env

// Azure Cognitive Search Configuration
const SEARCH_SERVICE_NAME = process.env.SEARCH_SERVICE_NAME;
const SEARCH_API_KEY = process.env.SEARCH_API_KEY;
const INDEX_NAME = process.env.INDEX_NAME;

// Azure Storage Configuration
const STORAGE_ACCOUNT_NAME = process.env.STORAGE_ACCOUNT_NAME;
const STORAGE_ACCOUNT_KEY = process.env.STORAGE_ACCOUNT_KEY;
const CONTAINER_NAME = process.env.CONTAINER_NAME;

// Function to generate SAS token
async function generateSasToken(blobName) {
    const sharedKeyCredential = new StorageSharedKeyCredential(STORAGE_ACCOUNT_NAME, STORAGE_ACCOUNT_KEY);
    const blobServiceClient = new BlobServiceClient(`https://${STORAGE_ACCOUNT_NAME}.blob.core.windows.net`, sharedKeyCredential);
    const containerClient = blobServiceClient.getContainerClient(CONTAINER_NAME);
    const blobClient = containerClient.getBlobClient(blobName);

    const expiresOn = new Date();
    expiresOn.setHours(expiresOn.getHours() + 1); // SAS token valid for 1 hour

    const sasToken = generateBlobSASQueryParameters(
        {
            containerName: CONTAINER_NAME,
            blobName: blobName,
            permissions: BlobSASPermissions.parse("r"), // Read-only permissions
            expiresOn: expiresOn,
        },
        sharedKeyCredential
    ).toString();

    const sasUrl = `${blobClient.url}?${sasToken}`;
    console.log(`Generated SAS URL for blob "${blobName}": ${sasUrl}`); // Log the SAS URL
    return sasUrl;
}

// Endpoint to query Azure Cognitive Search
app.post("/search", async (req, res) => {
    const { query } = req.body;

    const searchUrl = `https://${SEARCH_SERVICE_NAME}.search.windows.net/indexes/${INDEX_NAME}/docs/search?api-version=2023-07-01-Preview`;

    try {
        const response = await axios.post(
            searchUrl,
            {
                search: query, // Query for tags like "people"
                select: "fileName,url,tags,description", // Fields to retrieve
            },
            {
                headers: {
                    "Content-Type": "application/json",
                    "api-key": SEARCH_API_KEY,
                },
            }
        );

        // Attach SAS token to each image URL
        const results = await Promise.all(
            response.data.value.map(async (item) => {
                const sasUrl = await generateSasToken(item.fileName);
                console.log(`File: ${item.fileName}, SAS URL: ${sasUrl}`); // Log the file name and SAS URL
                return {
                    ...item,
                    url: sasUrl, // Replace the URL with the SAS URL
                };
            })
        );

        res.json(results); // Return the search results with SAS URLs
    } catch (error) {
        console.error("Error querying Azure Cognitive Search:", error.message);
        res.status(500).send("Error querying Azure Cognitive Search");
    }
});

// Upload endpoint
app.post("/upload", upload.single("file"), async (req, res) => {
    try {
        const file = req.file;

        if (!file) {
            return res.status(400).send("No file uploaded.");
        }

        // Create BlobServiceClient
        const sharedKeyCredential = new StorageSharedKeyCredential(STORAGE_ACCOUNT_NAME, STORAGE_ACCOUNT_KEY);
        const blobServiceClient = new BlobServiceClient(`https://${STORAGE_ACCOUNT_NAME}.blob.core.windows.net`, sharedKeyCredential);

        // Get container client
        const containerClient = blobServiceClient.getContainerClient(CONTAINER_NAME);

        // Upload file to Azure Blob Storage
        const blobName = file.originalname;
        const blockBlobClient = containerClient.getBlockBlobClient(blobName);
        await blockBlobClient.uploadFile(file.path);

        // Respond with success
        res.status(200).send("File uploaded successfully!");
    } catch (error) {
        console.error("Error uploading file:", error);
        res.status(500).send("Error uploading file.");
    }
});

// Start the server
app.listen(port, () => {
    console.log(`Server running at http://localhost:${port}`);
});
