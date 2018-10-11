# RetrieveAndPush Azure Function #

## How it works ##
In order to achieve offline support goals the function will:

- Instantiate a registryManager object for querying the IoT Hub Device Registry 
- Retrieve all the IoT Edge Devices (a.k.a. all the devices with the capability "iotedge" set to true)
- Retrieve all the leaf devices associated to each IoT Edge Device Gateway found
- Update the IoT Edge LoRaWanNetworkSrvModule Module Twin with the list of all the leaf devices found

## Setup ##

Connection strings needed:

- IoTHubConnectionString

EnvironmentVariables needed:

- ModuleId