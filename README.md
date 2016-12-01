# pxAspNetModule

HTTPModule for ASP.NET

## Integration

#### Edit web.config

Add the PerimeterX PxModule (configuration -> system.webServer level):

```xml
    <modules>
      <add name="PxModule" type="PerimeterX.PxModule"/>
    </modules>
```

Add the configuration section (configuration level):

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

Add site specific configuration (configuration level):

```xml
 <perimeterX>
    <pxModuleConfigurationSection
      enabled="true"
      appId="<Application ID (PX)>"
      internalBlockPage="true"
      apiToken="<API token>"
      cookieKey="<cookie key>"
      blockingScore="70"
      ignoreUrlRegex="(\.css|\.txt|\.js|\.gif |\.jpg|\.png)$"
      >
    </pxModuleConfigurationSection>
  </perimeterX>
```
