# Change Log

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](http://keepachangelog.com/)

## [3.3.0] - 2024-01-24
### Added
- Support for custom cookie header

### Fixed
- first-party XHR url validation

## [3.2.2] - 2023-08-02
### Fixed
- Added firstPartyEnabled field to advanced blocking response


## [3.2.1] - 2023-07-17
### Fixed
- Fixed advanced blocking response feature


## [3.2.0] - 2023-01-03
### Added
- Support for credentials intelligence protocols `v2` and `multistep_sso`
- Support for login successful reporting methods `header`, `status`, `body`, and `custom`
- Support for automatic sending of `additional_s2s` activity
- Support for manual sending of `additional_s2s` activity via header
- Support for sending raw username on `additional_s2s` activity
- Support for login credentials extraction via custom function
- New `request_id` field to all enforcer activities

## [3.1.4] - 2022-12-05
### Added
- Compatibility with .NET Framework 4.7 and higher

## [3.1.3] - 2022-07-21
### Added
- Simulated block indication on block activities
- HTTP method on async activities

## [3.1.2] - 2019-09-22
### Fixed
- customBlockUrl - url query param will now point to path instead of absolute url

## [3.1.1] - 2019-07-03
### Fixed
- customBlockUrl redirect flow
- customBlockUrl whitelisting

## [3.1.0] - 2019-06-05
### Added
- Send telemetry on demand by header
- Support for testing blocking flow in monitor mode

### Fixed
- CustomVerificationHandler handling

## [3.0.0] - 2019-01-17
### Added
- Added PXHD handling
- Added cookie names extraction
- Added data enrichment cookie handling to context
- Added custom block page with redirects feature

## [2.7.0] - 2018-09-16
### Added
- Support for simulated_block
### Fixed
- Captcha v2 template and error handling
- Various stablity and performance fixes

## [2.6.0] - 2018-08-07
### Added
- Support for captcha v2

## [2.5.1] - 2018-11-06
### Fixed
- Mobile token extraction in cookie validator

## [2.5.0] - 2018-14-03
### Added
- Support for first party

## [2.4.0] - 2018-21-02
### Added
- Support enforced specific routes

## [2.3.0] - 2018-05-02
### Added
- Support for mobile sdk
- Support for original tokens
- Support funCaptcha in mobile
- Enforcer Telemetry
### Modified
- Edit block page footer
- Edit reCaptcha template to use b64 captcha
- Enrichment for async activities
### Fixed
- Handling duplicate cookies

## [2.2.0] - 2017-11-10
### Fixed
- Fixed default value for sensitive_route
- Using action_block to render block pages
- Naming for s2s expired_cookie reason to cookie_expired
### Added
- JS Challenge support
- FunCaptcha support
- CustomVerificationHandler support
- MonitorMode and set default to true
	Please note: 	MonitorMode is breaking backward support
		if you upgrade to this version or further
		and want to keep your blocking active, please set its value to False

## [2.1.0] - 2017-04-06
### Fixed
- Renamed risk_score to block_score in activity details
- Fixed block score threshold
## Added
- Support for sensitive routes
- Log page requested reason
- Mesure risk rout trip time

## [2.0.3] - 2017-15-05
### Fixed
- Collect right Hostname in context
- Renamed module_version
### Added
- Block/Page Requested Activities now sends module_verison and risk_socre
- Support Cookie v3
- Support RiskAPI v2
### Changed
- Moved PxModule verification code, request state, api calls to managable files
- New classes, Validators, DataContracts (Cookies, Activities, Requests etc...)
- Refactor module to work with PxContext
- Reordered library into folders

## [1.2.0] - 2017-24-04
- Support custom header for user-agent

## [1.1.1] - 2017-20-04
- added .axd files to whitelist files
- sending px_orig_value when decryption fails

## [1.1] - 2017-28-03
- Moved server url to new URL
- New design for block pages
- Block page customisation
- Support for classic pipeline mode
