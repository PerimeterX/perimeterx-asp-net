# pxAspNetModule

HTTPModule for ASP.NET

## Integration

#### Edit web.config

Add PerimeterX PxModule (system.webServer level)

```
    <modules>
      <add name="PxModule" type="PerimeterX.PxModule"/>
    </modules>
```

Add configuration section (configuration level)

```
  <configSections>
    <sectionGroup name="perimeterX">
      <section
        name="pxModuleConfigurationSection"
        type="PerimeterX.PxModuleConfigurationSection"
      />
    </sectionGroup>
    ...
```

Add site specific configuration (configuration level)

```
 <perimeterX>
    <pxModuleConfigurationSection
      enabled="true"
      internalBlockPage="true"
      baseUri="http://localhost:5000"
      apiToken="eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJzY29wZXMiOlsicmlza19zY29yZSIsInJlc3RfYXBpIl0sImlhdCI6MTQ2MTA1MTc1Nywic3ViIjoiUFg2MDAyIiwianRpIjoiMTczNTJkYmMtODYwOS00YTBjLWIzN2EtYzY0NjUxNDE2MzU1In0.gSxeM85FtJhh4YeNqHP2WfOSKZl6Y0AwgS1icjiSrSk"
      cookieKey="password"
      blockScore="60"
      signWithSocketIp="false"
      ignoreUrlRegex="(\.css|\.txt|\.js|\.gif |\.jpg|\.png)$"
      >
    </pxModuleConfigurationSection>
  </perimeterX>
```
