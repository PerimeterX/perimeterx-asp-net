![image](http://media.marketwire.com/attachments/201604/34215_PerimeterX_logo.jpg)

# perimeterx-asp-net

[PerimeterX](http://www.perimeterx.com) ASP.NET SDK
===================================================

Table of Contents
-----------------

-   [Usage](#usage)
  *   [Dependencies](#dependencies)
  *   [Installation](#installation)
  *   [Basic Usage Example](#basic-usage)
-   [Configuration](#configuration)
  *   [Blocking Score](#blocking-score)
  *   [Enable/Disable Captcha](#captcha-support)
  *   [Extracting Real IP Address](#real-ip)
  *   [Filter Sensitive Headers](#sensitive-headers)
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
<a name="contributing"></a> Contributing
----------------------------------------
