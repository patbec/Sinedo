<img align="right" src="https://www.opc-router.de/wp-content/uploads/2020/04/icon_rest_webservice_600x400px.png" width="120"/>

# Sinedo

To control downloads with other programs or to add new ones automatically there are the following interfaces.

## System

Returns information about the server.

Name | Wert
--- | ---
Address | **/api/system**
Return | **application/json**
Method | **GET**
Status code | **401 Unauthorized** or **200 OK**

Example response:
```
{
    "hostname": "Patricks-iMac",
    "platform": "Unix",
    "architecture": "X64",
    "pid": 11375,
    "version": "1.0.0"
}
```

## Scheduler

Returns information about the task scheduler.

Name | Wert
--- | ---
Address | **/api/scheduler**
Return | **application/json**
Method | **GET**
Status code | **401 Unauthorized** or **200 OK**

Example response:
```
{
    "downloadsCount": 1,
    "downloadsRunning": 0
}
```

## Downloads

Returns a list of downloads.

Name | Wert
--- | ---
Address | **/api/downloads**
Return | **application/json**
Optional parameter | filter={state}
Method | **GET**
Status code | **401 Unauthorized** or **200 OK**

Example request with a filter:
```
http://0.0.0.0:5000/api/downloads
http://0.0.0.0:5000/api/downloads?filter=Idle
http://0.0.0.0:5000/api/downloads?filter=Queued
http://0.0.0.0:5000/api/downloads?filter=Canceled
http://0.0.0.0:5000/api/downloads?filter=Failed
http://0.0.0.0:5000/api/downloads?filter=Running
http://0.0.0.0:5000/api/downloads?filter=Completed
http://0.0.0.0:5000/api/downloads?filter=Deleting
http://0.0.0.0:5000/api/downloads?filter=Stopping
http://0.0.0.0:5000/api/downloads?filter=Unsupported
```

Example response:
```
[
    {
        "name": "Hello World 1",
        "state": 1,
        "meta": 0,
        "files": [
            "45f0bb72b0cf12e263b18cb3556c7b59",
            "f6356be8cc8eff7d77dee00ac1d4d0d1",
            "706195526552d9a32877724d0ea556c0"
            ],
        "lastException": null,
        "secondsToComplete": null,
        "groupPercent": null,
        "bytesPerSecond": null
    },
    {
        "name": "Hello World 2",
        "state": 5,
        "meta": 1,
        "files": [
            "45f0bb72b0cf12e263b18cb3556c7b59",
            "f6356be8cc8eff7d77dee00ac1d4d0d1",
            "706195526552d9a32877724d0ea556c0"
            ],
        "lastException": null,
        "secondsToComplete": 32,
        "groupPercent": 50,
        "bytesPerSecond": 128070
    }
]
```

## Download

Returns information about the specified download.

Name | Wert
--- | ---
Address | **/api/downloads/{name}**
Return | **application/json**
Method | **GET**
Status code | **401 Unauthorized**, **200 OK** or **404 Not Found**

Example response:
```
{
    "name": "Hello World 1",
    "state": 1,
    "meta": 0,
    "files": [
        "45f0bb72b0cf12e263b18cb3556c7b59",
        "f6356be8cc8eff7d77dee00ac1d4d0d1",
        "706195526552d9a32877724d0ea556c0"
        ],
    "lastException": null,
    "secondsToComplete": null,
    "groupPercent": null,
    "bytesPerSecond": null
}
```

## Create Download

Creates a new download under the specified name.

Name | Wert
--- | ---
Address | **/api/downloads/{name}**
Return | **application/json**
Parameter | files={array_with_files}?autostart={true/false}
Method | **POST**
Status code | **401 Unauthorized**, **201 Created** or **400 Bad Request**

Example response:
```
{
    name: "Name of the created download"
}
```
The sent name can be different from the returned name, not allowed path characters will be removed. If the download already exists, a number is appended at the end.
