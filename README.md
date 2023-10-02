# Avia-Hoffelner-Price-Api
This web service exposes the regularly updated prices exposed as PDF files on the downloads page as a REST API.
Currently there is no caching layer, therefore for every GET on the endpoint a GET to the PDF will be executed.

## Example
Following shows an example of a PDF to JSON conversion done by this service.
GET /avia/entries
![Pdf Example](./docs/images/price-pdf.png?raw=true)

````json
{
  "entries": [
    {
      "dayOfMonth": "2022-09-01",
      "grossPriceCtkWH": 65.089,
      "netPriceCtkwH": 54.241
    },
    {
      "dayOfMonth": "2022-10-01",
      "grossPriceCtkWH": 51.295,
      "netPriceCtkwH": 42.746
    },
    {
      "dayOfMonth": "2022-11-01",
      "grossPriceCtkWH": 24.163,
      "netPriceCtkwH": 20.136
    },
    {
      "dayOfMonth": "2022-12-01",
      "grossPriceCtkWH": 28.964,
      "netPriceCtkwH": 24.137
    },
    {
      "dayOfMonth": "2023-01-01",
      "grossPriceCtkWH": 35.220,
      "netPriceCtkwH": 29.350
    },
    {
      "dayOfMonth": "2023-02-01",
      "grossPriceCtkWH": 20.242,
      "netPriceCtkwH": 16.868
    },
    {
      "dayOfMonth": "2023-03-01",
      "grossPriceCtkWH": 20.160,
      "netPriceCtkwH": 16.800
    },
    {
      "dayOfMonth": "2023-04-01",
      "grossPriceCtkWH": 16.237,
      "netPriceCtkwH": 13.531
    },
    {
      "dayOfMonth": "2023-05-01",
      "grossPriceCtkWH": 15.137,
      "netPriceCtkwH": 12.614
    },
    {
      "dayOfMonth": "2023-06-01",
      "grossPriceCtkWH": 12.250,
      "netPriceCtkwH": 10.208
    },
    {
      "dayOfMonth": "2023-07-01",
      "grossPriceCtkWH": 13.854,
      "netPriceCtkwH": 11.545
    },
    {
      "dayOfMonth": "2023-08-01",
      "grossPriceCtkWH": 12.574,
      "netPriceCtkwH": 10.478
    },
    {
      "dayOfMonth": "2023-09-01",
      "grossPriceCtkWH": 13.583,
      "netPriceCtkwH": 11.319
    },
    {
      "dayOfMonth": "2023-10-01",
      "grossPriceCtkWH": 14.778,
      "netPriceCtkwH": 12.315
    },
    {
      "dayOfMonth": "2023-11-01",
      "grossPriceCtkWH": 14.623,
      "netPriceCtkwH": 12.186
    }
  ],
  "createdAt": "2023-09-25T18:58:02.870482+00:00"
}
````

When GET 'avia/entries/today' is called only the entry of the current month is shown. This can be helpful for external systems that regularly query the endpoint like home-assistant.
An example configuration for home-assistant could look like the following:
````yaml
rest:
    - resource: "http://price-service.local/avia/entries/today"
      scan_interval: 86400
      method: GET
      headers:
          Accept: application/json
          Content-Type: application/json
      sensor:
          - name: "GrossPriceCtkWH"
            unique_id: "20db483f-270b-49a5-a81f-cebc0f235b17"
            device_class: monetary
            value_template: "{{ value_json.grossPriceCtkWH/100.0 }}"
            force_update: true
            unit_of_measurement: "EUR/kWh"

          - name: "NetPriceCtkwH"
            unique_id: "1f9be0c9-e056-4677-af44-3d635d236c67"
            device_class: monetary
            value_template: "{{ value_json.netPriceCtkwH/100.0 }}"
            force_update: true
            unit_of_measurement: "EUR/kWh"
````

Docker: https://hub.docker.com/r/swimmes/avia-hoffelner-price-api
