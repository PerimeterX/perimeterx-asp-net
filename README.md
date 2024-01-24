![image](https://s.perimeterx.net/logo.png)

# perimeterx-asp-net

[PerimeterX](http://www.perimeterx.com) ASP.NET SDK
===================================================

> Latest stable version: [v3.3.0](https://www.nuget.org/packages/PerimeterXModule/3.2.1)

Table of Contents
-----------------

  **[Usage](#usage)**
  *   [Dependencies](#dependencies)
  *   [Installation](#installation)
  *   [Basic Usage Example](#basic-usage)
  *   [Upgrading](#upgrade)

  **[Configuration](#configuration)**
  *   [Customizing Default Block Pages](#custom-block-page)
  *   [Blocking Score](#blocking-score)
  *   [Custom Verification Handler](#custom-verification-handler)
  *   [Enable/Disable Captcha](#captcha-support)
  *   [First Party Mode](#first-party)
  *   [Extracting Real IP Address](#real-ip)
  *   [Filter Sensitive Headers](#sensitive-headers)
  *   [Sensitive Routes](#sensitive-routes)
  *   [Whitelist Routes](#whitelist-routes)
  *   [Enforcer Specific Routes](#enforcer-specific-routes)
  *   [API Timeouts](#api-timeout)
  *   [Send Page Activities](#send-page-activities)
  *   [Monitor Mode](#monitor-mode)
  *   [Base URI](#base-uri)
  *   [Override UA header](#override-ua)
  *   [Custom Cookie Header](#customCookieHeader)
  *   [Mitigation Urls](#mitigiation-urls)
  *   [Test Block Flow on Monitoring Mode](#bypass-monitor-header)

  **[Advanced Blocking Response](#advancedBlockingResponse)**

  **[Contributing](#contributing)**
  *   [Tests](#tests)

<a name="Usage"></a>

<a name="dependencies"></a> Dependencies
----------------------------------------

-   [.NET >= 4.5](https://www.microsoft.com/en-us/download/details.aspx?id=30653)

<a name="installation"></a> Installation
----------------------------------------

To install PxModule, run the following command in the Package Manager Console

```
PM> Install-Package PerimeterXModule
```

## Integration

#### Edit web.config

Add PerimeterX PxModule (configuration -> system.webServer level)

```xml
    <modules>
      <add name="PxModule" type="PerimeterX.PxModule"/>
    </modules>
```

Add configuration section (configuration level)

```xml
  <configSections>
    <sectionGroup name="perimeterX">
      <section
        name="pxModuleConfigurationSection"
        type="PerimeterX.PxModuleConfigurationSection"
      />
    </sectionGroup>
    ...
```

### <a name="basic-usage"></a> Basic Usage Example

Add site specific configuration (configuration level)

```xml
 <perimeterX>
    <pxModuleConfigurationSection
      enabled="true"
      appId="<PX Application ID>"
      apiToken="<API token>"
      cookieKey="<cookie key>"
      monitorMode="true"
      blockingScore="100"
      >
    </pxModuleConfigurationSection>
  </perimeterX>
```
### <a name="upgrade"></a> Upgrading
To upgrade to the latest Enforcer version:

1. In Visual Studio, right click on the solution and Select **Manage NuGet packages for solution**.
2. Search for `perimeterxmodule` in the updates section, and update.

   **OR**

Run `Install-Package PerimeterXModule` in the Package Manager Console

Your Enforcer version is now upgraded to the latest enforcer version.

For more information, contact [PerimeterX Support](support@perimeterx.com).

### <a name="additional-info"></a> Additional Info

#### <a name="uri-delimiters"></a> URI Delimiters

PerimeterX processes URI paths with general- and sub-delimiters according to RFC 3986. General delimiters (e.g., `?`, `#`) are used to separate parts of the URI. Sub-delimiters (e.g., `$`, `&`) are not used to split the URI as they are considered valid characters in the URI path.

### <a name="configuration"></a> Configuration Options

#### Configuring Required Parameters

Configuration options are set in `pxModuleConfigurationSection`

#### Required parameters:

- appId
- cookieKey
- apiToken

#### <a name="custom-block-page"></a> Customizing Default Block Pages

##### Custom Logo
Adding a custom logo to the blocking page is by providing the pxConfig a key ```customLogo``` , the logo will be displayed at the top div of the the block page The logo's ```max-height``` property would be 150px and width would be set to ```auto```

The key ```customLogo``` expects a valid URL address such as ```https://s.perimeterx.net/logo.png```

Example below:
```xml
...
  customLogo="https://s.perimeterx.net/logo.png"
...
```

##### Custom JS/CSS

The block page can be modified with a custom CSS by adding to the ```pxConfig``` the key ```cssRef``` and providing a valid URL to the css In addition there is also the option to add a custom JS file by adding ```jsRef``` key to the ```pxConfig``` and providing the JS file that will be loaded with the block page, this key also expects a valid URL

On both cases if the URL is not a valid format an exception will be thrown

Example below:
```xml
...
  jsRef="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/js/bootstrap.min.js"
  cssRef="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css"
...
```

##### Redirect to a Custom Block Page URL
Customizes the block page to meet branding and message requirements by specifying the URL of the block page HTML file. The page can also implement CAPTCHA.

**Default:** "" (empty string)

Example:

```xml
customBlockUrl = "http://sub.domain.com/block.html"
```

> Note: This URI is whitelisted automatically to avoid infinite redirects.

##### <a name="redirect_on_custom_url"></a> Redirect on Custom URL

The `redirectOnCustomUrl` boolean flag to redirect users to a block page.

**Default:** false

Example:

```xml
  redirectOnCustomUrl = "false"
```

By default, when a user exceeds the blocking threshold and blocking is enabled, the user is redirected to the block page defined by the `customBlockUrl` variable. The defined block page displays a **307 (Temporary Redirect)** HTTP Response Code.

When the flag is set to false, a **403 (Unauthorized)** HTTP Response Code is displayed on the blocked page URL.


Setting the flag to true (enabling redirects) results in the following URL upon blocking:

```
http://www.example.com/block.html?url=L3NvbWVwYWdlP2ZvbyUzRGJhcg==&uuid=e8e6efb0-8a59-11e6-815c-3bdad80c1d39&vid=08320300-6516-11e6-9308-b9c827550d47
```

Setting the flag to false does not require the block page to include any of the examples below, as they are injected into the blocking page via the PerimeterX ASP.NET Enforcer.

> NOTE: The `url` parameter should be built with the URL *Encoded* query parameters (of the original request) with both the original path and variables Base64 Encoded (to avoid collisions with block page query params).

##### Custom Block Pages Requirements

As of version 2.8.0, Captcha logic is being handled through the JavaScript snippet and not through the Enforcer.

Users who have Custom Block Pages must include the new script tag and a new div in the _.html_ block page.


#### <a name="blocking-score"></a> Changing the Minimum Score for Blocking

**default:** 100

```xml
...
  blockingScore="100"
...
```

#### <a name="custom-verification-handler"></a> Custom Verification Handler

A custom verification handler can be called by the PxModule instead of the default behavior, to allow a user to customize the behavior based on the risk score returned by PerimeterX.

The custom handler class should implement the `IVerificationHandler` interface, and its name should be added to the configuration section:

```xml
...
  customVerificationHandler="UniqueVerificationHandler"
...
```

The custom logic will reside in the `Handle` method, making use of the following arguments:

- `HttpApplication application` - The currently running ASP.NET application methods, properties and events. Calling [`application.CompleteRequest`](https://msdn.microsoft.com/en-us/library/system.web.httpapplication.completerequest(v=vs.110).aspx), for example, will directly execute the [`EndRequest`](https://msdn.microsoft.com/en-us/library/system.web.httpapplication.endrequest(v=vs.110).aspx) event to return a response to the client.
- `PxContext pxContext` - The PerimeterX context, containing valuable fields such as `Score`, `UUID`, `BlockAction` etc.
- `PxModuleConfigurationSection pxConfig` - The current configuration used by the PxModule, representing the `PerimeterX.PxModuleConfigurationSection` settings. Contains fields such as `BlockingScore`.

Common customization options are presenting of a captcha or a custom branded block page.

```xml
...

namespace myUniqueApp
{
    public class UniqueVerificationHandler : IVerificationHandler
    {
        public void Handle(HttpApplication application, PxContext pxContext, PxModuleConfigurationSection pxConfig)
        {
            // Custom verification logic goes here.
            // The following code is only an example of a possible implementation:

            if (pxContext.Score >= pxConfig.BlockingScore) // In the case of a high score, present the standard block/captcha page
            {
                PxModule.BlockRequest(pxContext, pxConfig);
                application.CompleteRequest();
            }
        }
    }
}
```

#### <a name="captcha-support"></a>Enable/disable captcha in the block page
***DEPRECATED***


By enabling captcha support, a captcha will be served as part of the block page giving real users the ability to answer, get score clean up and passed to the requested page.

**default: true**

```xml
...
  captchaEnabled="true"
...
```

#### <a name="first-party"></a>First Party Mode

Enables the module to receive/send data from/to the sensor, acting as a "reverse-proxy" for client requests and sensor activities.

Customers are advised to use the first party sensor (where the web sensor is served locally, from your domain) for two main reasons:

 - Improved performance - serving the sensor as part of the standard site content removes the need to open a new connection to PerimeterX servers when a page is loaded.
 - Improved detection - third party content may sometimes be blocked by certain browser plugins and privacy addons.
 - First party sensor directly leads to improved detection, as observed on customers who previously moved away from third party sensor.

The following routes will be used in order to serve the sensor and send activities:
 - /\<PREFIX\>/xhr/*
 - /\<PREFIX\>/init.js

First Party may also require additional changes on the sensor snippet (client side). Refer to the portal for more information.

**default: true**

```xml
...
  firstPartyEnabled="true"
  firstPartyXhrEnabled="true"
...
```

#### <a name="real-ip"></a>Extracting the Real User IP Address

> Note: IP extraction according to your network setup is important. It is common to have a load balancer/proxy on top of your applications, in this case the PerimeterX module will send an internal IP as the user's. In order to perform processing and detection for server-to-server calls, PerimeterX module need the real user ip.

The user ip can be returned to the PerimeterX module using a name of a header including the IP address.

**default: IP is taken from UserHostAddress of the incoming request**

```xml
...
  socketIpHeader="X-PX-TRUE-IP"
...
```

#### <a name="sensitive-headers"></a> Filter sensitive headers

A user can define a list of sensitive header he want to prevent from being send to perimeterx servers (lowered case header name), filtering cookie header for privacy is set by default and will be overridden if a user set the configuration

**default: cookie, cookies**

```xml
...
  sensitiveHeaders="cookie,cookies"
...
```

#### <a name="sensitive-routes"></a> Sensitive Routes

List of routes prefix. The Perimeterx module will always match request uri by this prefix list and if match was found will create a server-to-server call for, even if the cookie score is low and valid.


**default: None**

```xml
...
  sensitiveRoutes="/login,/user/profile"
...
```

#### <a name="whitelist-routes"></a> Whitelist Routes

List of routes prefix. The Perimeterx module will skip detection if the prefix match request uri .

**default: None**

```xml
...
  routesWhitelist="/login,/user/profile"
...
```

#### <a name="enforcer-specific-routes"></a> Enforcer Specific Routes

List of routes prefix. If the list is not empty, The Perimeterx module will enforcer only on the url that match the prefix, any other route will be skipped

**default: None**

```xml
...
  enforceSpecificRoutes="/protect/route,/login,/checkout"
...
```

#### <a name="api-timeout"></a>API Timeouts

Control the timeouts for PerimeterX requests. The API is called when the risk cookie does not exist, or is expired or invalid.

API Timeout in milliseconds to wait for the PerimeterX server API response.


**default:** 2000

```
...
  apiTimeout="2000"
...
```

#### <a name="send-page-activities"></a> Send Page Activities
Boolean flag to enable or disable page activities
Sending page activities is asynchronous and not blocking the request

**default:** True

```xml
...
  sendPageActivities="false"
...
```


#### <a name="monitor-mode"></a> Monitor Mode

Boolean flag to enable or disable monitor mode
While monitor mode is on, all requests will be inspected but not blocked
Set this flag to false to disable monitor mode

```xml
...
  monitorMode="true"
...
```
**default:** true

#### <a name="base-uri"></a> Base URI

A user can define a different API endpoint as a target URI to send the requests and will override the default address. Use this parameter after discussing the change with PerimeterX support team.

**default:** https://sapi.perimeterx.net

```xml
...
  baseUri="https://sapi.perimeterx.net"
...
```

#### <a name="override-ua"></a> Custom User Agent Header

The user's user agent can be returned to the PerimeterX module using a name of a header that includes the user agent

**default: The User Agent is taken from header name "user-agent" from the incoming request**

```xml
...
  useragentOverride="px-user-agent"
...
```

#### <a name="customCookieHeader"></a>Custom Cookie Header

When set, instead of extrating the PerimeterX Cookie from the `Cookie` header, this property specifies a header name that will contain the PerimeterX Cookie.

**Default:** x-px-cookies

```xml
  ...
  customCookieHeader: "some-header-name"
  ...
```

#### <a name="data-enrichment"></a> Data Enrichment

Users can use the additional activity handler to retrieve information for the request using the data-enrichment object. First, check that the data enrichment object is verified, then you can access it's properties.

```c#
...

#### <a name="mitigiation-urls"></a> Mitigation Urls

Users can define custom paths that allow blocking. All other paths will be set to monitoring mode.
```c#
mitigiation-urls="path1, path2"
...

namespace MyApp
{
    public class MyVerificationHandler : IVerificationHandler
    {
        public void Handle(HttpApplication application, PxContext pxContext, PxModuleConfigurationSection pxConfig)
        {
          ...
          if (pxContext.IsPxdeVerified) {
            dynamic pxde = pxContext.Pxde;
            // do something with the data enrichment
          }
          ...
        }
    }
}
```

#### <a name=“bypass-monitor-header”></a> Test Block Flow on Monitoring Mode

Allows you to test an enforcer’s blocking flow while you are still in Monitor Mode.

When the header name is set (eg. `x-px-block`) and the value is set to `1`, when there is a block response (for example from using a User-Agent header with the value of `PhantomJS/1.0`) the Monitor Mode is bypassed and full block mode is applied. If one of the conditions is missing you will stay in Monitor Mode. This is done per request.
To stay in Monitor Mode, set the header value to `0`.

The Header name is configurable using the `bypassMonitorHeader` property.

**Default:** not set

```xml
...
  bypassMonitorHeader="x-px-block"
...
```

<a name="advancedBlockingResponse"></a> Advanced Blocking Response
------------------------------------------------------------------
In special cases, (such as XHR post requests) a full Captcha page render might not be an option. In such cases, using the Advanced Blocking Response returns a JSON object containing all the information needed to render your own Captcha challenge implementation, be it a popup modal, a section on the page, etc. The Advanced Blocking Response occurs when a request contains the *Accept* header with the value of `application/json`. A sample JSON response appears as follows:

```javascript
{
    "appId": String,
    "jsClientSrc": String,
    "firstPartyEnabled": Boolean,
    "vid": String,
    "uuid": String,
    "hostUrl": String,
    "blockScript": String
}
```

Once you have the JSON response object, you can pass it to your implementation (with query strings or any other solution) and render the Captcha challenge.

In addition, you can add the `_pxOnCaptchaSuccess` callback function on the window object of your Captcha page to react according to the Captcha status. For example when using a modal, you can use this callback to close the modal once the Captcha is successfullt solved. <br/> An example of using the `_pxOnCaptchaSuccess` callback is as follows:

```javascript
window._pxOnCaptchaSuccess = function(isValid) {
    if(isValid) {
        alert("yay");
    } else {
        alert("nay");
    }
}
```

For details on how to create a custom Captcha page, refer to the [documentation](https://docs.perimeterx.com/pxconsole/docs/customize-challenge-page)

<a name="contributing"></a> Contributing
----------------------------------------

The following steps are welcome when contributing to our project.
### Fork/Clone
First and foremost, [Create a fork](https://guides.github.com/activities/forking/) of the repository, and clone it locally.
Create a branch on your fork, preferably using a self descriptive branch name.

### Code/Run
Code your way out of your mess, and help improve our project by implementing missing features, adding capabilities or fixing bugs.

To run the code, simply follow the steps in the [installation guide](#installation). Grab the keys from the PerimeterX Portal, and try refreshing your page several times continuously. If no default behaviours have been overridden, you should see the PerimeterX block page. Solve the CAPTCHA to clean yourself and start fresh again.

### Pull Request
After you have completed the process, create a pull request to the Upstream repository. Please provide a complete and thorough description explaining the changes. Remember this code has to be read by our maintainers, so keep it simple, smart and accurate.

### Thanks
After all, you are helping us by contributing to this project, and we want to thank you for it.
We highly appreciate your time invested in contributing to our project, and are glad to have people like you - kind helpers.
