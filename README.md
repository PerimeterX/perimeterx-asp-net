![image](https://s.perimeterx.net/logo.png)

# perimeterx-asp-net

[PerimeterX](http://www.perimeterx.com) ASP.NET SDK
===================================================

> Latest stable version: [v2.7.0](https://www.nuget.org/packages/PerimeterXModule/2.7.0)

Table of Contents
-----------------

  **[Usage](#usage)**
  *   [Dependencies](#dependencies)
  *   [Installation](#installation)
  *   [Basic Usage Example](#basic-usage)

  **[Configuration](#configuration)**
  *   [Customizing Default Block Pages](#custom-block-page)
  *   [Blocking Score](#blocking-score)
  *   [Custom Verification Handler](#custom-verification-handler)
  *   [Enable/Disable Captcha](#captcha-support)
  *   [First Party Mode](#first-party)
  *   [Extracting Real IP Address](#real-ip)
  *   [Override UA header](#override-ua)
  *   [Filter Sensitive Headers](#sensitive-headers)
  *   [Sensitive Routes](#sensitive-routes)
  *   [Whitelist Routes](#whitelist-routes)
  *   [Enforcer Specific Routes](#enforcer-specific-routes)
  *   [API Timeouts](#api-timeout)
  *   [Send Page Activities](#send-page-activities)
  *   [Monitor Mode](#monitor-mode)
  *   [Base URI](#base-uri)

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
      monitorMode="false"
      blockingScore="70"
      >
    </pxModuleConfigurationSection>
  </perimeterX>
```

### <a name="configuration"></a> Configuration Options

#### Configuring Required Parameters

Configuration options are set in `pxModuleConfigurationSection`

#### Required parameters:

- appId
- cookieKey
- apiToken

#### <a name="custom-block-page"></a> Customizing Default Block Pages
###### Custom Logo
Adding a custom logo to the blocking page is by providing the pxConfig a key ```customLogo``` , the logo will be displayed at the top div of the the block page The logo's ```max-heigh``` property would be 150px and width would be set to ```auto```

The key ```customLogo``` expects a valid URL address such as ```https://s.perimeterx.net/logo.png```

Example below:
```xml
...
  customLogo="https://s.perimeterx.net/logo.png"
...
```

Custom JS/CSS

The block page can be modified with a custom CSS by adding to the ```pxConfig``` the key ```cssRef``` and providing a valid URL to the css In addition there is also the option to add a custom JS file by adding ```jsRef``` key to the ```pxConfig``` and providing the JS file that will be loaded with the block page, this key also expects a valid URL

On both cases if the URL is not a valid format an exception will be thrown

Example below:
```xml
...
  jsRef="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css"
  cssRef="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/js/bootstrap.min.js"
...
```
#### <a name="blocking-score"></a> Changing the Minimum Score for Blocking

**default:** 70

```xml
...
  blockingScore="70"
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
  monitorMode="false"
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

<a name="contributing"></a> Contributing
----------------------------------------

The following steps are welcome when contributing to our project.
###Fork/Clone
First and foremost, [Create a fork](https://guides.github.com/activities/forking/) of the repository, and clone it locally.
Create a branch on your fork, preferably using a self descriptive branch name.

###Code/Run
Code your way out of your mess, and help improve our project by implementing missing features, adding capabilities or fixing bugs.

To run the code, simply follow the steps in the [installation guide](#installation). Grab the keys from the PerimeterX Portal, and try refreshing your page several times continuously. If no default behaviours have been overridden, you should see the PerimeterX block page. Solve the CAPTCHA to clean yourself and start fresh again.

###Pull Request
After you have completed the process, create a pull request to the Upstream repository. Please provide a complete and thorough description explaining the changes. Remember this code has to be read by our maintainers, so keep it simple, smart and accurate.

###Thanks
After all, you are helping us by contributing to this project, and we want to thank you for it.
We highly appreciate your time invested in contributing to our project, and are glad to have people like you - kind helpers.
