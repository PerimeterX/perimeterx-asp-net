# Change Log

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)

##[2.0.1] - 2017-15-05
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
	

##[1.2.0] - 2017-24-04
- Support custom header for user-agent

##[1.1.1] - 2017-20-04 
- added .axd files to whitelist files
- sending px_orig_value when decryption fails

##[1.1] - 2017-28-03
- Moved server url to new URL
- New design for block pages
- Block page customisation 
- Support for classic pipeline mode