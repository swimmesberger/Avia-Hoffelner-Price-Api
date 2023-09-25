# Avia-Hoffelner-Price-Api
This web service exposes the regularly updated prices exposed as PDF files on the downloads page as a REST API.
Currently there is no caching layer, therefore for every GET on the endpoint a GET to the PDF will be executed.