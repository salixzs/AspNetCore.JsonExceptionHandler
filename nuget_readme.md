# Salix.AspNetCore.Utilities
A collection of additional functionality to Asp.Net Core framework for building better APIs.

There are few functionality extensions provided in package, allowing to better handle some task implementations when creating REST API in Asp.net Core framework.

These include:

* **Global Exception handler** (Json error) 
* **Health check formatter**
* **[ConfigurationValidation](https://github.com/salixzs/ConfigurationValidation) handlers**
* **Home page & Health page** (w/o full MVC stack)

Package is built on .Net Standard 2.0. It is tested by using within Asp.Net Core 5 (on .Net 5) API. [Sample solution](https://github.com/salixzs/AspNetCore.Utilities/tree/main/Samples/Sample.AspNet5) is available in GitHub repository.

### Global error handler

Allows exceptions to be returned to API calling parties as JSON structure, adding some additional information on its type and causes (incl. CallStack when API runs in development mode). This allows to get these 400 & 500 Http Status code errors with additional information, somewhat complying to [IETF rfc7807](https://tools.ietf.org/html/rfc7807) proposed standard. Here is example on how error is returned when exception is thrown somewhere in API:
```json
{
  "type": "ServerError",
  "title": "This is thrown on purpose.",
  "status": 500,
  "requestedUrl": "/api/sample/exception",
  "errorType": 1,
  "exceptionType": "ApplicationException",
  "innerException": null,
  "innerInnerException": null,
  "stackTrace": [
    "at FaultyLogic() in Sample.AspNet5.Logic\\SampleLogic.cs: line 18",
    "at ThrowException() in Sample.AspNet5.Api\\Services\\HomeController.cs: line 110",
    "at Invoke(HttpContext httpContext) in Source\\Salix.ExceptionHandling\\ApiJsonExceptionMiddleware.cs: line 56"
  ],
  "validationErrors": []
}
```
(Stack trace is not shown when API runs in production (configurable)).

Provided functionalities can handle other types of exceptions differently, like `NotImplementedException`, `DataValidationException` and any other specific exception.

See [more extensive documentation](https://github.com/salixzs/AspNetCore.Utilities/blob/main/Documentation/GlobalErrorHandler.md) on how to use this functionality.


### Health check formatter
Custom formatter for Asp.Net HealthCheck functionalities, extending it to include additional information, like configuration values and other information which might help immediately pinpoint problem when health check is returning Degraded or Unhealthy responses. 

Example response:

```json
{
    "status": "Healthy",
    "checks": [
        {
            "key": "Database",
            "status": "Healthy",
            "description": "Database is OK.",
            "exception": null,
            "data": [
                {
                    "key": "ConnString",
                    "value": "Connection string (shown only in developer mode)"
                }
            ]
        },
        {
            "key": "ExtApi",
            "status": "Healthy",
            "description": "ExtAPI is OK.",
            "exception": null,
            "data": [
                {
                    "key": "ExtApi URL",
                    "value": "https://extapi.com/api"
                },
                {
                    "key": "User",
                    "value": "username from config"
                },
                {
                    "key": "Password",
                    "value": "password from config"
                },
                {
                    "key": "Token",
                    "value": "Secret token from config"
                }
            ]
        }
    ]
}
```

See [more extensive documentation](https://github.com/salixzs/AspNetCore.Utilities/blob/main/Documentation/HealthCheckFormatter.md) on how to use this functionality.

### [ConfigurationValidation](https://github.com/salixzs/ConfigurationValidation) handlers

Extends configuration class validations, provided by package [ConfigurationValidation](https://www.nuget.org/packages/ConfigurationValidation/) ([Repo](https://github.com/salixzs/ConfigurationValidation)).

It implements necessary registration extensions for your configuration classes and three different handlers of validation results.

#### IStartupFilter
Checks configuration objects during app startup and throws exception (preventing app from starting up).

#### Configuration Error page
Implemented as middleware component, similar to "UseDeveloperErrorPage", which returns "yellow screen of death" for your application when configuration validation failed. Application itself will not work, but you can see in browser (when open app in it) this error page.

![Configuration error page](https://raw.githubusercontent.com/salixzs/AspNetCore.Utilities/main/Documentation/config-error.jpg)

#### HealthCheck
Standard Asp.Net HealthCheck solution to include in application health checking routines.

See [more extensive documentation](https://github.com/salixzs/AspNetCore.Utilities/blob/main/Documentation/ConfigurationValidation.md) on how to use this functionality.

### Root page

Default Asp.Net API provides no visible output when its root URL is open in browser. If there is no default controller for root path, it returns 404 (not found) error. 

Salix.AspNetCore.Utilites include simple page renderer to show some technical and monitoring/troubleshooting information, which can be used in HomeController Index action. 

It does not require full MVC stack (views, razor) to have them, so API stays in controller-only mode.

Here is example of such page from Sample solution with all bells and whistles:

![Root page example](https://raw.githubusercontent.com/salixzs/AspNetCore.Utilities/main/Documentation/root-page.JPG)


### Health check / test page

**NOTE:** This functionality should be implemented *only* if you have at least one HelathCheck implemented.
ASP.NET HealthCheck provides only JSON endpoint for health checking, but looking at HealthCheck Json output can be problematic for human eye to grasp necessary information right away. This page can provide visual colored cues for such overview, something like this:

![Health check and testing page](https://raw.githubusercontent.com/salixzs/AspNetCore.Utilities/main/Documentation/health-check-page.JPG)

See [more extensive documentation](https://github.com/salixzs/AspNetCore.Utilities/blob/main/Documentation/Pages.md) on how to use this functionality.


## How to use

You add `Salix.AspNetCore.Utilities` package to asp.net project use Visual Studio NuGet manager or from command line:
```plaintext
PM> Install-Package Salix.AspNetCore.Utilities
```


## Release notes
Available [in this link](https://github.com/salixzs/AspNetCore.Utilities/blob/main/Documentation/ReleaseNotes.md).
