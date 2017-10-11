![image](http://media.marketwire.com/attachments/201604/34215_PerimeterX_logo.jpg)

# perimeterx-asp-net

[PerimeterX](http://www.perimeterx.com) ASP.NET SDK
===================================================

> Latest stable version: [v2.10.0](https://www.nuget.org/packages/PerimeterXModule/2.1.0)

Table of Contents
-----------------

-   [Usage](#usage)
  *   [Dependencies](#dependencies)
  *   [Installation](#installation)
  *   [Basic Usage Example](#basic-usage)
-   [Configuration](#configuration)
  *   [Customizing Default Block Pages](#custom-block-page)
  *   [Blocking Score](#blocking-score)
  *   [Enable/Disable Captcha](#captcha-support)
  *   [Extracting Real IP Address](#real-ip)
  *   [Override UA header](#override-ua)
  *   [Filter Sensitive Headers](#sensitive-headers)
  *   [Sensitive Routes](#sensitive-routes)
  *   [API Timeouts](#api-timeout)
  *   [Send Page Activities](#send-page-activities)
  *   [Debug Mode](#debug-mode)
  *   [Base URI](#base-uri)
-   [Contributing](#contributing)
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
	..
    blockingScore="70"
    ..
```

#### <a name="captcha-support"></a>Enable/disable captcha in the block page

By enabling captcha support, a captcha will be served as part of the block page giving real users the ability to answer, get score clean up and passed to the requested page.

**default: true**

```xml
	..
    captchaEnabled="true"
    ..
```

#### <a name="real-ip"></a>Extracting the Real User IP Address

> Note: IP extraction according to your network setup is important. It is common to have a load balancer/proxy on top of your applications, in this case the PerimeterX module will send an internal IP as the user's. In order to perform processing and detection for server-to-server calls, PerimeterX module need the real user ip.

The user ip can be returned to the PerimeterX module using a name of a header including the IP address.

**default: IP is taken from UserHostAddress of the incoming request**

```xml
	..
    socketIpHeader="X-PX-TRUE-IP"
    ..
```

#### <a name="sensitive-headers"></a> Filter sensitive headers

A user can define a list of sensitive header he want to prevent from being send to perimeterx servers (lowered case header name), filtering cookie header for privacy is set by default and will be overridden if a user set the configuration

**default: cookie, cookies**

```xml
	..
    sensitiveHeaders="cookie,cookies"
    ..
```

#### <a name="sensitive-routes"></a> Sensitive Routes

List of routes prefix. The Perimeterx module will always match request uri by this prefix list and if match was found will create a server-to-server call for, even if the cookie score is low and valid.


**default: None**

```xml
	..
    sensitiveRoutes="/login,/user/profile"
    ..
```

#### <a name="api-timeout"></a>API Timeouts

Control the timeouts for PerimeterX requests. The API is called when the risk cookie does not exist, or is expired or invalid.

API Timeout in milliseconds to wait for the PerimeterX server API response.


**default:** 2000

```
	..
    apiTimeout="2000"
    ..
```

#### <a name="send-page-activities"></a> Send Page Activities

Boolean flag to enable or disable sending activities and metrics to
PerimeterX on each page request. Enabling this feature will provide data
that populates the PerimeterX portal with valuable information such as
amount requests blocked and API usage statistics.

**default:** false

```xml
    ..
    sendPageActivities="false"
    ..
```

#### <a name="base-uri"></a> Base URI

A user can define a different API endpoint as a target URI to send the requests and will override the default address. Use this parameter after discussing the change with PerimeterX support team.

**default:** https://sapi.perimeterx.net

```xml
	..
    baseUri="https://sapi.perimeterx.net"
    ..
```

#### <a name="override-ua"></a> Custom User Agent Header

The user's user agent can be returned to the PerimeterX module using a name of a header that includes the user agent

**default: The User Agent is taken from header name "user-agent" from the incoming request**

```xml
    ..
    useragentOverride="px-user-agent"
    ..
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
